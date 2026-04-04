using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Card.Data;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Card.Systems
{
    public class CardFactory
    {
        public BaseCard CreateBaseCard(BaseCardData data)
        {
            Suit? suit = data.Category == CardCategory.Standard ? data.Suit : null;
            Rank? rank = data.Category == CardCategory.Standard ? data.Rank : null;

            return new BaseCard(
                id: data.Id,
                category: data.Category,
                suit: suit,
                rank: rank,
                displayName: data.DisplayName,
                description: data.Description);
        }

        public CardVariant CreateCardVariant(CardVariantData data, BaseCard baseCard)
        {
            var modifiers = new List<StatModifier>();
            if (data.StatModifiers != null)
            {
                modifiers.AddRange(data.StatModifiers);
            }

            return new CardVariant(
                id: data.Id,
                baseCard: baseCard,
                displayName: data.DisplayName,
                skinId: data.SkinId,
                element: data.Element,
                statModifiers: modifiers);
        }
    }
}
