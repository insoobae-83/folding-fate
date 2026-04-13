using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Structs;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class StatsSystemTests
    {
        private StatsSystem _system;
        private Stats _stats;

        [SetUp]
        public void SetUp()
        {
            _system = new StatsSystem();
            _stats = new Stats();
            _stats.BaseStats[EntityStatType.Attack] = 10f;
            _stats.BaseStats[EntityStatType.Defense] = 5f;
        }

        [Test]
        public void GetValue_ReturnsBaseValue()
        {
            Assert.AreEqual(10f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void GetValue_UnsetStat_ReturnsZero()
        {
            Assert.AreEqual(0f, _system.GetValue(_stats, EntityStatType.Speed));
        }

        [Test]
        public void GetValue_WithModifier_ReturnsBaseAndModifierSum()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-1"));
            Assert.AreEqual(13f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void GetValue_MultipleModifiers_SumsAll()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, -2f, ModifierSource.Debuff, "debuff-1"));
            Assert.AreEqual(11f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void RemoveModifiersBySourceId_RemovesOnlyMatching()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 5f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-2"));
            _system.RemoveModifiersBySourceId(_stats, "buff-1");
            Assert.AreEqual(13f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void RemoveModifiersBySource_RemovesAllOfSource()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 5f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-2"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Defense, 2f, ModifierSource.Equipment, "equip-1"));
            _system.RemoveModifiersBySource(_stats, ModifierSource.Buff);
            Assert.AreEqual(10f, _system.GetValue(_stats, EntityStatType.Attack));
            Assert.AreEqual(7f, _system.GetValue(_stats, EntityStatType.Defense));
        }
    }
}