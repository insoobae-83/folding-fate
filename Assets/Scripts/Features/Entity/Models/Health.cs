using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Health : IEntityComponent
    {
        public FoldingFate.Core.Entity Owner { get; set; }
        public float CurrentHp { get; set; }
        public float MaxHp { get; set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsDead => !IsAlive;

        public Health(float maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }
    }
}