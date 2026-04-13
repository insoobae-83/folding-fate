using NUnit.Framework;
using FoldingFate.Core;

namespace FoldingFate.Tests.EditMode.Entity
{
    public class DummyComponent : IEntityComponent
    {
        public Core.Entity Owner { get; set; }
        public int Value { get; set; }
    }

    public class AnotherDummyComponent : IEntityComponent
    {
        public Core.Entity Owner { get; set; }
    }

    [TestFixture]
    public class EntityTests
    {
        private Core.Entity _entity;

        [SetUp]
        public void SetUp()
        {
            _entity = new Core.Entity("test-id", EntityType.Character, "TestEntity");
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            Assert.AreEqual("test-id", _entity.Id);
            Assert.AreEqual(EntityType.Character, _entity.Type);
            Assert.AreEqual("TestEntity", _entity.DisplayName);
        }

        [Test]
        public void Add_ThenGet_ReturnsComponent()
        {
            var comp = new DummyComponent { Value = 42 };
            _entity.Add(comp);
            var result = _entity.Get<DummyComponent>();
            Assert.IsNotNull(result);
            Assert.AreEqual(42, result.Value);
        }

        [Test]
        public void Add_SetsOwner()
        {
            var comp = new DummyComponent();
            _entity.Add(comp);
            Assert.AreEqual(_entity, comp.Owner);
        }

        [Test]
        public void Has_ReturnsTrueAfterAdd()
        {
            _entity.Add(new DummyComponent());
            Assert.IsTrue(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Has_ReturnsFalseBeforeAdd()
        {
            Assert.IsFalse(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Get_ReturnsNullWhenNotAdded()
        {
            Assert.IsNull(_entity.Get<DummyComponent>());
        }

        [Test]
        public void Remove_ReturnsTrueAndClearsOwner()
        {
            var comp = new DummyComponent();
            _entity.Add(comp);
            var removed = _entity.Remove<DummyComponent>();
            Assert.IsTrue(removed);
            Assert.IsNull(comp.Owner);
            Assert.IsFalse(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Remove_ReturnsFalseWhenNotPresent()
        {
            Assert.IsFalse(_entity.Remove<DummyComponent>());
        }

        [Test]
        public void Add_OverwritesPreviousComponent()
        {
            var oldComp = new DummyComponent { Value = 1 };
            _entity.Add(oldComp);
            _entity.Add(new DummyComponent { Value = 2 });
            Assert.AreEqual(2, _entity.Get<DummyComponent>().Value);
            Assert.IsNull(oldComp.Owner);
        }

        [Test]
        public void MultipleComponentTypes_Independent()
        {
            _entity.Add(new DummyComponent());
            _entity.Add(new AnotherDummyComponent());
            Assert.IsTrue(_entity.Has<DummyComponent>());
            Assert.IsTrue(_entity.Has<AnotherDummyComponent>());
        }
        [Test]
        public void Constructor_ThrowsOnNullId()
        {
            Assert.Throws<System.ArgumentNullException>(() => new Core.Entity(null, EntityType.Character, "Name"));
        }

        [Test]
        public void Constructor_ThrowsOnNullDisplayName()
        {
            Assert.Throws<System.ArgumentNullException>(() => new Core.Entity("id", EntityType.Character, null));
        }
    }
}
