using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class BattleSystemTests
    {
        private BattleSystem _battleSystem;

        [SetUp]
        public void SetUp()
        {
            _battleSystem = new BattleSystem();
        }

        private FoldingFate.Core.Entity CreateEntity(string id, EntityType type)
        {
            var entity = new FoldingFate.Core.Entity(id, type, id);
            entity.Add(new Health(100f));
            entity.Add(new Combat());
            return entity;
        }

        [Test]
        public void StartBattle_CreatesBattleModel()
        {
            var ally = CreateEntity("ally", EntityType.Character);
            var enemy = CreateEntity("enemy", EntityType.Monster);

            var battle = _battleSystem.StartBattle(
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });

            Assert.IsNotNull(battle);
            Assert.IsNotNull(battle.Id);
            Assert.AreEqual(1, battle.Allies.Count);
            Assert.AreEqual(1, battle.Enemies.Count);
            battle.Dispose();
        }

        [Test]
        public void StartBattle_SetsIsInCombatTrue()
        {
            var ally = CreateEntity("ally", EntityType.Character);
            var enemy = CreateEntity("enemy", EntityType.Monster);

            var battle = _battleSystem.StartBattle(
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });

            Assert.IsTrue(ally.Get<Combat>().IsInCombat);
            Assert.IsTrue(enemy.Get<Combat>().IsInCombat);
            battle.Dispose();
        }

        [Test]
        public void EndBattle_SetsIsInCombatFalse()
        {
            var ally = CreateEntity("ally", EntityType.Character);
            var enemy = CreateEntity("enemy", EntityType.Monster);

            var battle = _battleSystem.StartBattle(
                new List<FoldingFate.Core.Entity> { ally },
                new List<FoldingFate.Core.Entity> { enemy });

            _battleSystem.EndBattle(battle);

            Assert.IsFalse(ally.Get<Combat>().IsInCombat);
            Assert.IsFalse(enemy.Get<Combat>().IsInCombat);
        }
    }
}
