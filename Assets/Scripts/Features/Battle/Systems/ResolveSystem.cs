using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Battle.Systems
{
    public class ResolveSystem
    {
        private readonly StatsSystem _statsSystem;

        public ResolveSystem(StatsSystem statsSystem)
        {
            _statsSystem = statsSystem ?? throw new ArgumentNullException(nameof(statsSystem));
        }

        public IReadOnlyList<ActionResult> Resolve(IReadOnlyList<BattleAction> actions)
        {
            var results = new List<ActionResult>();
            for (int i = 0; i < actions.Count; i++)
                results.Add(ResolveAction(actions[i]));
            return results.AsReadOnly();
        }

        private ActionResult ResolveAction(BattleAction action)
        {
            switch (action.ActionType)
            {
                case BattleActionType.Attack:
                    var attackerStats = action.Actor.Get<Stats>();
                    var targetStats = action.Target.Get<Stats>();
                    var attack = _statsSystem.GetValue(attackerStats, EntityStatType.Attack);
                    var defense = _statsSystem.GetValue(targetStats, EntityStatType.Defense);
                    var damage = Math.Max(0f, attack - defense);
                    return new ActionResult(action, ActionResultType.Damage, action.Target, damage);
                case BattleActionType.Defend:
                    return new ActionResult(action, ActionResultType.Buff, action.Actor, 0f);
                default:
                    return new ActionResult(action, ActionResultType.Miss, action.Target, 0f);
            }
        }
    }
}
