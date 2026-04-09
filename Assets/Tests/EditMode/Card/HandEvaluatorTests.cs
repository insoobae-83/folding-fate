using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class HandEvaluatorTests
    {
        private HandEvaluator _evaluator;

        [SetUp]
        public void SetUp() => _evaluator = new HandEvaluator();

        // Helper: Create Standard card
        private static BaseCard S(Suit suit, Rank rank) =>
            new BaseCard($"{suit}_{rank}", CardCategory.Standard, suit, rank, "", "");

        // Helper: Create Joker card
        private static BaseCard J() =>
            new BaseCard("joker", CardCategory.Joker, null, null, "", "");

        // Helper: Create Custom card
        private static BaseCard Custom() =>
            new BaseCard("custom", CardCategory.Custom, null, null, "", "");

        [Test]
        public void Evaluate_FiveUnrelatedCards_ReturnsHighCard()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Two),
                S(Suit.Heart, Rank.Four),
                S(Suit.Diamond, Rank.Seven),
                S(Suit.Club, Rank.Nine),
                S(Suit.Spade, Rank.King)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(HandRank.HighCard, result.Rank);
            Assert.AreEqual(5, result.BestHand.Count);
        }

        [Test]
        public void Evaluate_EmptyInput_ReturnsHighCard()
        {
            var result = _evaluator.Evaluate(new List<BaseCard>());
            Assert.AreEqual(HandRank.HighCard, result.Rank);
        }

        [Test]
        public void Evaluate_OnePair_ReturnsOnePair()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Seven),
                S(Suit.Spade, Rank.Ace)
            };
            Assert.AreEqual(HandRank.OnePair, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_TwoPair_ReturnsTwoPair()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Three),
                S(Suit.Spade, Rank.Ace)
            };
            Assert.AreEqual(HandRank.TwoPair, _evaluator.Evaluate(cards).Rank);
        }
    }
}
