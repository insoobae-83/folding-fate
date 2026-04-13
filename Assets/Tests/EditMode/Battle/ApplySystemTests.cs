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
    public class ApplySystemTests
    {
        private HealthSystem _healthSystem;
        private ApplySystem _applySystem;

        [SetUp]
        public void SetUp()
        {
            _healthSystem = new HealthSystem();
            _applySystem = new ApplySystem(_healthSystem);
        }

        private FoldingFate.Core.Entity CreateEntityWithHealth(string id, float maxHp)
        {
            var entity = new FoldingFate.Core.Entity(id, EntityType.Character, id);
            entity.Add(new Health(maxHp));
            return entity;
        }

        private BattleAction CreateDummyAction(FoldingFate.Core.Entity actor, FoldingFate.Core.Entity target)
        {
            return new BattleAction(actor, BattleActionType.Attack, target);
        }

        [Test]
        public void Apply_Damage_ReducesTargetHp()
        {
            var actor = CreateEntityWithHealth("actor", 100f);
            var target = CreateEntityWithHealth("target", 50f);
            var action = CreateDummyAction(actor, target);
            var result = new ActionResult(action, ActionResultType.Damage, target, 10f);

            _applySystem.Apply(new List<ActionResult> { result });

            Assert.AreEqual(40f, target.Get<Health>().CurrentHp, 0.001f);
        }

        [Test]
        public void Apply_Heal_IncreasesTargetHp()
        {
            var actor = CreateEntityWithHealth("actor", 100f);
            var target = CreateEntityWithHealth("target", 100f);
            target.Get<Health>().CurrentHp = 60f;
            var action = CreateDummyAction(actor, target);
            var result = new ActionResult(action, ActionResultType.Heal, target, 20f);

            _applySystem.Apply(new List<ActionResult> { result });

            Assert.AreEqual(80f, target.Get<Health>().CurrentHp, 0.001f);
        }

        [Test]
        public void Apply_Heal_DoesNotExceedMaxHp()
        {
            var actor = CreateEntityWithHealth("actor", 100f);
            var target = CreateEntityWithHealth("target", 100f);
            target.Get<Health>().CurrentHp = 95f;
            var action = CreateDummyAction(actor, target);
            var result = new ActionResult(action, ActionResultType.Heal, target, 20f);

            _applySystem.Apply(new List<ActionResult> { result });

            Assert.AreEqual(100f, target.Get<Health>().CurrentHp, 0.001f);
        }

        [Test]
        public void Apply_MultipleResults_AppliesSequentially()
        {
            var actor = CreateEntityWithHealth("actor", 100f);
            var target = CreateEntityWithHealth("target", 50f);
            var action = CreateDummyAction(actor, target);
            var results = new List<ActionResult>
            {
                new ActionResult(action, ActionResultType.Damage, target, 10f),
                new ActionResult(action, ActionResultType.Damage, target, 15f)
            };

            _applySystem.Apply(results);

            Assert.AreEqual(25f, target.Get<Health>().CurrentHp, 0.001f);
        }
    }
}
