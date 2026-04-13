using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class BattleModelTests
    {
        private FoldingFate.Core.Entity _ally;
        private FoldingFate.Core.Entity _enemy;
        private BattleModel _battle;

        [SetUp]
        public void SetUp()
        {
            _ally = new FoldingFate.Core.Entity("ally-1", EntityType.Character, "Hero");
            _enemy = new FoldingFate.Core.Entity("enemy-1", EntityType.Monster, "Goblin");
            _battle = new BattleModel(
                "battle-1",
                new List<FoldingFate.Core.Entity> { _ally },
                new List<FoldingFate.Core.Entity> { _enemy });
        }

        [TearDown]
        public void TearDown()
        {
            _battle?.Dispose();
        }

        [Test]
        public void Constructor_DefaultPhaseStart()
        {
            Assert.AreEqual(BattlePhase.Start, _battle.Phase.CurrentValue);
        }

        [Test]
        public void Constructor_DefaultTurnCountZero()
        {
            Assert.AreEqual(0, _battle.TurnCount.CurrentValue);
        }

        [Test]
        public void Constructor_TurnHistoryEmpty()
        {
            Assert.AreEqual(0, _battle.TurnHistory.Count);
        }

        [Test]
        public void Allies_Accessible()
        {
            Assert.AreEqual(1, _battle.Allies.Count);
            Assert.AreEqual(_ally, _battle.Allies[0]);
        }

        [Test]
        public void Enemies_Accessible()
        {
            Assert.AreEqual(1, _battle.Enemies.Count);
            Assert.AreEqual(_enemy, _battle.Enemies[0]);
        }

        [Test]
        public void TurnRecord_PreservesData()
        {
            var action = new BattleAction(_ally, BattleActionType.Attack, _enemy);
            var result = new ActionResult(action, ActionResultType.Damage, _enemy, 10f);
            var actions = new List<BattleAction> { action };
            var results = new List<ActionResult> { result };

            var record = new TurnRecord(1, actions, results);

            Assert.AreEqual(1, record.TurnNumber);
            Assert.AreEqual(1, record.Actions.Count);
            Assert.AreEqual(action, record.Actions[0]);
            Assert.AreEqual(1, record.Results.Count);
            Assert.AreEqual(result, record.Results[0]);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _battle.Dispose());
        }

        [Test]
        public void Constructor_ThrowsOnNullId()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new BattleModel(null, new List<FoldingFate.Core.Entity>(), new List<FoldingFate.Core.Entity>()));
        }

        [Test]
        public void Constructor_ThrowsOnNullAllies()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new BattleModel("id", null, new List<FoldingFate.Core.Entity>()));
        }

        [Test]
        public void Constructor_ThrowsOnNullEnemies()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new BattleModel("id", new List<FoldingFate.Core.Entity>(), null));
        }
    }
}
