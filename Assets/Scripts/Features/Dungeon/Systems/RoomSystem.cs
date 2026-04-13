using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
    public class RoomSystem
    {
        public void Enter(RoomModel room)
        {
            room.State.Value = RoomState.Active;
        }

        public void Clear(RoomModel room)
        {
            room.State.Value = RoomState.Cleared;
        }
    }
}
