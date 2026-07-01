using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Settings;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISettingsVideo : UIPopup
    {
        private static readonly int[] HzOptions        = { 60, 120, 144, 240 };
        private static readonly int[] FrameRateOptions = { 30, 60, 120, 144, 165, 240, -1 };
        private static readonly string[] GraphicsLabels = { "낮음", "보통", "화려하게" };

        [System.Serializable]
        private struct ResolutionOption
        {
            public int width, height, hz;
            public string Label => $"{width} x {height}  {hz}hz";
        }

        [Header("비디오 UI")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Slider       fovSlider;
        [SerializeField] private TMP_Text     fovLabel;
        [SerializeField] private Slider       frameRateSlider;
        [SerializeField] private TMP_Text     frameRateLabel;
        [SerializeField] private Slider       renderDistanceSlider;
        [SerializeField] private TMP_Text     renderDistanceLabel;
        [SerializeField] private Button       graphicsButton;
        [SerializeField] private TMP_Text     graphicsLabel;
        [SerializeField] private Button       vsyncButton;
        [SerializeField] private TMP_Text     vsyncLabel;
        [SerializeField] private Slider       guiScaleSlider;
        [SerializeField] private TMP_Text     guiScaleLabel;
        [SerializeField] private Button       motionBlurButton;
        [SerializeField] private TMP_Text     motionBlurLabel;
        [SerializeField] private Toggle       fullscreenToggle;

        [Header("외부 참조")]
        [SerializeField] private CinemachineCamera[] playerCameras;
        [SerializeField] private ChunkManager        chunkManager;
        [SerializeField] private Volume              postProcessVolume;
        [SerializeField] private CanvasScaler        uiCanvasScaler;

        private readonly List<ResolutionOption> _resolutions = new();
        private PlayerSettingsData _data;
        private int                _currentResolutionIndex;
        private bool               _initialized;

        void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }
        
        public void InitData(PlayerSettingsData data)
        {
            _data = data;
            if (!_initialized)
            {
                InitResolutions();
                InitSliders();
                _initialized = true;
            }
            Refresh();
        }

        public void ApplyData(PlayerSettingsData data)
        {
            _data = data;
            ApplyAll();
        }

        void InitResolutions()
        {
            _resolutions.Clear();
            var seen  = new HashSet<string>();
            var sizes = new HashSet<(int, int)>();

            foreach (Resolution res in Screen.resolutions)
                sizes.Add((res.width, res.height));

            foreach (var (w, h) in sizes)
            foreach (int hz in HzOptions)
            {
                string key = $"{w}x{h}x{hz}";
                if (!seen.Add(key)) continue;
                _resolutions.Add(new ResolutionOption { width = w, height = h, hz = hz });
            }

            _resolutions.Sort((a, b) =>
            {
                int area = (b.width * b.height).CompareTo(a.width * a.height);
                return area != 0 ? area : a.hz.CompareTo(b.hz);
            });

            if (resolutionDropdown == null) return;
            resolutionDropdown.ClearOptions();
            var options = new List<string>();

            for (int i = 0; i < _resolutions.Count; i++)
            {
                options.Add(_resolutions[i].Label);
                if (_resolutions[i].width  == _data.resolutionWidth &&
                    _resolutions[i].height == _data.resolutionHeight &&
                    _resolutions[i].hz     == _data.refreshRate)
                    _currentResolutionIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(_currentResolutionIndex);
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        void InitSliders()
        {
            if (fovSlider != null)
            {
                fovSlider.minValue = 30f; fovSlider.maxValue = 120f; fovSlider.wholeNumbers = true;
                fovSlider.onValueChanged.RemoveAllListeners();
                fovSlider.onValueChanged.AddListener(OnFovChanged);
            }
            if (frameRateSlider != null)
            {
                frameRateSlider.minValue = 0; frameRateSlider.maxValue = FrameRateOptions.Length - 1;
                frameRateSlider.wholeNumbers = true;
                frameRateSlider.onValueChanged.RemoveAllListeners();
                frameRateSlider.onValueChanged.AddListener(OnFrameRateChanged);
            }
            if (renderDistanceSlider != null)
            {
                renderDistanceSlider.minValue = 2; renderDistanceSlider.maxValue = 16;
                renderDistanceSlider.wholeNumbers = true;
                renderDistanceSlider.onValueChanged.RemoveAllListeners();
                renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceChanged);
            }
            if (guiScaleSlider != null)
            {
                guiScaleSlider.minValue = 1; guiScaleSlider.maxValue = 6;
                guiScaleSlider.wholeNumbers = true;
                guiScaleSlider.onValueChanged.RemoveAllListeners();
                guiScaleSlider.onValueChanged.AddListener(OnGuiScaleChanged);
            }
            if (guiScaleSlider != null)
            {
                guiScaleSlider.minValue    = 1f;
                guiScaleSlider.maxValue    = 3f;
                guiScaleSlider.wholeNumbers = false;  // 소수점 허용
                guiScaleSlider.onValueChanged.RemoveAllListeners();
                guiScaleSlider.onValueChanged.AddListener(OnGuiScaleChanged);
            }
            graphicsButton?.onClick.AddListener(OnGraphicsClicked);
            vsyncButton?.onClick.AddListener(OnVSyncClicked);
            motionBlurButton?.onClick.AddListener(OnMotionBlurClicked);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
        }

        void Refresh()
        {
            if (_data == null) return;
            if (fovSlider         != null) fovSlider.SetValueWithoutNotify(_data.fov);
            if (fovLabel          != null) fovLabel.text = $"FOV : {(int)_data.fov}";
            int fpsIdx = System.Array.IndexOf(FrameRateOptions, _data.maxFrameRate);
            if (frameRateSlider   != null) frameRateSlider.SetValueWithoutNotify(fpsIdx >= 0 ? fpsIdx : FrameRateOptions.Length - 1);
            if (frameRateLabel    != null) frameRateLabel.text = _data.maxFrameRate == -1 ? "최대 프레임률 : 무한" : $"최대 프레임률 : {_data.maxFrameRate}";
            if (renderDistanceSlider != null) renderDistanceSlider.SetValueWithoutNotify(_data.renderDistance);
            if (renderDistanceLabel  != null) renderDistanceLabel.text = $"렌더 거리 : 청크 {_data.renderDistance}개";
            if (graphicsLabel     != null) graphicsLabel.text  = $"그래픽 : {GraphicsLabels[Mathf.Clamp(_data.graphicsLevel, 0, 2)]}";
            if (vsyncLabel        != null) vsyncLabel.text     = $"VSync : {(_data.vSync ? "활성화" : "비활성화")}";
            if (motionBlurLabel   != null) motionBlurLabel.text= $"모션 블러 : {(_data.motionBlur ? "켜기" : "끄기")}";
            if (guiScaleSlider    != null) guiScaleSlider.SetValueWithoutNotify(_data.guiScale);
            if (guiScaleLabel     != null) guiScaleLabel.text  = $"GUI 크기 : {_data.guiScale}";
            fullscreenToggle?.SetIsOnWithoutNotify(_data.isFullScreen);
            ApplyAll();
        }

        void ApplyAll()
        {
            if (_data == null) return;
            ApplyFov(_data.fov);
            ApplyFrameRate(_data.maxFrameRate);
            ApplyRenderDistance(_data.renderDistance);
            ApplyVSync(_data.vSync);
            ApplyMotionBlur(_data.motionBlur);
        }

        void OnFovChanged(float v)               { _data.fov = v; ApplyFov(v); if (fovLabel != null) fovLabel.text = $"FOV : {(int)v}"; }
        void OnFrameRateChanged(float v)         { int fps = FrameRateOptions[(int)v]; _data.maxFrameRate = fps; ApplyFrameRate(fps); if (frameRateLabel != null) frameRateLabel.text = fps == -1 ? "최대 프레임률 : 무한" : $"최대 프레임률 : {fps}"; }
        void OnRenderDistanceChanged(float v)    { _data.renderDistance = (int)v; ApplyRenderDistance((int)v); if (renderDistanceLabel != null) renderDistanceLabel.text = $"렌더 거리 : 청크 {(int)v}개"; }
        void OnGuiScaleChanged(float v)
        {
            _data.guiScale = (int)v;
            ApplyGuiScale((int)v);
            if (guiScaleLabel != null)
                guiScaleLabel.text = $"GUI 크기 : {(int)v}";
        }

        void ApplyGuiScale(int scale)
        {
            if (uiCanvasScaler != null)
                uiCanvasScaler.scaleFactor = scale / 3f;
        }
        
        void OnResolutionChanged(int i)          { _currentResolutionIndex = i; var r = _resolutions[i]; _data.resolutionWidth = r.width; _data.resolutionHeight = r.height; _data.refreshRate = r.hz; Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, new RefreshRate { numerator = (uint)r.hz, denominator = 1 }); }
        void OnFullscreenChanged(bool on)        { _data.isFullScreen = on; Screen.fullScreenMode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; }

        void OnGraphicsClicked()
        {
            _data.graphicsLevel = (_data.graphicsLevel + 1) % 3;
            QualitySettings.SetQualityLevel(_data.graphicsLevel);
            if (graphicsLabel != null) graphicsLabel.text = $"그래픽 : {GraphicsLabels[_data.graphicsLevel]}";
        }

        void OnVSyncClicked()
        {
            _data.vSync = !_data.vSync;
            ApplyVSync(_data.vSync);
            if (vsyncLabel != null) vsyncLabel.text = $"VSync : {(_data.vSync ? "활성화" : "비활성화")}";
        }

        void OnMotionBlurClicked()
        {
            _data.motionBlur = !_data.motionBlur;
            ApplyMotionBlur(_data.motionBlur);
            if (motionBlurLabel != null) motionBlurLabel.text = $"모션 블러 : {(_data.motionBlur ? "켜기" : "끄기")}";
        }

        void ApplyFov(float fov)
        {
            if (playerCameras == null) return;
            foreach (var c in playerCameras)
                if (c != null) c.Lens.FieldOfView = fov;
        }
        
        void ApplyFrameRate(int fps)         => Application.targetFrameRate = fps;
        void ApplyRenderDistance(int dist)   { if (chunkManager != null) chunkManager.renderDistance = dist; }
        void ApplyVSync(bool on)             => QualitySettings.vSyncCount = on ? 1 : 0;
        void ApplyMotionBlur(bool on)
        {
            if (postProcessVolume == null) return;
            if (postProcessVolume.profile.TryGet<MotionBlur>(out var mb)) mb.active = on;
        }
    }
}