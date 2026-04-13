using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class DungeonModelTests
    {
        private FloorModel CreateFloor(int index = 0)
        {
            var room = new RoomModel("room-1", RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly());
            return new FloorModel(index, new List<RoomModel> { room }.AsReadOnly());
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            var floors = new List<FloorModel> { CreateFloor() }.AsReadOnly();
            var dungeon = new DungeonModel("d1", "Test Dungeon", floors);
            Assert.AreEqual("d1", dungeon.Id);
            Assert.AreEqual("Test Dungeon", dungeon.DisplayName);
            Assert.AreEqual(1, dungeon.Floors.Count);
        }

        [Test]
        public void Constructor_DefaultFloorIndexZero()
        {
            var dungeon = new DungeonModel("d1", "Test Dungeon",
                new List<FloorModel> { CreateFloor() }.AsReadOnly());
            Assert.AreEqual(0, dungeon.CurrentFloorIndex.CurrentValue);
        }

        [Test]
        public void CurrentFloor_ReturnsCorrectFloor()
        {
            var floor0 = CreateFloor(0);
            var floor1 = CreateFloor(1);
            var dungeon = new DungeonModel("d1", "Test Dungeon",
                new List<FloorModel> { floor0, floor1 }.AsReadOnly());
            Assert.AreEqual(floor0, dungeon.CurrentFloor);
            dungeon.CurrentFloorIndex.Value = 1;
            Assert.AreEqual(floor1, dungeon.CurrentFloor);
        }

        [Test]
        public void Constructor_ThrowsOnNullId()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new DungeonModel(null, "name", new List<FloorModel> { CreateFloor() }.AsReadOnly()));
        }

        [Test]
        public void Constructor_ThrowsOnNullDisplayName()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new DungeonModel("id", null, new List<FloorModel> { CreateFloor() }.AsReadOnly()));
        }

        [Test]
        public void Constructor_ThrowsOnNullFloors()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new DungeonModel("id", "name", null));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var dungeon = new DungeonModel("d1", "Test Dungeon",
                new List<FloorModel> { CreateFloor() }.AsReadOnly());
            Assert.DoesNotThrow(() => dungeon.Dispose());
        }
    }
}
