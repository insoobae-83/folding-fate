using System;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;

namespace FoldingFate.Features.Entity.Structs
{
    [Serializable]
    public struct EntityStatModifier
    {
        public EntityStatType StatType;
        public float Value;
        public ModifierSource Source;
        public string SourceId;

        public EntityStatModifier(EntityStatType statType, float value, ModifierSource source, string sourceId)
        {
            StatType = statType;
            Value = value;
            Source = source;
            SourceId = sourceId;
        }
    }
}