using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class DealSystemTests
    {
        private DeckModel _deck;
        private HandModel _hand;
        private DealSystem _system;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _hand = new HandModel(maxHandSize: 8);
            _system = new DealSystem(_deck, _hand, new HandEvaluator());
            _system.InitializeDeck();
        }

        [TearDown]
        public void TearDown()
        {
            _deck.Dispose();
            _hand.Dispose();
        }

        [Test]
        public void InitializeDeck_DeckHas52Cards()
        {
            Assert.AreEqual(52, _deck.RemainingCount.Value);
        }

        [Test]
        public void Deal_AddsCardsToHand()
        {
            _system.Deal(5);
            Assert.AreEqual(5, _hand.Cards.Value.Count);
        }

        [Test]
        public void DrawToFull_FillsHandToMaxHandSize()
        {
            _system.Deal(3);
            _system.DrawToFull();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }

        [Test]
        public void DrawToFull_DoesNothingWhenHandIsFull()
        {
            _system.Deal(8);
            int deckBefore = _deck.RemainingCount.Value;
            _system.DrawToFull();
            Assert.AreEqual(deckBefore, _deck.RemainingCount.Value);
        }

        [Test]
        public void ToggleSelect_SelectsCard()
        {
            _system.Deal(5);
            _system.ToggleSelect(0);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_IgnoresWhenAt5CardsAndAddingNew()
        {
            _system.Deal(8);
            for (int i = 0; i < 5; i++) _system.ToggleSelect(i);
            Assert.AreEqual(5, _hand.SelectedCount);

            _system.ToggleSelect(5);
            Assert.AreEqual(5, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_AllowsDeselectWhenAt5Cards()
        {
            _system.Deal(8);
            for (int i = 0; i < 5; i++) _system.ToggleSelect(i);

            _system.ToggleSelect(0);
            Assert.AreEqual(4, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_IgnoresOutOfRangeIndex()
        {
            _system.Deal(3);
            Assert.DoesNotThrow(() => _system.ToggleSelect(-1));
            Assert.DoesNotThrow(() => _system.ToggleSelect(3));
            Assert.AreEqual(0, _hand.SelectedCount);
        }

        [Test]
        public void EvaluateSelected_ReturnsPairForTwoSameRankCards()
        {
            var cards = new List<BaseCard>
            {
                new BaseCard("s_k", CardCategory.Standard, Suit.Spade, Rank.King, "", ""),
                new BaseCard("h_k", CardCategory.Standard, Suit.Heart, Rank.King, "", ""),
                new BaseCard("d_2", CardCategory.Standard, Suit.Diamond, Rank.Two, "", ""),
            };
            _hand.AddCards(cards);
            _hand.ToggleSelect(0);
            _hand.ToggleSelect(1);
            _hand.ToggleSelect(2);

            var result = _system.EvaluateSelected();
            Assert.AreEqual(HandRank.OnePair, result.Rank);
        }

        [Test]
        public void DiscardSelected_RemovesSelectedCardsFromHand()
        {
            _system.Deal(8);
            _system.ToggleSelect(0);
            _system.ToggleSelect(1);
            _system.ToggleSelect(2);
            Assert.AreEqual(3, _hand.SelectedCount);

            _system.DiscardSelected();

            Assert.AreEqual(5, _hand.Cards.Value.Count);
            Assert.AreEqual(0, _hand.SelectedCount);
        }

        [Test]
        public void DrawOne_AddsOneCardToHand()
        {
            Assert.AreEqual(0, _hand.Cards.Value.Count);
            _system.DrawOne();
            Assert.AreEqual(1, _hand.Cards.Value.Count);
            Assert.AreEqual(51, _deck.RemainingCount.Value);
        }

        [Test]
        public void DrawOne_DoesNothingWhenHandIsFull()
        {
            _system.DrawToFull();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
            int deckBefore = _deck.RemainingCount.Value;

            _system.DrawOne();

            Assert.AreEqual(8, _hand.Cards.Value.Count);
            Assert.AreEqual(deckBefore, _deck.RemainingCount.Value);
        }

        [Test]
        public void CardsNeeded_ReturnsCorrectCount()
        {
            Assert.AreEqual(8, _system.CardsNeeded());
            _system.Deal(3);
            Assert.AreEqual(5, _system.CardsNeeded());
            _system.DrawToFull();
            Assert.AreEqual(0, _system.CardsNeeded());
        }
    }
}
