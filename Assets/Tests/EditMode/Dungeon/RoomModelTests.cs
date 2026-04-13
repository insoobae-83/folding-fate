using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class RoomModelTests
    {
        [Test]
        public void Constructor_DefaultStateLocked()
        {
            var room = new RoomModel("room-1", RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly());
            Assert.AreEqual(RoomState.Locked, room.State.CurrentValue);
        }

        [Test]
        public void Constructor_SetsTypeAndId()
        {
            var room = new RoomModel("room-1", RoomType.Shop, new List<FoldingFate.Core.Entity>().AsReadOnly());
            Assert.AreEqual("room-1", room.Id);
            Assert.AreEqual(RoomType.Shop, room.Type);
        }

        [Test]
        public void Entities_AccessibleAfterCreation()
        {
            var entity = new FoldingFate.Core.Entity("e1", EntityType.Character, "Hero");
            var entities = new List<FoldingFate.Core.Entity> { entity }.AsReadOnly();
            var room = new RoomModel("room-1", RoomType.Combat, entities);
            Assert.AreEqual(1, room.Entities.Count);
            Assert.AreEqual(entity, room.Entities[0]);
        }

        [Test]
        public void Constructor_ThrowsOnNullId()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new RoomModel(null, RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly()));
        }

        [Test]
        public void Constructor_ThrowsOnNullEntities()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new RoomModel("room-1", RoomType.Combat, null));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var room = new RoomModel("room-1", RoomType.Combat, new List<FoldingFate.Core.Entity>().AsReadOnly());
            Assert.DoesNotThrow(() => room.Dispose());
        }
    }
}
