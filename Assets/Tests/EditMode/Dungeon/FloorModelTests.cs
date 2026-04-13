using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class FloorModelTests
    {
        private RoomModel CreateRoom(string id = "room-1", RoomType type = RoomType.Combat)
        {
            return new RoomModel(id, type, new List<FoldingFate.Core.Entity>().AsReadOnly());
        }

        [Test]
        public void Constructor_DefaultCurrentRoomIndexZero()
        {
            var floor = new FloorModel(0, new List<RoomModel> { CreateRoom() }.AsReadOnly());
            Assert.AreEqual(0, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void Constructor_SetsIndex()
        {
            var floor = new FloorModel(3, new List<RoomModel> { CreateRoom() }.AsReadOnly());
            Assert.AreEqual(3, floor.Index);
        }

        [Test]
        public void CurrentRoom_ReturnsCorrectRoom()
        {
            var room0 = CreateRoom("r0");
            var room1 = CreateRoom("r1");
            var floor = new FloorModel(0, new List<RoomModel> { room0, room1 }.AsReadOnly());
            Assert.AreEqual(room0, floor.CurrentRoom);
            floor.CurrentRoomIndex.Value = 1;
            Assert.AreEqual(room1, floor.CurrentRoom);
        }

        [Test]
        public void IsCleared_FalseWhenNotAllCleared()
        {
            var room0 = CreateRoom("r0");
            var room1 = CreateRoom("r1");
            var floor = new FloorModel(0, new List<RoomModel> { room0, room1 }.AsReadOnly());
            room0.State.Value = RoomState.Cleared;
            Assert.IsFalse(floor.IsCleared);
        }

        [Test]
        public void IsCleared_TrueWhenAllCleared()
        {
            var room0 = CreateRoom("r0");
            var room1 = CreateRoom("r1");
            var floor = new FloorModel(0, new List<RoomModel> { room0, room1 }.AsReadOnly());
            room0.State.Value = RoomState.Cleared;
            room1.State.Value = RoomState.Cleared;
            Assert.IsTrue(floor.IsCleared);
        }

        [Test]
        public void Constructor_ThrowsOnNullRooms()
        {
            Assert.Throws<System.ArgumentNullException>(() => new FloorModel(0, null));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var floor = new FloorModel(0, new List<RoomModel> { CreateRoom() }.AsReadOnly());
            Assert.DoesNotThrow(() => floor.Dispose());
        }
    }
}
