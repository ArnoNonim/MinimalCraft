using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISettingsMouse : UIPopup
    {
        [Header("마우스 UI")]
        [SerializeField] private Slider   sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityLabel;
        [SerializeField] private Button   smoothRotationButton;
        [SerializeField] private TMP_Text smoothRotationLabel;

        [Header("외부 참조")]
        [SerializeField] private PlayerETCStatSO playerStat;

        private PlayerSettingsData _data;
        private bool               _initialized;

        void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }
        
        public void InitData(PlayerSettingsData data)
        {
            _data = data;
            if (!_initialized) { InitSliders(); _initialized = true; }
            Refresh();
        }

        public void ApplyData(PlayerSettingsData data) { _data = data; ApplyAll(); }

        void InitSliders()
        {
            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.1f;
                sensitivitySlider.maxValue = 1f;
                sensitivitySlider.onValueChanged.RemoveAllListeners();
                sensitivitySlider.onValueChanged.AddListener(v =>
                {
                    _data.sensitivity = v;
                    ApplySensitivity(v);
                    if (sensitivityLabel) sensitivityLabel.text = $"마우스 감도 : {Mathf.RoundToInt(v * 100)}%";
                });
            }
            smoothRotationButton?.onClick.AddListener(OnSmoothRotationClicked);
        }

        void Refresh()
        {
            if (_data == null) return;
            sensitivitySlider?.SetValueWithoutNotify(_data.sensitivity);
            if (sensitivityLabel) sensitivityLabel.text = $"마우스 감도 : {Mathf.RoundToInt(_data.sensitivity * 100)}%";
            RefreshSmoothLabel();
            ApplyAll();
        }

        void ApplyAll()
        {
            if (_data == null) return;
            ApplySensitivity(_data.sensitivity);
            ApplySmoothRotation(_data.smoothRotation);
        }

        void OnSmoothRotationClicked()
        {
            _data.smoothRotation = !_data.smoothRotation;
            ApplySmoothRotation(_data.smoothRotation);
            RefreshSmoothLabel();
        }

        void RefreshSmoothLabel()
        {
            if (smoothRotationLabel)
                smoothRotationLabel.text = $"부드러운 시야 : {(_data.smoothRotation ? "켜짐" : "꺼짐")}";
        }

        void ApplySensitivity(float v)
        {
            if (playerStat == null) return;
            playerStat.sensitivity = v;
        }
        void ApplySmoothRotation(bool on)
        {
            if (playerStat == null) return;
            playerStat.useSmoothSpeed = on;
        }
    }
}