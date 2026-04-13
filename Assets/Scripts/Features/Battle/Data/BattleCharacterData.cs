using UnityEngine;

namespace FoldingFate.Features.Battle.Data
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "FoldingFate/Battle/CharacterData")]
    public class BattleCharacterData : ScriptableObject
    {
        public string DisplayName;
        public float MaxHp = 100f;
        public float Attack = 15f;
        public float Defense = 5f;
        public GameObject Prefab;
    }
}