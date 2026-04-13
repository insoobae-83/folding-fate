using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
    public class BattleModel : IDisposable
    {
        public string Id { get; }
        public IReadOnlyList<FoldingFate.Core.Entity> Allies { get; }
        public IReadOnlyList<FoldingFate.Core.Entity> Enemies { get; }
        public ReactiveProperty<BattlePhase> Phase { get; }
        public ReactiveProperty<int> TurnCount { get; }
        public List<TurnRecord> TurnHistory { get; } = new();

        public BattleModel(string id, IReadOnlyList<FoldingFate.Core.Entity> allies, IReadOnlyList<FoldingFate.Core.Entity> enemies)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Allies = allies ?? throw new ArgumentNullException(nameof(allies));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            Phase = new ReactiveProperty<BattlePhase>(BattlePhase.Start);
            TurnCount = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            Phase?.Dispose();
            TurnCount?.Dispose();
        }
    }
}
