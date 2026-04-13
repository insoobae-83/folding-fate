using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class ResolveSystemTests
    {
        private StatsSystem _statsSystem;
        private ResolveSystem _resolveSystem;

        [SetUp]
        public void SetUp()
        {
            _statsSystem = new StatsSystem();
            _resolveSystem = new ResolveSystem(_statsSystem);
        }

        private FoldingFate.Core.Entity CreateEntityWithStats(string id, float attack, float defense, float maxHp = 100f)
        {
            var entity = new FoldingFate.Core.Entity(id, EntityType.Character, id);
            var stats = new Stats();
            stats.BaseStats[EntityStatType.Attack] = attack;
            stats.BaseStats[EntityStatType.Defense] = defense;
            entity.Add(stats);
            var health = new Health(maxHp);
            entity.Add(health);
            return entity;
        }

        [Test]
        public void Resolve_Attack_ReturnsDamageResult()
        {
            var attacker = CreateEntityWithStats("attacker", 15f, 0f);
            var target = CreateEntityWithStats("target", 0f, 3f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            var results = _resolveSystem.Resolve(new List<BattleAction> { action });

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ActionResultType.Damage, results[0].ResultType);
            Assert.AreEqual(12f, results[0].Value, 0.001f);
        }

        [Test]
        public void Resolve_Attack_DefenseHigherThanAttack_ZeroDamage()
        {
            var attacker = CreateEntityWithStats("attacker", 3f, 0f);
            var target = CreateEntityWithStats("target", 0f, 10f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            var results = _resolveSystem.Resolve(new List<BattleAction> { action });

            Assert.AreEqual(0f, results[0].Value, 0.001f);
        }

        [Test]
        public void Resolve_Defend_ReturnsBuffResult()
        {
            var defender = CreateEntityWithStats("defender", 5f, 5f);
            var action = new BattleAction(defender, BattleActionType.Defend, defender);

            var results = _resolveSystem.Resolve(new List<BattleAction> { action });

            Assert.AreEqual(ActionResultType.Buff, results[0].ResultType);
            Assert.AreEqual(defender, results[0].Target);
        }

        [Test]
        public void Resolve_DoesNotModifyEntityState()
        {
            var attacker = CreateEntityWithStats("attacker", 15f, 0f, 100f);
            var target = CreateEntityWithStats("target", 0f, 3f, 50f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            _resolveSystem.Resolve(new List<BattleAction> { action });

            Assert.AreEqual(50f, target.Get<Health>().CurrentHp, 0.001f);
        }

        [Test]
        public void Resolve_MultipleActions_ReturnsSameCount()
        {
            var a = CreateEntityWithStats("a", 10f, 0f);
            var b = CreateEntityWithStats("b", 5f, 2f);
            var actions = new List<BattleAction>
            {
                new BattleAction(a, BattleActionType.Attack, b),
                new BattleAction(b, BattleActionType.Attack, a),
                new BattleAction(a, BattleActionType.Defend, a)
            };

            var results = _resolveSystem.Resolve(actions);

            Assert.AreEqual(3, results.Count);
        }

        [Test]
        public void Resolve_Skill_ReturnsMissResult()
        {
            var entity = CreateEntityWithStats("entity", 10f, 5f);
            var action = new BattleAction(entity, BattleActionType.Skill, entity);

            var results = _resolveSystem.Resolve(new List<BattleAction> { action });

            Assert.AreEqual(ActionResultType.Miss, results[0].ResultType);
        }
    }
}
