using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class CardVariant
    {
        public string Id { get; }
        public BaseCard BaseCard { get; }
        public string DisplayName { get; }
        public string SkinId { get; }
        public Element Element { get; }
        public IReadOnlyList<StatModifier> StatModifiers { get; }

        public bool HasElement => Element != Element.None;
        public bool HasSkin => !string.IsNullOrEmpty(SkinId);
        public bool HasStatModifiers => StatModifiers.Count > 0;

        public CardVariant(
            string id,
            BaseCard baseCard,
            string displayName,
            string skinId,
            Element element,
            List<StatModifier> statModifiers)
        {
            Id = id;
            BaseCard = baseCard;
            DisplayName = displayName;
            SkinId = skinId;
            Element = element;
            StatModifiers = new List<StatModifier>(statModifiers).AsReadOnly();
        }

        public float GetStatValue(StatType type)
        {
            float sum = 0f;
            for (int i = 0; i < StatModifiers.Count; i++)
            {
                if (StatModifiers[i].Type == type)
                {
                    sum += StatModifiers[i].Value;
                }
            }
            return sum;
        }
    }
}
