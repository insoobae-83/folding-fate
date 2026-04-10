using NUnit.Framework;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class DeckModelTests
    {
        private DeckModel _deck;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _deck.Initialize();
        }

        [Test]
        public void Initialize_Creates52Cards()
        {
            Assert.AreEqual(52, _deck.RemainingCount.Value);
        }

        [Test]
        public void Draw_ReturnsRequestedCount_AndReducesRemaining()
        {
            var drawn = _deck.Draw(5);
            Assert.AreEqual(5, drawn.Count);
            Assert.AreEqual(47, _deck.RemainingCount.Value);
        }

        [Test]
        public void Draw_WhenNotEnoughCards_AutoResetsAndRedraws()
        {
            _deck.Draw(50);
            Assert.AreEqual(2, _deck.RemainingCount.Value);

            var drawn = _deck.Draw(5);
            Assert.AreEqual(5, drawn.Count);
            Assert.AreEqual(47, _deck.RemainingCount.Value);
        }

        [Test]
        public void Shuffle_ChangesCardOrder()
        {
            var deck2 = new DeckModel();
            deck2.Initialize();

            var drawn1 = _deck.Draw(10);
            var drawn2 = deck2.Draw(10);

            bool allSame = true;
            for (int i = 0; i < 10; i++)
                if (drawn1[i].Id != drawn2[i].Id) { allSame = false; break; }

            Assert.IsFalse(allSame, "두 독립 셔플의 결과가 동일해서는 안 됩니다");
        }

        [Test]
        public void Draw_ContainsAllFourSuitsAndThirteenRanks()
        {
            var all = _deck.Draw(52);
            var suits = new System.Collections.Generic.HashSet<string>();
            var ranks = new System.Collections.Generic.HashSet<string>();
            foreach (var c in all)
            {
                suits.Add(c.Suit.ToString());
                ranks.Add(c.Rank.ToString());
            }
            Assert.AreEqual(4, suits.Count);
            Assert.AreEqual(13, ranks.Count);
        }
    }
}
