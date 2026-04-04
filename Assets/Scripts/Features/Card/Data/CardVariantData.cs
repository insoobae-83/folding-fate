using System.Collections.Generic;
using UnityEngine;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Data
{
    [CreateAssetMenu(fileName = "NewCardVariant", menuName = "FoldingFate/Card/CardVariantData")]
    public class CardVariantData : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public BaseCardData BaseCard { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string SkinId { get; private set; }
        [field: SerializeField] public Element Element { get; private set; }
        [field: SerializeField] public List<StatModifier> StatModifiers { get; private set; }
    }
}
