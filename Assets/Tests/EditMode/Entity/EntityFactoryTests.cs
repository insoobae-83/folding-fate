using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class EntityFactoryTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
        }

        [Test]
        public void CreateCombatEntity_HasStatsHealthCombat()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            Assert.AreEqual("hero", entity.Id);
            Assert.AreEqual(EntityType.Character, entity.Type);
            Assert.AreEqual("Hero", entity.DisplayName);
            Assert.IsTrue(entity.Has<Stats>());
            Assert.IsTrue(entity.Has<Health>());
            Assert.IsTrue(entity.Has<Combat>());
        }

        [Test]
        public void CreateCombatEntity_StatsInitialized()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            var stats = entity.Get<Stats>();
            Assert.AreEqual(10f, stats.BaseStats[EntityStatType.Attack]);
            Assert.AreEqual(5f, stats.BaseStats[EntityStatType.Defense]);
        }

        [Test]
        public void CreateCombatEntity_HealthInitialized()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            var health = entity.Get<Health>();
            Assert.AreEqual(100f, health.MaxHp);
            Assert.AreEqual(100f, health.CurrentHp);
        }

        [Test]
        public void CreateCombatEntity_CombatNotInCombat()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            Assert.IsFalse(entity.Get<Combat>().IsInCombat);
        }

        [Test]
        public void CreateCombatEntity_Monster()
        {
            var entity = _factory.CreateCombatEntity("slime", EntityType.Monster, "Slime", 50f, 8f, 3f);
            Assert.AreEqual(EntityType.Monster, entity.Type);
            Assert.AreEqual("Slime", entity.DisplayName);
        }
    }
}