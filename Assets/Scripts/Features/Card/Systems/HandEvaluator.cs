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
    }
}
