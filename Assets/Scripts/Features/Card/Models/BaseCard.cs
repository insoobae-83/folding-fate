using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class BaseCard
    {
        public string Id { get; }
        public CardCategory Category { get; }
        public Suit? Suit { get; }
        public Rank? Rank { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public bool IsStandard => Category == CardCategory.Standard;
        public bool IsJoker => Category == CardCategory.Joker;
        public bool IsCustom => Category == CardCategory.Custom;

        public BaseCard(
            string id,
            CardCategory category,
            Suit? suit,
            Rank? rank,
            string displayName,
            string description)
        {
            Id = id;
            Category = category;
            Suit = suit;
            Rank = rank;
            DisplayName = displayName;
            Description = description;
        }
    }
}
