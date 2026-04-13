using UnityEngine;

namespace FoldingFate.Features.Battle.Data
{
    [CreateAssetMenu(fileName = "NewMonster", menuName = "FoldingFate/Battle/MonsterData")]
    public class BattleMonsterData : ScriptableObject
    {
        public string DisplayName;
        public float MaxHp = 200f;
        public float Attack = 12f;
        public float Defense = 3f;
        public GameObject Prefab;
    }
}