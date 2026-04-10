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

        [Test]
        public void Evaluate_ThreeOfAKind_ReturnsThreeOfAKind()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Queen),
                S(Suit.Heart, Rank.Queen),
                S(Suit.Diamond, Rank.Queen),
                S(Suit.Club, Rank.Two),
                S(Suit.Spade, Rank.Five)
            };
            Assert.AreEqual(HandRank.ThreeOfAKind, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_FullHouse_ReturnsFullHouse()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.King),
                S(Suit.Club, Rank.Ace),
                S(Suit.Spade, Rank.Ace)
            };
            Assert.AreEqual(HandRank.FullHouse, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_FourOfAKind_ReturnsFourOfAKind()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Jack),
                S(Suit.Heart, Rank.Jack),
                S(Suit.Diamond, Rank.Jack),
                S(Suit.Club, Rank.Jack),
                S(Suit.Spade, Rank.Three)
            };
            Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_Straight_ReturnsStraight()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Five),
                S(Suit.Heart, Rank.Six),
                S(Suit.Diamond, Rank.Seven),
                S(Suit.Club, Rank.Eight),
                S(Suit.Spade, Rank.Nine)
            };
            Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_AceHighStraight_ReturnsStraight()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Ten),
                S(Suit.Heart, Rank.Jack),
                S(Suit.Diamond, Rank.Queen),
                S(Suit.Club, Rank.King),
                S(Suit.Spade, Rank.Ace)
            };
            Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_AceLowStraight_ReturnsStraight()
        {
            // A-2-3-4-5 (wheel)
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Ace),
                S(Suit.Heart, Rank.Two),
                S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Four),
                S(Suit.Spade, Rank.Five)
            };
            Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_Flush_ReturnsFlush()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Heart, Rank.Two),
                S(Suit.Heart, Rank.Five),
                S(Suit.Heart, Rank.Seven),
                S(Suit.Heart, Rank.Nine),
                S(Suit.Heart, Rank.King)
            };
            Assert.AreEqual(HandRank.Flush, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_StraightFlush_ReturnsStraightFlush()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Diamond, Rank.Five),
                S(Suit.Diamond, Rank.Six),
                S(Suit.Diamond, Rank.Seven),
                S(Suit.Diamond, Rank.Eight),
                S(Suit.Diamond, Rank.Nine)
            };
            Assert.AreEqual(HandRank.StraightFlush, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_RoyalFlush_ReturnsRoyalFlush()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Club, Rank.Ten),
                S(Suit.Club, Rank.Jack),
                S(Suit.Club, Rank.Queen),
                S(Suit.Club, Rank.King),
                S(Suit.Club, Rank.Ace)
            };
            Assert.AreEqual(HandRank.RoyalFlush, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_SevenCards_ReturnsBestHand()
        {
            // Heart 5 cards (Flush) + Spade Ace + Club Ace -> best is Flush
            var cards = new List<BaseCard>
            {
                S(Suit.Heart, Rank.Two),
                S(Suit.Heart, Rank.Five),
                S(Suit.Heart, Rank.Seven),
                S(Suit.Heart, Rank.Nine),
                S(Suit.Heart, Rank.King),
                S(Suit.Spade, Rank.Ace),
                S(Suit.Club, Rank.Ace)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(HandRank.Flush, result.Rank);
            Assert.AreEqual(5, result.BestHand.Count);
        }

        [Test]
        public void Evaluate_SixCards_PicksBestFive()
        {
            // FourOfAKind(J) + OnePair(K) -> FourOfAKind selected
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Jack),
                S(Suit.Heart, Rank.Jack),
                S(Suit.Diamond, Rank.Jack),
                S(Suit.Club, Rank.Jack),
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King)
            };
            Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_OneJokerWithPair_ReturnsThreeOfAKind()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Seven),
                J()
            };
            Assert.AreEqual(HandRank.ThreeOfAKind, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_TwoJokersWithPair_ReturnsFourOfAKind()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Three),
                J(),
                J()
            };
            Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_OneJokerWithFourFlushCards_ReturnsFlush()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Heart, Rank.Two),
                S(Suit.Heart, Rank.Five),
                S(Suit.Heart, Rank.Seven),
                S(Suit.Heart, Rank.Nine),
                J()
            };
            Assert.AreEqual(HandRank.Flush, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_OneJokerWithFourStraightCards_ReturnsStraight()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Five),
                S(Suit.Heart, Rank.Six),
                S(Suit.Diamond, Rank.Seven),
                S(Suit.Club, Rank.Eight),
                J()
            };
            Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_CustomCardsIgnored_EvaluatesRemainingCards()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King),
                S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Seven),
                S(Suit.Spade, Rank.Ace),
                Custom(),
                Custom()
            };
            Assert.AreEqual(HandRank.OnePair, _evaluator.Evaluate(cards).Rank);
        }

        [Test]
        public void Evaluate_ThreeCards_ReturnsValidResult()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Ace),
                S(Suit.Heart, Rank.Ace),
                S(Suit.Diamond, Rank.King)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(HandRank.OnePair, result.Rank);
            Assert.AreEqual(3, result.BestHand.Count);
        }

        [Test]
        public void Compare_HigherHandRankWins()
        {
            var flush = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Heart, Rank.Two), S(Suit.Heart, Rank.Five),
                S(Suit.Heart, Rank.Seven), S(Suit.Heart, Rank.Nine), S(Suit.Heart, Rank.King)
            });
            var straight = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Spade, Rank.Five), S(Suit.Heart, Rank.Six),
                S(Suit.Diamond, Rank.Seven), S(Suit.Club, Rank.Eight), S(Suit.Spade, Rank.Nine)
            });
            Assert.Greater(flush.CompareTo(straight), 0);
        }

        [Test]
        public void Compare_SameRankHigherKickerWins()
        {
            // OnePair K with Ace kicker vs OnePair K with Queen kicker
            var pairKingAceKicker = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Ace), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
            });
            var pairKingQueenKicker = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Queen), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
            });
            Assert.Greater(pairKingAceKicker.CompareTo(pairKingQueenKicker), 0);
        }

        [Test]
        public void HandResult_ContributingCards_StoresProvidedCards()
        {
            var card1 = S(Suit.Spade, Rank.Ace);
            var card2 = S(Suit.Heart, Rank.Ace);
            var allCards = new List<BaseCard> { card1, card2, S(Suit.Diamond, Rank.Three) };
            var contributing = new List<BaseCard> { card1, card2 };
            var result = new HandResult(HandRank.OnePair, allCards, new List<int> { 14 }, contributing);
            Assert.AreEqual(2, result.ContributingCards.Count);
            Assert.Contains(card1, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(card2, (System.Collections.ICollection)result.ContributingCards);
        }

        [Test]
        public void Evaluate_HighCard_ContributingCardsIsHighestCard()
        {
            var king = S(Suit.Spade, Rank.King);
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Two), S(Suit.Heart, Rank.Four),
                S(Suit.Diamond, Rank.Seven), S(Suit.Club, Rank.Nine), king
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(1, result.ContributingCards.Count);
            Assert.AreEqual(king, result.ContributingCards[0]);
        }

        [Test]
        public void Evaluate_OnePair_ContributingCardsIsPairOnly()
        {
            var kingS = S(Suit.Spade, Rank.King);
            var kingH = S(Suit.Heart, Rank.King);
            var cards = new List<BaseCard>
            {
                kingS, kingH, S(Suit.Diamond, Rank.Three),
                S(Suit.Club, Rank.Seven), S(Suit.Spade, Rank.Ace)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(2, result.ContributingCards.Count);
            Assert.Contains(kingS, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(kingH, (System.Collections.ICollection)result.ContributingCards);
        }

        [Test]
        public void Evaluate_TwoPair_ContributingCardsIsBothPairs()
        {
            var kingS = S(Suit.Spade, Rank.King);
            var kingH = S(Suit.Heart, Rank.King);
            var threeD = S(Suit.Diamond, Rank.Three);
            var threeC = S(Suit.Club, Rank.Three);
            var cards = new List<BaseCard>
            {
                kingS, kingH, threeD, threeC, S(Suit.Spade, Rank.Ace)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(4, result.ContributingCards.Count);
            Assert.Contains(kingS, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(kingH, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(threeD, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(threeC, (System.Collections.ICollection)result.ContributingCards);
        }

        [Test]
        public void Evaluate_ThreeOfAKind_ContributingCardsIsTripleOnly()
        {
            var qS = S(Suit.Spade, Rank.Queen);
            var qH = S(Suit.Heart, Rank.Queen);
            var qD = S(Suit.Diamond, Rank.Queen);
            var cards = new List<BaseCard>
            {
                qS, qH, qD, S(Suit.Club, Rank.Two), S(Suit.Spade, Rank.Five)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(3, result.ContributingCards.Count);
            Assert.Contains(qS, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(qH, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(qD, (System.Collections.ICollection)result.ContributingCards);
        }

        [Test]
        public void Evaluate_FourOfAKind_ContributingCardsIsQuadOnly()
        {
            var jS = S(Suit.Spade, Rank.Jack);
            var jH = S(Suit.Heart, Rank.Jack);
            var jD = S(Suit.Diamond, Rank.Jack);
            var jC = S(Suit.Club, Rank.Jack);
            var cards = new List<BaseCard>
            {
                jS, jH, jD, jC, S(Suit.Spade, Rank.Three)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(4, result.ContributingCards.Count);
            Assert.Contains(jS, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(jH, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(jD, (System.Collections.ICollection)result.ContributingCards);
            Assert.Contains(jC, (System.Collections.ICollection)result.ContributingCards);
        }

        [Test]
        public void Evaluate_Straight_ContributingCardsIsAll()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Five), S(Suit.Heart, Rank.Six),
                S(Suit.Diamond, Rank.Seven), S(Suit.Club, Rank.Eight), S(Suit.Spade, Rank.Nine)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(5, result.ContributingCards.Count);
        }

        [Test]
        public void Evaluate_Flush_ContributingCardsIsAll()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Heart, Rank.Two), S(Suit.Heart, Rank.Five),
                S(Suit.Heart, Rank.Seven), S(Suit.Heart, Rank.Nine), S(Suit.Heart, Rank.King)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(5, result.ContributingCards.Count);
        }

        [Test]
        public void Evaluate_FullHouse_ContributingCardsIsAll()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.King), S(Suit.Club, Rank.Ace), S(Suit.Spade, Rank.Ace)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(5, result.ContributingCards.Count);
        }

        [Test]
        public void Evaluate_StraightFlush_ContributingCardsIsAll()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Diamond, Rank.Five), S(Suit.Diamond, Rank.Six),
                S(Suit.Diamond, Rank.Seven), S(Suit.Diamond, Rank.Eight), S(Suit.Diamond, Rank.Nine)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(5, result.ContributingCards.Count);
        }

        [Test]
        public void Evaluate_RoyalFlush_ContributingCardsIsAll()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Club, Rank.Ten), S(Suit.Club, Rank.Jack),
                S(Suit.Club, Rank.Queen), S(Suit.Club, Rank.King), S(Suit.Club, Rank.Ace)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(5, result.ContributingCards.Count);
        }

        [Test]
        public void Compare_IdenticalHands_ReturnsZero()
        {
            var hand1 = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
                S(Suit.Diamond, Rank.Ace), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
            });
            var hand2 = _evaluator.Evaluate(new List<BaseCard>
            {
                S(Suit.Club, Rank.King), S(Suit.Diamond, Rank.King),
                S(Suit.Spade, Rank.Ace), S(Suit.Heart, Rank.Three), S(Suit.Club, Rank.Two)
            });
            Assert.AreEqual(0, hand1.CompareTo(hand2));
        }
    }
}
