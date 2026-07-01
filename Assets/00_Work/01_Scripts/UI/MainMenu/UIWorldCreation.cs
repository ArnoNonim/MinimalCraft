using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 월드 생성 설정 팝업.
    /// 저장 파일이 없을 때 메인메뉴에서 열림.
    /// UIPopup 상속 → UIManager._allPopups에 등록되어 ESC 닫기 지원.
    /// </summary>
    public class UIWorldCreation : UIPopup
    {
        // ── Seed ───────────────────────────────────────────────────────
        [Header("시드")]
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Toggle         randomSeedToggle;

        // ── Noise ──────────────────────────────────────────────────────
        [Header("노이즈")]
        [SerializeField] private Slider    noiseSlider;
        [SerializeField] private TMP_Text  noiseValueText;

        [SerializeField] private float noiseMin   = 0.001f;
        [SerializeField] private float noiseMax   = 0.05f;
        [SerializeField] private float noiseDefault = 0.008f;

        // ── 완료 ───────────────────────────────────────────────────────
        [Header("완료")]
        [SerializeField] private Button confirmButton;

        // ── 콜백 ───────────────────────────────────────────────────────
        /// <summary>확인 버튼 클릭 시 — (seed, noiseScale) 전달</summary>
        public event Action<int, float> OnConfirm;

        // ──────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();

            // 슬라이더 범위 초기화
            noiseSlider.minValue = noiseMin;
            noiseSlider.maxValue = noiseMax;
            noiseSlider.value    = noiseDefault;
            RefreshNoiseText(noiseDefault);

            // 바인딩
            randomSeedToggle.onValueChanged.AddListener(OnRandomSeedToggleChanged);
            noiseSlider.onValueChanged.AddListener(RefreshNoiseText);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void Start()
        {
            UIManager.Instance.RegisterPopup(this);
        }

        protected override void OnOpen()
        {
            // 열릴 때마다 기본값으로 리셋
            randomSeedToggle.isOn = false;
            seedInput.text        = string.Empty;
            seedInput.interactable = true;
            noiseSlider.value     = noiseDefault;
            RefreshNoiseText(noiseDefault);
        }

        // ── 핸들러 ─────────────────────────────────────────────────────
        private void OnRandomSeedToggleChanged(bool isOn)
        {
            seedInput.interactable = !isOn;
            seedInput.text         = string.Empty;
        }

        private void RefreshNoiseText(float value)
        {
            noiseValueText.text = value.ToString("F3");
        }

        private void OnConfirmClicked()
        {
            int seed;
            if (randomSeedToggle.isOn || string.IsNullOrWhiteSpace(seedInput.text))
            {
                seed = UnityEngine.Random.Range(0, int.MaxValue);
            }
            else
            {
                seed = Mathf.Abs(int.TryParse(seedInput.text, out int parsed)
                    ? parsed
                    : seedInput.text.GetHashCode());
            }

            float noise = noiseSlider.value;

            WorldCreationData.Set(seed, noise);
            OnConfirm?.Invoke(seed, noise);

            Close();
        }
    }
}