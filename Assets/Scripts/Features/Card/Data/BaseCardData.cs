using UnityEngine;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Data
{
    [CreateAssetMenu(fileName = "NewBaseCard", menuName = "FoldingFate/Card/BaseCardData")]
    public class BaseCardData : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public CardCategory Category { get; private set; }
        [field: SerializeField] public Suit Suit { get; private set; }
        [field: SerializeField] public Rank Rank { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
    }
}
