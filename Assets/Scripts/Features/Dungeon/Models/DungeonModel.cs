using System;
using System.Collections.Generic;
using R3;

namespace FoldingFate.Features.Dungeon.Models
{
    public class DungeonModel : IDisposable
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<FloorModel> Floors { get; }
        public ReactiveProperty<int> CurrentFloorIndex { get; }

        public FloorModel CurrentFloor => Floors[CurrentFloorIndex.CurrentValue];

        public DungeonModel(string id, string displayName, IReadOnlyList<FloorModel> floors)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Floors = floors ?? throw new ArgumentNullException(nameof(floors));
            CurrentFloorIndex = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            CurrentFloorIndex?.Dispose();
        }
    }
}
