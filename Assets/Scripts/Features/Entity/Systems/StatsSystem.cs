using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Structs;

namespace FoldingFate.Features.Entity.Systems
{
    public class StatsSystem
    {
        public float GetValue(Stats stats, EntityStatType type)
        {
            stats.BaseStats.TryGetValue(type, out var baseValue);
            float modifierSum = 0f;
            var modifiers = stats.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].StatType == type)
                    modifierSum += modifiers[i].Value;
            }
            return baseValue + modifierSum;
        }

        public void AddModifier(Stats stats, EntityStatModifier modifier)
        {
            stats.Modifiers.Add(modifier);
        }

        public void RemoveModifiersBySourceId(Stats stats, string sourceId)
        {
            stats.Modifiers.RemoveAll(m => m.SourceId == sourceId);
        }

        public void RemoveModifiersBySource(Stats stats, ModifierSource source)
        {
            stats.Modifiers.RemoveAll(m => m.Source == source);
        }
    }
}