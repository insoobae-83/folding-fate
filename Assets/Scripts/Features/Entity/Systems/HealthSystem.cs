using System;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Entity.Systems
{
    public class HealthSystem
    {
        public void TakeDamage(Health health, float amount)
        {
            health.CurrentHp = Math.Max(0, health.CurrentHp - amount);
        }

        public void Heal(Health health, float amount)
        {
            health.CurrentHp = Math.Min(health.MaxHp, health.CurrentHp + amount);
        }

        public void SetMaxHp(Health health, float value)
        {
            health.MaxHp = value;
            if (health.CurrentHp > health.MaxHp)
                health.CurrentHp = health.MaxHp;
        }
    }
}