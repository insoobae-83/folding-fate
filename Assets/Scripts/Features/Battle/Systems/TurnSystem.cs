using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Systems
{
    public class TurnSystem
    {
        private readonly ResolveSystem _resolveSystem;
        private readonly ApplySystem _applySystem;

        public TurnSystem(ResolveSystem resolveSystem, ApplySystem applySystem)
        {
            _resolveSystem = resolveSystem;
            _applySystem = applySystem;
        }

        public void StartTurn(BattleModel battle)
        {
            battle.TurnCount.Value++;
            battle.Phase.Value = BattlePhase.PlayerTurn;
        }

        public void ExecuteTurn(BattleModel battle, IReadOnlyList<BattleAction> actions)
        {
            var results = _resolveSystem.Resolve(actions);
            _applySystem.Apply(results);
            battle.TurnHistory.Add(new TurnRecord(battle.TurnCount.CurrentValue, actions, results));
        }

        public void EndTurn(BattleModel battle)
        {
            if (AllDead(battle.Enemies))
            {
                battle.Phase.Value = BattlePhase.Victory;
                return;
            }

            if (AllDead(battle.Allies))
            {
                battle.Phase.Value = BattlePhase.Defeat;
                return;
            }

            battle.Phase.Value = battle.Phase.CurrentValue == BattlePhase.PlayerTurn
                ? BattlePhase.EnemyTurn
                : BattlePhase.PlayerTurn;
        }

        private static bool AllDead(IReadOnlyList<FoldingFate.Core.Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var health = entities[i].Get<Health>();
                if (health != null && health.IsAlive)
                    return false;
            }
            return true;
        }
    }
}
