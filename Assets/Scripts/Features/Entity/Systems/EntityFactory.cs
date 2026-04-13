using FoldingFate.Core;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Entity.Systems
{
    public class EntityFactory
    {
        public Core.Entity CreateCombatEntity(string id, EntityType type, string displayName,
            float maxHp, float attack, float defense)
        {
            var entity = new Core.Entity(id, type, displayName);

            var stats = new Stats();
            stats.BaseStats[EntityStatType.Attack] = attack;
            stats.BaseStats[EntityStatType.Defense] = defense;
            entity.Add(stats);

            entity.Add(new Health(maxHp));
            entity.Add(new Combat());

            return entity;
        }
    }
}