using System.Collections.Generic;

namespace FoldingFate.Features.Battle.Models
{
    public class TurnRecord
    {
        public int TurnNumber { get; }
        public IReadOnlyList<BattleAction> Actions { get; }
        public IReadOnlyList<ActionResult> Results { get; }

        public TurnRecord(int turnNumber, IReadOnlyList<BattleAction> actions, IReadOnlyList<ActionResult> results)
        {
            TurnNumber = turnNumber;
            Actions = actions;
            Results = results;
        }
    }
}
