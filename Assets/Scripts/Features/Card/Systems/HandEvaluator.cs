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

            return EvaluateBest(standard, jokerCount);
        }

        private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
        {
            // Checks are ordered highest-to-lowest: each TryXxx assumes higher hands were already eliminated.
            if (TryFourOfAKind(cards, out var foak)) return foak;
            if (TryFullHouse(cards, out var fh)) return fh;
            if (TryStraight(cards, out var st)) return st;
            if (TryThreeOfAKind(cards, out var toak)) return toak;
            if (TryTwoPair(cards, out var tp)) return tp;
            if (TryOnePair(cards, out var op)) return op;
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

        private static bool TryOnePair(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();
            if (pairs.Count == 0) return false;

            int pairRank = pairs[0].Key;
            var kickers = counts
                .Where(kv => kv.Key != pairRank)
                .OrderByDescending(kv => kv.Key)
                .Take(3)
                .Select(kv => kv.Key)
                .ToList();
            var tiebreak = new List<int> { pairRank };
            tiebreak.AddRange(kickers);
            result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
            return true;
        }

        private static bool TryTwoPair(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();
            if (pairs.Count < 2) return false;

            int highPair = pairs[0].Key;
            int lowPair = pairs[1].Key;
            int kicker = counts
                .Where(kv => kv.Key != highPair && kv.Key != lowPair)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => kv.Key)
                .FirstOrDefault();
            result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { highPair, lowPair, kicker });
            return true;
        }

        private static bool TryFourOfAKind(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var quads = counts.Where(kv => kv.Value >= 4).OrderByDescending(kv => kv.Key).ToList();
            if (quads.Count == 0) return false;

            int quadRank = quads[0].Key;
            int kicker = counts
                .Where(kv => kv.Key != quadRank)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => kv.Key)
                .FirstOrDefault();
            result = new HandResult(HandRank.FourOfAKind, cards.ToList(), new List<int> { quadRank, kicker });
            return true;
        }

        private static bool TryFullHouse(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var triples = counts.Where(kv => kv.Value >= 3).OrderByDescending(kv => kv.Key).ToList();
            if (triples.Count == 0) return false;

            int tripleRank = triples[0].Key;
            var pairs = counts
                .Where(kv => kv.Key != tripleRank && kv.Value >= 2)
                .OrderByDescending(kv => kv.Key)
                .ToList();
            if (pairs.Count == 0) return false;

            result = new HandResult(HandRank.FullHouse, cards.ToList(),
                new List<int> { tripleRank, pairs[0].Key });
            return true;
        }

        private static bool TryStraight(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var values = new HashSet<int>(cards
                .Where(c => c.Rank.HasValue)
                .Select(c => AceHighValue(c.Rank.Value)));
            if (values.Contains(14)) values.Add(1); // Ace Low 지원 (Duplicate Ace)

            for (int top = 14; top >= 5; top--)
            {
                if (values.Contains(top) && values.Contains(top - 1) && values.Contains(top - 2)
                    && values.Contains(top - 3) && values.Contains(top - 4))
                {
                    // top=14: A-K-Q-J-10, top=5: A(1)-2-3-4-5 (wheel)
                    result = new HandResult(HandRank.Straight, cards.ToList(), new List<int> { top });
                    return true;
                }
            }
            return false;
        }

        private static bool TryThreeOfAKind(List<BaseCard> cards, out HandResult result)
        {
            result = null;
            var counts = GetRankCounts(cards);
            var triples = counts.Where(kv => kv.Value >= 3).OrderByDescending(kv => kv.Key).ToList();
            if (triples.Count == 0) return false;

            int tripleRank = triples[0].Key;
            var kickers = counts
                .Where(kv => kv.Key != tripleRank)
                .OrderByDescending(kv => kv.Key)
                .Take(2)
                .Select(kv => kv.Key)
                .ToList();
            var tiebreak = new List<int> { tripleRank };
            tiebreak.AddRange(kickers);
            result = new HandResult(HandRank.ThreeOfAKind, cards.ToList(), tiebreak);
            return true;
        }
    }
}
