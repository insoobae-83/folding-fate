using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Combat : IEntityComponent
    {
        public FoldingFate.Core.Entity Owner { get; set; }
        public bool IsInCombat { get; set; }
    }
}