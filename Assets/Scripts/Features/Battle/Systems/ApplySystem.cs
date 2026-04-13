using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Battle.Systems
{
    public class ApplySystem
    {
        private readonly HealthSystem _healthSystem;

        public ApplySystem(HealthSystem healthSystem)
        {
            _healthSystem = healthSystem;
        }

        public void Apply(IReadOnlyList<ActionResult> results)
        {
            for (int i = 0; i < results.Count; i++)
                ApplyResult(results[i]);
        }

        public void ApplyResult(ActionResult result)
        {
            switch (result.ResultType)
            {
                case ActionResultType.Damage:
                    var health = result.Target.Get<Health>();
                    _healthSystem.TakeDamage(health, result.Value);
                    break;
                case ActionResultType.Heal:
                    var healTarget = result.Target.Get<Health>();
                    _healthSystem.Heal(healTarget, result.Value);
                    break;
            }
        }
    }
}
