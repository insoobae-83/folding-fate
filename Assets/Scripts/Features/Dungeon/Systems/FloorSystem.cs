using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
    public class FloorSystem
    {
        public void MoveToNextRoom(FloorModel floor)
        {
            var next = floor.CurrentRoomIndex.CurrentValue + 1;
            if (next < floor.Rooms.Count)
                floor.CurrentRoomIndex.Value = next;
        }

        public void MoveToNextFloor(DungeonModel dungeon)
        {
            var next = dungeon.CurrentFloorIndex.CurrentValue + 1;
            if (next < dungeon.Floors.Count)
                dungeon.CurrentFloorIndex.Value = next;
        }
    }
}
