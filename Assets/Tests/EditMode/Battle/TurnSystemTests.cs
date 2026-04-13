using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class TurnSystemTests
    {
        private StatsSystem _statsSystem;
        private HealthSystem _healthSystem;
        private ResolveSystem _resolveSystem;
        private ApplySystem _applySystem;
        private TurnSystem _turnSystem;

        [SetUp]
        public void SetUp()
        {
            _statsSystem = new StatsSystem();
            _healthSystem = new HealthSystem();
            _resolveSystem = new ResolveSystem(_statsSystem);
            _applySystem = new ApplySystem(_healthSystem);
            _turnSystem = new TurnSystem(_resolveSystem, _applySystem);
        }

        private FoldingFate.Core.Entity CreateEntity(string id, EntityType type, float attack, float defense, float maxHp)
        {
            var entity = new FoldingFate.Core.Entity(id, type, id);
            var stats = new Stats();
            stats.BaseStats[EntityStatType.Attack] = attack;
            stats.BaseStats[EntityStatType.Defense] = defense;
            entity.Add(stats);
            entity.Add(new Health(maxHp));
            entity.Add(new Combat());
            return entity;
        }

        [Test]
        public void StartTurn_IncrementsTurnCount()
        {
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { CreateEntity("a", EntityType.Character, 10, 5, 100) },
                new List<FoldingFate.Core.Entity> { CreateEntity("e", EntityType.Monster, 5, 3, 50) });

            _turnSystem.StartTurn(battle);

            Assert.AreEqual(1, battle.TurnCount.CurrentValue);
            battle.Dispose();
        }

        [Test]
        public void StartTurn_SetsPhaseToPlayerTurn()
        {
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { CreateEntity("a", EntityType.Character, 10, 5, 100) },
                new List<FoldingFate.Core.Entity> { CreateEntity("e", EntityType.Monster, 5, 3, 50) });

            _turnSystem.StartTurn(battle);

            Assert.AreEqual(BattlePhase.PlayerTurn, battle.Phase.CurrentValue);
            battle.Dispose();
        }

        [Test]
        public void ExecuteTurn_AppliesDamageAndRecordsTurn()
        {
            var ally = CreateEntity("ally", EntityType.Character, 15, 5, 100);
            var enemy = CreateEntity("enemy", EntityType.Monster, 5, 3, 50);
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });

            _turnSystem.StartTurn(battle);
            var actions = new List<BattleAction>
            {
                new BattleAction(ally, BattleActionType.Attack, enemy)
            };
            _turnSystem.ExecuteTurn(battle, actions);

            // 15 attack - 3 defense = 12 damage, 50 - 12 = 38
            Assert.AreEqual(38f, enemy.Get<Health>().CurrentHp, 0.001f);
            Assert.AreEqual(1, battle.TurnHistory.Count);
            Assert.AreEqual(1, battle.TurnHistory[0].TurnNumber);
            battle.Dispose();
        }

        [Test]
        public void EndTurn_AllEnemiesDead_Victory()
        {
            var ally = CreateEntity("ally", EntityType.Character, 10, 5, 100);
            var enemy = CreateEntity("enemy", EntityType.Monster, 5, 3, 50);
            enemy.Get<Health>().CurrentHp = 0;
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });
            battle.Phase.Value = BattlePhase.PlayerTurn;

            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.Victory, battle.Phase.CurrentValue);
            battle.Dispose();
        }

        [Test]
        public void EndTurn_AllAlliesDead_Defeat()
        {
            var ally = CreateEntity("ally", EntityType.Character, 10, 5, 100);
            ally.Get<Health>().CurrentHp = 0;
            var enemy = CreateEntity("enemy", EntityType.Monster, 5, 3, 50);
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });
            battle.Phase.Value = BattlePhase.PlayerTurn;

            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.Defeat, battle.Phase.CurrentValue);
            battle.Dispose();
        }

        [Test]
        public void EndTurn_BothAlive_PlayerTurnToEnemyTurn()
        {
            var ally = CreateEntity("ally", EntityType.Character, 10, 5, 100);
            var enemy = CreateEntity("enemy", EntityType.Monster, 5, 3, 50);
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });
            battle.Phase.Value = BattlePhase.PlayerTurn;

            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.EnemyTurn, battle.Phase.CurrentValue);
            battle.Dispose();
        }

        [Test]
        public void EndTurn_BothAlive_EnemyTurnToPlayerTurn()
        {
            var ally = CreateEntity("ally", EntityType.Character, 10, 5, 100);
            var enemy = CreateEntity("enemy", EntityType.Monster, 5, 3, 50);
            var battle = new BattleModel("b1",
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });
            battle.Phase.Value = BattlePhase.EnemyTurn;

            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.PlayerTurn, battle.Phase.CurrentValue);
            battle.Dispose();
        }
    }
}
