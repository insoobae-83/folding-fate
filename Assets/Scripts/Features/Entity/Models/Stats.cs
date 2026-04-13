using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Structs;

namespace FoldingFate.Features.Entity.Models
{
    public class Stats : IEntityComponent
    {
        public FoldingFate.Core.Entity Owner { get; set; }
        public Dictionary<EntityStatType, float> BaseStats { get; } = new();
        public List<EntityStatModifier> Modifiers { get; } = new();
    }
}