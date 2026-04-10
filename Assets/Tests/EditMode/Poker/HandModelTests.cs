using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class HandModelTests
    {
        private HandModel _hand;

        private static BaseCard MakeCard(Suit suit, Rank rank) =>
            new BaseCard($"{suit}_{rank}", CardCategory.Standard, suit, rank, "", "");

        [SetUp]
        public void SetUp() => _hand = new HandModel(maxHandSize: 8);

        [TearDown]
        public void TearDown() => _hand.Dispose();

        [Test]
        public void InitialState_IsEmpty()
        {
            Assert.AreEqual(0, _hand.Cards.Value.Count);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
            Assert.IsFalse(_hand.IsFull);
        }

        [Test]
        public void AddCards_UpdatesCardsAndCount()
        {
            var cards = new List<BaseCard>
            {
                MakeCard(Suit.Spade, Rank.Ace),
                MakeCard(Suit.Heart, Rank.King)
            };
            _hand.AddCards(cards);
            Assert.AreEqual(2, _hand.Cards.Value.Count);
        }

        [Test]
        public void IsFull_TrueWhenAtMaxHandSize()
        {
            var cards = new List<BaseCard>();
            for (int i = 0; i < 8; i++)
                cards.Add(MakeCard(Suit.Spade, Rank.Ace));
            _hand.AddCards(cards);
            Assert.IsTrue(_hand.IsFull);
        }

        [Test]
        public void ToggleSelect_SelectsCard()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            Assert.AreEqual(1, _hand.SelectedIndices.Value.Count);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_DeselectsAlreadySelectedCard()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            _hand.ToggleSelect(0);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
        }

        [Test]
        public void Clear_RemovesAllCardsAndSelections()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            _hand.Clear();
            Assert.AreEqual(0, _hand.Cards.Value.Count);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
        }

        [Test]
        public void SelectedIndices_AreSorted()
        {
            var cards = new List<BaseCard>();
            for (int i = 0; i < 5; i++) cards.Add(MakeCard(Suit.Spade, Rank.Ace));
            _hand.AddCards(cards);
            _hand.ToggleSelect(4);
            _hand.ToggleSelect(1);
            _hand.ToggleSelect(3);
            var indices = _hand.SelectedIndices.Value;
            Assert.AreEqual(1, indices[0]);
            Assert.AreEqual(3, indices[1]);
            Assert.AreEqual(4, indices[2]);
        }
    }
}
