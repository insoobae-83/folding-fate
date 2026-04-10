using UnityEngine;

namespace FoldingFate.Features.Poker.Data
{
    [CreateAssetMenu(fileName = "PokerConfig", menuName = "FoldingFate/Poker/PokerConfig")]
    public class PokerConfig : ScriptableObject
    {
        [Tooltip("족보 연출 표시 시간 (초)")]
        [Min(0f)]
        public float ShowcaseDurationSeconds = 2f;

        [Tooltip("딜링 시 카드 간 간격 (초)")]
        [Min(0f)]
        public float DealIntervalSeconds = 0.1f;

        [Tooltip("카드 한 장의 이동 애니메이션 시간 (초)")]
        [Min(0f)]
        public float DealAnimationDurationSeconds = 0.15f;
    }
}
