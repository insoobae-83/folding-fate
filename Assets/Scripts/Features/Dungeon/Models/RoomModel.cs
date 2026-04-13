using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Dungeon.Models
{
    public class RoomModel : IDisposable
    {
        public string Id { get; }
        public RoomType Type { get; }
        public ReactiveProperty<RoomState> State { get; }
        public IReadOnlyList<FoldingFate.Core.Entity> Entities { get; }

        public RoomModel(string id, RoomType type, IReadOnlyList<FoldingFate.Core.Entity> entities)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            State = new ReactiveProperty<RoomState>(RoomState.Locked);
            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void Dispose()
        {
            State?.Dispose();
        }
    }
}
