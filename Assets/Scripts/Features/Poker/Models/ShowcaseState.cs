using System.Collections.Generic;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class ShowcaseState
    {
        public static readonly ShowcaseState Inactive = new(false, new List<BaseCard>(), new HashSet<BaseCard>(), string.Empty);

        private readonly HashSet<BaseCard> _highlightedSet;

        public bool IsActive { get; }
        public IReadOnlyList<BaseCard> Cards { get; }
        public string RankText { get; }

        public ShowcaseState(bool isActive, IReadOnlyList<BaseCard> cards,
                             HashSet<BaseCard> highlightedCards, string rankText)
        {
            IsActive = isActive;
            Cards = cards;
            _highlightedSet = highlightedCards;
            RankText = rankText;
        }

        public bool IsHighlighted(BaseCard card) => _highlightedSet.Contains(card);
    }
}
