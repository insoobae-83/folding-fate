using System;
using System.Collections.Generic;
using System.Linq;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Card.Systems
{
    public class HandEvaluator
    {
        public HandResult Evaluate(IReadOnlyList<BaseCard> cards)
        {
            var standard = new List<BaseCard>();
            int jokerCount = 0;
            foreach (var card in cards)
            {
                if (card.Category == CardCategory.Standard)
                    standard.Add(card);
                else if (card.Category == CardCategory.Joker)
                    jokerCount++;
                // Custom: ignore
            }

            if (standard.Count == 0 && jokerCount == 0)
                return new HandResult(HandRank.HighCard, new List<BaseCard>(), new List<int> { 0 });

            if (standard.Count <= 5)
                return EvaluateBest(standard, jokerCount);

            // n > 5: nC5 combination search for best hand
            HandResult best = null;
            foreach (var combo in GetCombinations(standard, 5))
            {
                var r = EvaluateBest(combo, jokerCount);
                if (best == null || r.CompareTo(best) > 0) best = r;
            }
            return best;
        }

        private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
        {
            // Checks are ordered highest-to-lowest: each TryXxx assumes higher hands were already eliminated.
            if (TryRoyalFlush(cards, jokerCount, out var rf)) return rf;
            if (TryStraightFlush(cards, jokerCount, out var sf)) return sf;
            if (TryFourOfAKind(cards, jokerCount, out var foak)) return foak;
            if (TryFullHouse(cards, jokerCount, out var fh)) return fh;
            if (TryFlush(cards, jokerCount, out var fl)) return fl;
            if (TryStraight(cards, jokerCount, out var st)) return st;
            if (TryThreeOfAKind(cards, jokerCount, out var toak)) return toak;
            if (TryTwoPair(cards, jokerCount, out var tp)) return tp;
            if (TryOnePair(cards, jokerCount, out var op)) return op;
            return MakeHighCard(cards);
        }

        private static HandResult MakeHighCard(List<BaseCard> cards)
        {
            var tiebreak = cards
                .Where(c => c.Rank.HasValue)
                .Select(c => AceHighValue(c.Rank.Value))
                .OrderByDescending(v => v)
                .ToList();
            return new HandResult(HandRank.HighCard, cards.ToList(), tiebreak);
        }

        private static int AceHighValue(Rank rank) =>
            rank == Rank.Ace ? 14 : (int)rank;

        private static Dictionary<int, int> GetRankCounts(List<BaseCard> cards)
        {
            var counts = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                if (!c.Rank.HasValue) continue;
                int v = AceHighValue(c.Rank.Value);
                counts[v] = counts.GetValueOrDefault(v, 0) + 1;
            }
            return counts;
        }

        private static bool TryOnePair(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();

            if (pairs.Count >= 1)
            {
                int pairRank = pairs[0].Key;
                var kickers = counts.Where(kv => kv.Key != pairRank)
                                    .OrderByDescending(kv => kv.Key).Take(3).Select(kv => kv.Key).ToList();
                var tiebreak = new List<int> { pairRank };
                tiebreak.AddRange(kickers);
                result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
                return true;
            }
            // 1+ joker: pair the highest rank card
            if (jokerCount >= 1 && counts.Count >= 1)
            {
                int bestRank = counts.Keys.Max();
                var kickers = counts.Keys.Where(k => k != bestRank).OrderByDescending(k => k).Take(3).ToList();
                var tiebreak = new List<int> { bestRank };
                tiebreak.AddRange(kickers);
                result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
                return true;
            }
            return false;
        }

        private static bool TryTwoPair(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();

            if (pairs.Count >= 2)
            {
                int highPair = pairs[0].Key;
                int lowPair = pairs[1].Key;
                int kicker = counts.Where(kv => kv.Key != highPair && kv.Key != lowPair)
                                   .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
                result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { highPair, lowPair, kicker });
                return true;
            }
            // 1 joker + 1 pair → use joker to make second pair from highest single
            if (jokerCount >= 1 && pairs.Count >= 1)
            {
                int existingPair = pairs[0].Key;
                int secondPairRank = counts.Where(kv => kv.Key != existingPair)
                                           .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
                if (secondPairRank > 0)
                {
                    int high = Math.Max(existingPair, secondPairRank);
                    int low = Math.Min(existingPair, secondPairRank);
                    result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { high, low, 0 });
                    return true;
                }
            }
            return false;
        }

        private static bool TryFourOfAKind(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            if (counts.Count == 0)
            {
                if (jokerCount >= 4)
                {
                    result = new HandResult(HandRank.FourOfAKind, new List<BaseCard>(), new List<int> { 14, 0 });
                    return true;
                }
                return false;
            }

            var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
            foreach (var kv in sorted)
            {
                if (kv.Value + jokerCount >= 4)
                {
                    int quadRank = kv.Key;
                    int kicker = counts
                        .Where(k => k.Key != quadRank)
                        .OrderByDescending(k => k.Key)
                        .Select(k => k.Key)
                        .FirstOrDefault();
                    result = new HandResult(HandRank.FourOfAKind, cards.ToList(), new List<int> { quadRank, kicker });
                    return true;
                }
            }
            return false;
        }

        private static bool TryFullHouse(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
            if (sorted.Count == 0) return false;

            // Try to form a triple using jokers
            int jokersLeft = jokerCount;
            int tripleRank = -1;
            foreach (var kv in sorted)
            {
                int needed = Math.Max(0, 3 - kv.Value);
                if (needed <= jokersLeft)
                {
                    tripleRank = kv.Key;
                    jokersLeft -= needed;
                    break;
                }
            }
            if (tripleRank == -1) return false;

            // Try to form a pair from remaining cards + remaining jokers
            foreach (var kv in sorted)
            {
                if (kv.Key == tripleRank) continue;
                int needed = Math.Max(0, 2 - kv.Value);
                if (needed <= jokersLeft)
                {
                    result = new HandResult(HandRank.FullHouse, cards.ToList(),
                        new List<int> { tripleRank, kv.Key });
                    return true;
                }
            }
            // Use remaining jokers to form a pair (if 2+ jokers remain)
            if (jokersLeft >= 2)
            {
                int pairRank = sorted.Where(kv => kv.Key != tripleRank).Select(kv => kv.Key).FirstOrDefault();
                if (pairRank == 0) pairRank = 14;
                result = new HandResult(HandRank.FullHouse, cards.ToList(), new List<int> { tripleRank, pairRank });
                return true;
            }
            return false;
        }

        private static bool TryStraight(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var values = new HashSet<int>(cards
                .Where(c => c.Rank.HasValue)
                .Select(c => AceHighValue(c.Rank.Value)));
            if (values.Contains(14)) values.Add(1); // Ace Low 지원 (Duplicate Ace)

            for (int top = 14; top >= 5; top--)
            {
                int needed = 0;
                for (int i = 0; i < 5; i++)
                    if (!values.Contains(top - i)) needed++;
                if (needed <= jokerCount)
                {
                    result = new HandResult(HandRank.Straight, cards.ToList(), new List<int> { top });
                    return true;
                }
            }
            return false;
        }

        private static bool TryThreeOfAKind(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
            foreach (var kv in sorted)
            {
                if (kv.Value + jokerCount >= 3)
                {
                    int tripleRank = kv.Key;
                    var kickers = counts
                        .Where(k => k.Key != tripleRank)
                        .OrderByDescending(k => k.Key)
                        .Take(2)
                        .Select(k => k.Key)
                        .ToList();
                    var tiebreak = new List<int> { tripleRank };
                    tiebreak.AddRange(kickers);
                    result = new HandResult(HandRank.ThreeOfAKind, cards.ToList(), tiebreak);
                    return true;
                }
            }
            if (jokerCount >= 3)
            {
                result = new HandResult(HandRank.ThreeOfAKind, new List<BaseCard>(), new List<int> { 14, 0, 0 });
                return true;
            }
            return false;
        }

        private static IEnumerable<List<BaseCard>> GetCombinations(List<BaseCard> list, int k)
        {
            if (k == 0) { yield return new List<BaseCard>(); yield break; }
            for (int i = 0; i <= list.Count - k; i++)
            {
                var rest = list.GetRange(i + 1, list.Count - i - 1);
                foreach (var combo in GetCombinations(rest, k - 1))
                {
                    combo.Insert(0, list[i]);
                    yield return combo;
                }
            }
        }

        private static bool TryRoyalFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            var royalValues = new HashSet<int> { 10, 11, 12, 13, 14 };
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                var matching = cards
                    .Where(c => c.Suit == suit && c.Rank.HasValue
                                && royalValues.Contains(AceHighValue(c.Rank.Value)))
                    .ToList();
                if (matching.Count + jokerCount >= 5)
                {
                    result = new HandResult(HandRank.RoyalFlush, matching, new List<int> { 0 });
                    return true;
                }
            }
            if (jokerCount >= 5)
            {
                result = new HandResult(HandRank.RoyalFlush, new List<BaseCard>(), new List<int> { 0 });
                return true;
            }
            return false;
        }

        private static bool TryStraightFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                var suitCards = cards.Where(c => c.Suit == suit && c.Rank.HasValue).ToList();
                var values = new HashSet<int>(suitCards.Select(c => AceHighValue(c.Rank.Value)));
                if (values.Contains(14)) values.Add(1);

                for (int top = 13; top >= 5; top--)
                {
                    int needed = 0;
                    for (int i = 0; i < 5; i++)
                        if (!values.Contains(top - i)) needed++;
                    if (needed <= jokerCount)
                    {
                        result = new HandResult(HandRank.StraightFlush, suitCards, new List<int> { top });
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
        {
            result = null;
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                var suitCards = cards
                    .Where(c => c.Suit == suit && c.Rank.HasValue)
                    .OrderByDescending(c => AceHighValue(c.Rank.Value))
                    .ToList();
                if (suitCards.Count + jokerCount >= 5)
                {
                    var top5 = suitCards.Take(5).ToList();
                    var tiebreak = top5.Select(c => AceHighValue(c.Rank.Value)).ToList();
                    result = new HandResult(HandRank.Flush, top5, tiebreak);
                    return true;
                }
            }
            return false;
        }
    }
}
