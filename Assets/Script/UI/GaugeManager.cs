using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class GaugeManager : MonoBehaviour
    {
        [SerializeField] private float maxGauge = 100f;
        [SerializeField] private float initialGaugeIncrease = 20f;
        [SerializeField] private float gaugeIncreaseModifier = 0.9f;
        [SerializeField] private float gaugeDecreaseRate = 5f;
        [SerializeField] private Slider gaugeSlider;
        //[SerializeField] private TextMeshProUGUI gaugeText;

        private float currentGauge = 0f;
        private float currentGaugeIncrease;
        private float currentGaugeDecreaseRate;

        void Awake()
        {
            currentGaugeIncrease = initialGaugeIncrease;
            currentGaugeDecreaseRate = gaugeDecreaseRate;
            UpdateUI();
        }

        void Update()
        {
            DecreaseGaugeOverTime();
        }

        public void IncreaseGauge()
        {
            currentGauge += currentGaugeIncrease;
            currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
            UpdateUI();
        }

        public void DecreaseGaugeOverTime()
        {
            if (currentGauge > 0f)
            {
                currentGauge -= currentGaugeDecreaseRate * Time.deltaTime;
                currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
                UpdateUI();
            }
        }

        public void ResetGauge()
        {
            currentGauge = 0f;
            currentGaugeIncrease = initialGaugeIncrease;
            currentGaugeDecreaseRate = gaugeDecreaseRate;
            UpdateUI();
        }

        public bool IsFull(float threshold = 0.05f)
        {
            return currentGauge + threshold >= maxGauge;
        }

        private void UpdateUI()
        {
            if (gaugeSlider != null)
                gaugeSlider.value = currentGauge / maxGauge;
            //if (gaugeText != null)
            //    gaugeText.text = $"{(int)currentGauge} / {(int)maxGauge}";
        }
    }
}