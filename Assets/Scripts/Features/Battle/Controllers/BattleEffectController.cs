using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Components;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Controllers
{
    public class BattleEffectController : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _moveDistance = 1.5f;
        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private float _attackAnimDelay = 0.4f;
        [SerializeField] private float _delayBetweenActions = 0.3f;

        private readonly Dictionary<string, EntityView> _entityViews = new();

        public void RegisterEntityView(FoldingFate.Core.Entity entity, EntityView view)
        {
            _entityViews[entity.Id] = view;
        }

        public EntityView GetEntityView(FoldingFate.Core.Entity entity)
        {
            _entityViews.TryGetValue(entity.Id, out var view);
            return view;
        }

        public async UniTask PlayTurnEffects(IReadOnlyList<ActionResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                await PlayActionEffect(results[i]);
                if (i < results.Count - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(_delayBetweenActions), cancellationToken: destroyCancellationToken);
            }
        }

        public async UniTask PlayVictory(IReadOnlyList<FoldingFate.Core.Entity> winners)
        {
            for (int i = 0; i < winners.Count; i++)
            {
                var view = GetEntityView(winners[i]);
                if (view != null)
                {
                    var health = winners[i].Get<Health>();
                    if (health != null && health.IsAlive)
                        view.PlayVictory();
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: destroyCancellationToken);
        }

        private async UniTask PlayActionEffect(ActionResult result)
        {
            var actorView = GetEntityView(result.Source.Actor);
            var targetView = GetEntityView(result.Target);
            if (actorView == null || targetView == null) return;

            await actorView.MoveToward(targetView.transform.position, _moveDistance, _moveDuration);
            actorView.PlayAttack();
            await UniTask.Delay(TimeSpan.FromSeconds(_attackAnimDelay), cancellationToken: destroyCancellationToken);

            if (result.ResultType == ActionResultType.Damage && result.Value > 0)
            {
                targetView.PlayHit();
                targetView.UpdateHpBar();
            }

            await actorView.MoveBack(_moveDuration);

            var targetHealth = result.Target.Get<Health>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                targetView.PlayDeath();
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);
            }
        }
    }
}
