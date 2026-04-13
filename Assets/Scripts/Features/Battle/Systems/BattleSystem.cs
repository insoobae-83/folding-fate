using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Systems
{
    public class BattleSystem
    {
        public BattleModel StartBattle(IReadOnlyList<FoldingFate.Core.Entity> allies, IReadOnlyList<FoldingFate.Core.Entity> enemies)
        {
            SetCombatState(allies, true);
            SetCombatState(enemies, true);
            return new BattleModel(Guid.NewGuid().ToString(), allies, enemies);
        }

        public void EndBattle(BattleModel battle)
        {
            SetCombatState(battle.Allies, false);
            SetCombatState(battle.Enemies, false);
            battle.Dispose();
        }

        private static void SetCombatState(IReadOnlyList<FoldingFate.Core.Entity> entities, bool isInCombat)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var combat = entities[i].Get<Combat>();
                if (combat != null)
                    combat.IsInCombat = isInCombat;
            }
        }
    }
}
