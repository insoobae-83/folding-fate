using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class RoomSystemTests
    {
        private RoomSystem _system;
        private RoomModel _room;

        [SetUp]
        public void SetUp()
        {
            _system = new RoomSystem();
            _room = new RoomModel("room-1", RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly());
        }

        [Test]
        public void Enter_SetsStateToActive()
        {
            _system.Enter(_room);
            Assert.AreEqual(RoomState.Active, _room.State.CurrentValue);
        }

        [Test]
        public void Clear_SetsStateToCleared()
        {
            _system.Clear(_room);
            Assert.AreEqual(RoomState.Cleared, _room.State.CurrentValue);
        }
    }
}
