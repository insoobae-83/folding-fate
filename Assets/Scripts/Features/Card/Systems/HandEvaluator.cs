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
    }
}
