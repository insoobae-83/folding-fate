using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Card.Systems
{
    public class CardStatSystem
    {
        public float GetStatValue(CardVariant variant, StatType type)
        {
            float sum = 0f;
            var modifiers = variant.StatModifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Type == type)
                {
                    sum += modifiers[i].Value;
                }
            }
            return sum;
        }
    }
}
