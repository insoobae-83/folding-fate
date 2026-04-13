using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
    public class DungeonSystem
    {
        public class FloorConfig
        {
            public RoomType[] RoomTypes { get; }

            public FloorConfig(RoomType[] roomTypes)
            {
                RoomTypes = roomTypes ?? throw new ArgumentNullException(nameof(roomTypes));
            }
        }

        public DungeonModel Create(string displayName, IReadOnlyList<FloorConfig> floorConfigs)
        {
            var floors = new List<FloorModel>();
            for (int i = 0; i < floorConfigs.Count; i++)
            {
                var config = floorConfigs[i];
                var rooms = new List<RoomModel>();
                for (int j = 0; j < config.RoomTypes.Length; j++)
                {
                    var room = new RoomModel(
                        id: Guid.NewGuid().ToString(),
                        type: config.RoomTypes[j],
                        entities: new List<FoldingFate.Core.Entity>().AsReadOnly()
                    );
                    rooms.Add(room);
                }
                floors.Add(new FloorModel(i, rooms.AsReadOnly()));
            }
            return new DungeonModel(
                id: Guid.NewGuid().ToString(),
                displayName: displayName,
                floors: floors.AsReadOnly()
            );
        }
    }
}
