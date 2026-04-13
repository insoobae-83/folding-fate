using System.Collections.Generic;

namespace FoldingFate.Features.Battle.Events
{
    public readonly struct CombatStartEvent
    {
        public IReadOnlyList<FoldingFate.Core.Entity> Allies { get; }
        public IReadOnlyList<FoldingFate.Core.Entity> Enemies { get; }

        public CombatStartEvent(IReadOnlyList<FoldingFate.Core.Entity> allies, IReadOnlyList<FoldingFate.Core.Entity> enemies)
        {
            Allies = allies;
            Enemies = enemies;
        }
    }
}
