using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class DungeonSystemTests
    {
        private DungeonSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new DungeonSystem();
        }

        [Test]
        public void CreateFromConfig_ReturnsCorrectStructure()
        {
            var configs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Shop }),
                new DungeonSystem.FloorConfig(new[] { RoomType.Boss })
            };
            var dungeon = _system.Create("Test Dungeon", configs);
            Assert.AreEqual("Test Dungeon", dungeon.DisplayName);
            Assert.AreEqual(2, dungeon.Floors.Count);
            Assert.AreEqual(2, dungeon.Floors[0].Rooms.Count);
            Assert.AreEqual(1, dungeon.Floors[1].Rooms.Count);
        }

        [Test]
        public void CreateFromConfig_RoomTypesMatchConfig()
        {
            var configs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Shop, RoomType.Boss })
            };
            var dungeon = _system.Create("Test", configs);
            Assert.AreEqual(RoomType.Combat, dungeon.Floors[0].Rooms[0].Type);
            Assert.AreEqual(RoomType.Shop, dungeon.Floors[0].Rooms[1].Type);
            Assert.AreEqual(RoomType.Boss, dungeon.Floors[0].Rooms[2].Type);
        }

        [Test]
        public void CreateFromConfig_FloorIndicesCorrect()
        {
            var configs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat }),
                new DungeonSystem.FloorConfig(new[] { RoomType.Boss })
            };
            var dungeon = _system.Create("Test", configs);
            Assert.AreEqual(0, dungeon.Floors[0].Index);
            Assert.AreEqual(1, dungeon.Floors[1].Index);
        }

        [Test]
        public void CreateFromConfig_RoomsHaveUniqueIds()
        {
            var configs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Shop }),
                new DungeonSystem.FloorConfig(new[] { RoomType.Boss })
            };
            var dungeon = _system.Create("Test", configs);
            var allIds = dungeon.Floors
                .SelectMany(f => f.Rooms)
                .Select(r => r.Id)
                .ToList();
            Assert.AreEqual(allIds.Count, allIds.Distinct().Count());
        }
    }
}
