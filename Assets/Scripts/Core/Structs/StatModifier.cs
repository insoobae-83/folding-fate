using System;

namespace FoldingFate.Core
{
    [Serializable]
    public struct StatModifier
    {
        public StatType Type;
        public float Value;

        public StatModifier(StatType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
