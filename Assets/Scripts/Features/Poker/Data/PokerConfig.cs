using UnityEngine;

namespace FoldingFate.Features.Poker.Data
{
    [CreateAssetMenu(fileName = "PokerConfig", menuName = "FoldingFate/Poker/PokerConfig")]
    public class PokerConfig : ScriptableObject
    {
        [Tooltip("족보 연출 표시 시간 (초)")]
        [Min(0f)]
        public float ShowcaseDurationSeconds = 2f;
    }
}
