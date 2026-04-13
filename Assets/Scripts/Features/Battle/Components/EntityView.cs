using Cysharp.Threading.Tasks;
using UnityEngine;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Components
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private WorldHpBar _hpBar;

        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");
        private static readonly int VictoryTrigger = Animator.StringToHash("Victory");

        public FoldingFate.Core.Entity Entity { get; private set; }
        private Vector3 _originalPosition;

        public void Bind(FoldingFate.Core.Entity entity)
        {
            Entity = entity;
            _originalPosition = transform.position;
            UpdateHpBar();
        }

        public void PlayAttack() => _animator.SetTrigger(AttackTrigger);
        public void PlayHit() => _animator.SetTrigger(HitTrigger);
        public void PlayDeath() => _animator.SetTrigger(DeathTrigger);
        public void PlayVictory() => _animator.SetTrigger(VictoryTrigger);

        public void UpdateHpBar()
        {
            var health = Entity?.Get<Health>();
            if (health != null && _hpBar != null)
                _hpBar.SetValue(health);
        }

        public async UniTask MoveToward(Vector3 targetPosition, float distance, float duration)
        {
            var direction = (targetPosition - _originalPosition).normalized;
            var destination = _originalPosition + direction * distance;
            await LerpPosition(destination, duration);
        }

        public async UniTask MoveBack(float duration)
        {
            await LerpPosition(_originalPosition, duration);
        }

        private async UniTask LerpPosition(Vector3 target, float duration)
        {
            var start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                await UniTask.Yield();
            }
            transform.position = target;
        }
    }
}
