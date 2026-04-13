using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Components;
using FoldingFate.Features.Battle.Data;
using FoldingFate.Features.Battle.Events;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;
using FoldingFate.Features.Poker.Events;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Battle.Controllers
{
    public class BattleController : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly EntityFactory _entityFactory;
        private readonly BattleSystem _battleSystem;
        private readonly TurnSystem _turnSystem;
        private readonly BattleEffectController _effectController;
        private readonly BattleCharacterData[] _characterDataList;
        private readonly BattleMonsterData _monsterData;
        private readonly EntityView[] _allyViews;
        private readonly EntityView _enemyView;
        private readonly CompositeDisposable _disposables = new();

        private BattleModel _battle;
        private bool _isBattleActive;
        private bool _isProcessingTurn;

        [Inject]
        public BattleController(
            EventBus eventBus,
            EntityFactory entityFactory,
            BattleSystem battleSystem,
            TurnSystem turnSystem,
            BattleEffectController effectController,
            BattleCharacterData[] characterDataList,
            BattleMonsterData monsterData,
            EntityView[] allyViews,
            EntityView enemyView)
        {
            _eventBus = eventBus;
            _entityFactory = entityFactory;
            _battleSystem = battleSystem;
            _turnSystem = turnSystem;
            _effectController = effectController;
            _characterDataList = characterDataList;
            _monsterData = monsterData;
            _allyViews = allyViews;
            _enemyView = enemyView;
        }

        public void Start()
        {
            Debug.Log("[BattleController] Start — initializing battle");
            InitializeBattle();
            _eventBus.Receive<HandSubmittedEvent>()
                .Subscribe(e =>
                {
                    Debug.Log($"[BattleController] HandSubmittedEvent received — rank: {e.Result.Rank}");
                    OnHandSubmitted(e).Forget();
                })
                .AddTo(_disposables);
            Debug.Log("[BattleController] Subscribed to HandSubmittedEvent");
        }

        private void InitializeBattle()
        {
            var allies = new List<FoldingFate.Core.Entity>();
            for (int i = 0; i < _characterDataList.Length; i++)
            {
                var data = _characterDataList[i];
                var entity = _entityFactory.CreateCombatEntity(
                    $"ally_{i}", EntityType.Character, data.DisplayName,
                    data.MaxHp, data.Attack, data.Defense);
                allies.Add(entity);

                if (i < _allyViews.Length)
                {
                    _allyViews[i].Bind(entity);
                    _effectController.RegisterEntityView(entity, _allyViews[i]);
                    Debug.Log($"[BattleController] Bound {data.DisplayName} to {_allyViews[i].gameObject.name}");
                }
            }

            var enemies = new List<FoldingFate.Core.Entity>();
            var monsterEntity = _entityFactory.CreateCombatEntity(
                "enemy_0", EntityType.Monster, _monsterData.DisplayName,
                _monsterData.MaxHp, _monsterData.Attack, _monsterData.Defense);
            enemies.Add(monsterEntity);

            if (_enemyView != null)
            {
                _enemyView.Bind(monsterEntity);
                _effectController.RegisterEntityView(monsterEntity, _enemyView);
                Debug.Log($"[BattleController] Bound {_monsterData.DisplayName} to {_enemyView.gameObject.name}");
            }

            _battle = _battleSystem.StartBattle(allies.AsReadOnly(), enemies.AsReadOnly());
            _isBattleActive = true;
            Debug.Log($"[BattleController] Battle started — {allies.Count} allies vs {enemies.Count} enemies");
        }

        private async UniTask OnHandSubmitted(HandSubmittedEvent e)
        {
            if (!_isBattleActive || _isProcessingTurn)
            {
                Debug.Log($"[BattleController] Skipping — active:{_isBattleActive}, processing:{_isProcessingTurn}");
                return;
            }
            _isProcessingTurn = true;

            try
            {
                Debug.Log("[BattleController] === Player Turn Start ===");
                // Player turn
                _turnSystem.StartTurn(_battle);

                var playerActions = new List<BattleAction>();
                var targetEnemy = GetFirstAliveEnemy();
                if (targetEnemy == null) return;

                for (int i = 0; i < _battle.Allies.Count; i++)
                {
                    var ally = _battle.Allies[i];
                    if (ally.Get<Health>().IsAlive)
                        playerActions.Add(new BattleAction(ally, BattleActionType.Attack, targetEnemy));
                }

                Debug.Log($"[BattleController] {playerActions.Count} player actions, executing turn");
                _turnSystem.ExecuteTurn(_battle, playerActions.AsReadOnly());

                var lastRecord = _battle.TurnHistory[_battle.TurnHistory.Count - 1];
                Debug.Log($"[BattleController] Playing {lastRecord.Results.Count} effects");
                await _effectController.PlayTurnEffects(lastRecord.Results);

                _turnSystem.EndTurn(_battle);

                if (_battle.Phase.CurrentValue == BattlePhase.Victory)
                {
                    await _effectController.PlayVictory(_battle.Allies);
                    EndBattle();
                    _eventBus.Publish(new BattleTurnCompleteEvent(true));
                    return;
                }

                // Enemy turn
                _battle.Phase.Value = BattlePhase.EnemyTurn;

                var enemyActions = new List<BattleAction>();
                for (int i = 0; i < _battle.Enemies.Count; i++)
                {
                    var enemy = _battle.Enemies[i];
                    if (enemy.Get<Health>().IsAlive)
                    {
                        var targetAlly = GetRandomAliveAlly();
                        if (targetAlly != null)
                            enemyActions.Add(new BattleAction(enemy, BattleActionType.Attack, targetAlly));
                    }
                }

                if (enemyActions.Count > 0)
                {
                    _turnSystem.ExecuteTurn(_battle, enemyActions.AsReadOnly());
                    var enemyRecord = _battle.TurnHistory[_battle.TurnHistory.Count - 1];
                    await _effectController.PlayTurnEffects(enemyRecord.Results);
                }

                _turnSystem.EndTurn(_battle);
                bool isOver = _battle.Phase.CurrentValue == BattlePhase.Defeat;
                if (isOver)
                    EndBattle();

                _eventBus.Publish(new BattleTurnCompleteEvent(isOver));
            }
            finally
            {
                _isProcessingTurn = false;
            }
        }

        private FoldingFate.Core.Entity GetFirstAliveEnemy()
        {
            for (int i = 0; i < _battle.Enemies.Count; i++)
                if (_battle.Enemies[i].Get<Health>().IsAlive) return _battle.Enemies[i];
            return null;
        }

        private FoldingFate.Core.Entity GetRandomAliveAlly()
        {
            var alive = new List<FoldingFate.Core.Entity>();
            for (int i = 0; i < _battle.Allies.Count; i++)
                if (_battle.Allies[i].Get<Health>().IsAlive) alive.Add(_battle.Allies[i]);
            if (alive.Count == 0) return null;
            return alive[UnityEngine.Random.Range(0, alive.Count)];
        }

        private void EndBattle()
        {
            _isBattleActive = false;
            _battleSystem.EndBattle(_battle);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
