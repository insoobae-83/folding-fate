using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class FloorSystemTests
    {
        private FloorSystem _system;

        private RoomModel CreateRoom(string id = "room-1")
        {
            return new RoomModel(id, RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly());
        }

        [SetUp]
        public void SetUp()
        {
            _system = new FloorSystem();
        }

        [Test]
        public void MoveToNextRoom_IncrementsIndex()
        {
            var floor = new FloorModel(0, new List<RoomModel> { CreateRoom("r0"), CreateRoom("r1") }.AsReadOnly());
            _system.MoveToNextRoom(floor);
            Assert.AreEqual(1, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextRoom_AtLastRoom_DoesNotIncrement()
        {
            var floor = new FloorModel(0, new List<RoomModel> { CreateRoom("r0") }.AsReadOnly());
            _system.MoveToNextRoom(floor);
            Assert.AreEqual(0, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextFloor_IncrementsIndex()
        {
            var floor0 = new FloorModel(0, new List<RoomModel> { CreateRoom("r0") }.AsReadOnly());
            var floor1 = new FloorModel(1, new List<RoomModel> { CreateRoom("r1") }.AsReadOnly());
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { floor0, floor1 }.AsReadOnly());
            _system.MoveToNextFloor(dungeon);
            Assert.AreEqual(1, dungeon.CurrentFloorIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextFloor_AtLastFloor_DoesNotIncrement()
        {
            var floor0 = new FloorModel(0, new List<RoomModel> { CreateRoom("r0") }.AsReadOnly());
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { floor0 }.AsReadOnly());
            _system.MoveToNextFloor(dungeon);
            Assert.AreEqual(0, dungeon.CurrentFloorIndex.CurrentValue);
        }
    }
}
