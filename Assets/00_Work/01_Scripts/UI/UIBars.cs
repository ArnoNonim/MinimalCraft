using _00_Work._01_Scripts.Player.SO;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI
{
    public class UIBars : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerStatSO playerStat;

        [Header("체력 슬롯 (10개)")]
        [SerializeField] private Image[] healthSlots;

        [Header("효율 슬롯 (10개)")]
        [SerializeField] private Image[] efficiencySlots;

        [Header("허기 슬롯 (10개)")]
        [SerializeField] private Image[] hungerSlots;

        [Header("목마름 슬롯 (10개)")]
        [SerializeField] private Image[] thirstySlots;

        [Header("체온 게이지")]
        [SerializeField] private Image temperatureFill;

        [Tooltip("fillAmount 0.5에 대응하는 기준 체온 (°C)")]
        [SerializeField] private float baseTemperature = 36.5f;

        [Tooltip("fillAmount 0.0에 대응하는 최저 체온 (°C) — 이 이하는 0으로 클램프")]
        [SerializeField] private float minTemperature = 20f;

        [Tooltip("fillAmount 1.0에 대응하는 최고 체온 (°C) — 이 이상은 1로 클램프")]
        [SerializeField] private float maxTemperature = 53f;

        // 색상 프리셋
        private static readonly Color ColorCold   = new Color(0.18f, 0.52f, 1.00f); // 파랑
        private static readonly Color ColorNormal = new Color(1.00f, 1.00f, 1.00f); // 흰색
        private static readonly Color ColorHot    = new Color(1.00f, 0.18f, 0.18f); // 빨강


        private void Update()
        {
            UpdateBar(efficiencySlots, playerStat.curEfficiency, playerStat.maxEfficiency);
        }
        
        public void UpdateBars()
        {
            UpdateBar(healthSlots,     playerStat.curHealth,     playerStat.maxHealth);
            UpdateBar(hungerSlots,     playerStat.curHunger,     playerStat.maxHunger,  reverse: true);
            UpdateBar(thirstySlots,    playerStat.curThirsty,    playerStat.maxThirsty, reverse: true);
            UpdateTemperatureUI(playerStat.temperature);
        }

        private static void UpdateBar(Image[] slots, int value, int max, bool reverse = false)
        {
            int slotCount = slots.Length;

            for (int i = 0; i < slotCount; i++)
            {
                // reverse: 채워지는 기준은 왼쪽(0번)이지만 소모는 오른쪽부터
                int si        = reverse ? slotCount - 1 - i : i;
                int slotValue = value - i * 2;

                float fill = slotValue >= 2 ? 1f
                           : slotValue == 1 ? 0.5f
                                            : 0f;

                if (!Mathf.Approximately(slots[si].fillAmount, fill))
                    slots[si].fillAmount = fill;
            }
        }

        public void UpdateTemperatureUI(float temperature)
        {
            if (temperatureFill == null) return;

            // fillAmount 계산
            float fill;
            if (temperature <= baseTemperature)
            {
                // 저온 구간: min ~ base → 0.0 ~ 0.5
                fill = Mathf.Lerp(0f, 0.5f,
                    Mathf.InverseLerp(minTemperature, baseTemperature, temperature));
            }
            else
            {
                // 고온 구간: base ~ max → 0.5 ~ 1.0
                fill = Mathf.Lerp(0.5f, 1f,
                    Mathf.InverseLerp(baseTemperature, maxTemperature, temperature));
            }

            // 색상 계산 (fill 0.5 기준 양쪽 Lerp)
            Color targetColor;
            if (fill <= 0.5f)
                targetColor = Color.Lerp(ColorCold, ColorNormal, fill / 0.5f);
            else
                targetColor = Color.Lerp(ColorNormal, ColorHot, (fill - 0.5f) / 0.5f);

            // 값이 같으면 대입 생략
            if (!Mathf.Approximately(temperatureFill.fillAmount, fill))
                temperatureFill.fillAmount = fill;

            if (temperatureFill.color != targetColor)
                temperatureFill.color = targetColor;
        }
    }
}