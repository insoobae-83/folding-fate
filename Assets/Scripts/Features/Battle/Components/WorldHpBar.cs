using UnityEngine;
using UnityEngine.UI;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Components
{
    public class WorldHpBar : MonoBehaviour
    {
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponentInChildren<Slider>();
            if (_slider == null)
            {
                CreateSlider();
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }
        }

        public void SetValue(Health health)
        {
            if (health == null || _slider == null) return;
            _slider.value = health.CurrentHp / health.MaxHp;
        }

        private void CreateSlider()
        {
            // Slider root
            var sliderGo = new GameObject("HpSlider");
            sliderGo.transform.SetParent(transform, false);

            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.anchoredPosition = Vector2.zero;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // Fill Area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.anchoredPosition = Vector2.zero;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;

            // Slider component
            _slider = sliderGo.AddComponent<Slider>();
            _slider.fillRect = fillRect;
            _slider.targetGraphic = fillImage;
            _slider.direction = Slider.Direction.LeftToRight;
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.value = 1f;
            _slider.interactable = false;

            // Handle 불필요 — 이미 없음
        }
    }
}
