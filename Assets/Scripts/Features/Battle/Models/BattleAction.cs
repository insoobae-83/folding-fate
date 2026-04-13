using System;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
    public class BattleAction
    {
        public FoldingFate.Core.Entity Actor { get; }
        public BattleActionType ActionType { get; }
        public FoldingFate.Core.Entity Target { get; }

        public BattleAction(FoldingFate.Core.Entity actor, BattleActionType actionType, FoldingFate.Core.Entity target)
        {
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            ActionType = actionType;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }
    }
}
