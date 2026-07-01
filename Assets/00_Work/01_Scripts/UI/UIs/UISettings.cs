using _00_Work._01_Scripts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISettings : UIPopup
    {
        [Header("카테고리 버튼")]
        [SerializeField] private Button videoButton;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button keybindingButton;
        [SerializeField] private Button mouseButton;

        [Header("서브 패널 (UIPopup)")]
        [SerializeField] private UISettingsVideo       videoSettings;
        [SerializeField] private UISettingsAudio       audioSettings;
        [SerializeField] private UISettingsKeybindings keybindingSettings;
        [SerializeField] private UISettingsMouse       mouseSettings;

        private PlayerSettingsData _data;
        private UIPopup            _activeSubPanel;

        protected override void Awake()
        {
            base.Awake();

            PlayerSettingsSaver.SaveDirectory = System.IO.Path.Combine(
                Application.persistentDataPath, "PlayerSettings");

            videoButton.onClick.AddListener(     () => ShowSubPanel(videoSettings));
            audioButton.onClick.AddListener(     () => ShowSubPanel(audioSettings));
            keybindingButton.onClick.AddListener(() => ShowSubPanel(keybindingSettings));
            mouseButton.onClick.AddListener(     () => ShowSubPanel(mouseSettings));
        }

        protected override void OnOpen()
        {
            _data = PlayerSettingsSaver.Load();
            videoSettings?.InitData(_data);
            audioSettings?.InitData(_data);
            keybindingSettings?.InitData(_data);
            mouseSettings?.InitData(_data);
        }

        protected override void OnClose()
        {
            _activeSubPanel?.Close();
            _activeSubPanel = null;

            PlayerSettingsSaver.Save(_data);
        }

        void ShowSubPanel(UIPopup panel)
        {
            if (panel == null) return;

            if (_activeSubPanel == panel)
            {
                // 같은 패널이면 토글
                if (panel.IsOpen) return;
                // 닫혀있으면 다시 열기 허용
            }
            else
            {
                _activeSubPanel?.Close();
            }

            _activeSubPanel = panel;
            panel.Open();
        }

        public void LoadSettingsOnStartup()
        {
            PlayerSettingsSaver.SaveDirectory = System.IO.Path.Combine(
                Application.persistentDataPath, "PlayerSettings");
            _data = PlayerSettingsSaver.Load();
            videoSettings?.ApplyData(_data);
            audioSettings?.ApplyData(_data);
            mouseSettings?.ApplyData(_data);
        }
    }
}