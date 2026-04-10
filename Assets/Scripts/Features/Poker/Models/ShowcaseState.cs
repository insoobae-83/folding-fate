using System.Collections.Generic;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class ShowcaseState
    {
        public static readonly ShowcaseState Inactive = new(false, new List<BaseCard>(), string.Empty);

        public bool IsActive { get; }
        public IReadOnlyList<BaseCard> Cards { get; }
        public string RankText { get; }

        public ShowcaseState(bool isActive, IReadOnlyList<BaseCard> cards, string rankText)
        {
            IsActive = isActive;
            Cards = cards;
            RankText = rankText;
        }
    }
}
