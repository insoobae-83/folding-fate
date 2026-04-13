using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Dungeon.Models
{
    public class FloorModel : IDisposable
    {
        public int Index { get; }
        public IReadOnlyList<RoomModel> Rooms { get; }
        public ReactiveProperty<int> CurrentRoomIndex { get; }

        public RoomModel CurrentRoom => Rooms[CurrentRoomIndex.CurrentValue];
        public bool IsCleared
        {
            get
            {
                for (int i = 0; i < Rooms.Count; i++)
                {
                    if (Rooms[i].State.CurrentValue != RoomState.Cleared)
                        return false;
                }
                return true;
            }
        }

        public FloorModel(int index, IReadOnlyList<RoomModel> rooms)
        {
            Index = index;
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            CurrentRoomIndex = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            CurrentRoomIndex?.Dispose();
        }
    }
}
