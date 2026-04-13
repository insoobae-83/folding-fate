using UnityEngine;
using UnityEngine.UI;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Components
{
    public class WorldHpBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        public void SetValue(Health health)
        {
            if (health == null) return;
            _fillImage.fillAmount = health.CurrentHp / health.MaxHp;
        }

        public void SetFill(float ratio)
        {
            _fillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}