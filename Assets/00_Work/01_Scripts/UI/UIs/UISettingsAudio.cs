using _00_Work._01_Scripts.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISettingsAudio : UIPopup
    {
        [Header("오디오 UI")]
        [SerializeField] private Slider   masterSlider;
        [SerializeField] private TMP_Text masterLabel;
        [SerializeField] private Slider   bgmSlider;
        [SerializeField] private TMP_Text bgmLabel;
        [SerializeField] private Slider   sfxSlider;
        [SerializeField] private TMP_Text sfxLabel;
        [Header("외부 참조")]
        [SerializeField] private AudioMixer audioMixer;

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
            if (masterSlider != null) { masterSlider.minValue = 0.0001f; masterSlider.maxValue = 1f; masterSlider.onValueChanged.RemoveAllListeners(); masterSlider.onValueChanged.AddListener(v => { _data.masterVolume = v; ApplyVolume("MasterVolume", v); if (masterLabel) masterLabel.text = $"전체 음량 : {Mathf.RoundToInt(v * 100)}%"; }); }
            if (bgmSlider    != null) { bgmSlider.minValue    = 0.0001f; bgmSlider.maxValue    = 1f; bgmSlider.onValueChanged.RemoveAllListeners();    bgmSlider.onValueChanged.AddListener(v    => { _data.bgmVolume    = v; ApplyVolume("BGMVolume",    v); if (bgmLabel)    bgmLabel.text    = $"배경 음악 : {Mathf.RoundToInt(v * 100)}%"; }); }
            if (sfxSlider    != null) { sfxSlider.minValue    = 0.0001f; sfxSlider.maxValue    = 1f; sfxSlider.onValueChanged.RemoveAllListeners();    sfxSlider.onValueChanged.AddListener(v    => { _data.sfxVolume    = v; ApplyVolume("SFXVolume",    v); if (sfxLabel)    sfxLabel.text    = $"효과음 : {Mathf.RoundToInt(v * 100)}%"; }); }
        }

        void Refresh()
        {
            if (_data == null) return;
            masterSlider?.SetValueWithoutNotify(_data.masterVolume);
            bgmSlider?.SetValueWithoutNotify(_data.bgmVolume);
            sfxSlider?.SetValueWithoutNotify(_data.sfxVolume);
            if (masterLabel) masterLabel.text = $"전체 음량 : {Mathf.RoundToInt(_data.masterVolume * 100)}%";
            if (bgmLabel)    bgmLabel.text    = $"배경 음악 : {Mathf.RoundToInt(_data.bgmVolume * 100)}%";
            if (sfxLabel)    sfxLabel.text    = $"효과음 : {Mathf.RoundToInt(_data.sfxVolume * 100)}%";
            ApplyAll();
        }

        void ApplyAll()
        {
            if (_data == null) return;
            ApplyVolume("MasterVolume", _data.masterVolume);
            ApplyVolume("BGMVolume",    _data.bgmVolume);
            ApplyVolume("SFXVolume",    _data.sfxVolume);
        }

        void ApplyVolume(string param, float value) => audioMixer?.SetFloat(param, Mathf.Log10(value) * 20f);
        void ApplySpeakerMode(int mode)
        {
            var cfg = AudioSettings.GetConfiguration();
            if (cfg.speakerMode == (AudioSpeakerMode)mode) return; // ← 같으면 리셋 안 함
            cfg.speakerMode = (AudioSpeakerMode)mode;
            AudioSettings.Reset(cfg);
        }
    }
}