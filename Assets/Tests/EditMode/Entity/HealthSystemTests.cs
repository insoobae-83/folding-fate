using NUnit.Framework;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class HealthSystemTests
    {
        private HealthSystem _system;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _system = new HealthSystem();
            _health = new Health(100f);
        }

        [Test]
        public void Health_Constructor_SetsCurrentHpToMax()
        {
            Assert.AreEqual(100f, _health.CurrentHp);
            Assert.AreEqual(100f, _health.MaxHp);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _system.TakeDamage(_health, 30f);
            Assert.AreEqual(70f, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_DoesNotGoBelowZero()
        {
            _system.TakeDamage(_health, 150f);
            Assert.AreEqual(0f, _health.CurrentHp);
            Assert.IsTrue(_health.IsDead);
        }

        [Test]
        public void Heal_IncreasesHp()
        {
            _system.TakeDamage(_health, 50f);
            _system.Heal(_health, 20f);
            Assert.AreEqual(70f, _health.CurrentHp);
        }

        [Test]
        public void Heal_DoesNotExceedMaxHp()
        {
            _system.TakeDamage(_health, 10f);
            _system.Heal(_health, 50f);
            Assert.AreEqual(100f, _health.CurrentHp);
        }

        [Test]
        public void SetMaxHp_ClampsCurrentHp()
        {
            _system.SetMaxHp(_health, 50f);
            Assert.AreEqual(50f, _health.MaxHp);
            Assert.AreEqual(50f, _health.CurrentHp);
        }

        [Test]
        public void SetMaxHp_DoesNotClampWhenCurrentIsLower()
        {
            _system.TakeDamage(_health, 80f);
            _system.SetMaxHp(_health, 50f);
            Assert.AreEqual(50f, _health.MaxHp);
            Assert.AreEqual(20f, _health.CurrentHp);
        }
    }
}