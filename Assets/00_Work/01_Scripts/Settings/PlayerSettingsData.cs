using System;

namespace _00_Work._01_Scripts.Settings
{
    [Serializable]
    public class PlayerSettingsData
    {
        // ── 비디오 ────────────────────────────────────────────────────
        public float fov              = 60f;
        public int   maxFrameRate     = -1;
        public int   renderDistance   = 8;
        public int   resolutionWidth  = 1920;
        public int   resolutionHeight = 1080;
        public int   refreshRate      = 60;
        public bool  isFullScreen     = true;
        public int   graphicsLevel    = 2;    // 0=낮음 1=보통 2=화려하게
        public bool  vSync            = false;
        public int   guiScale         = 3;
        public bool  motionBlur       = true;

        // ── 오디오 ────────────────────────────────────────────────────
        public float masterVolume = 1f;
        public float bgmVolume    = 1f;
        public float sfxVolume    = 1f;

        // ── 마우스 ────────────────────────────────────────────────────
        public float sensitivity      = 0.2f;
        public bool  smoothRotation   = true;

        // ── 키 바인딩 ─────────────────────────────────────────────────
        public string keybindOverrides = ""; // InputActionAsset JSON 오버라이드
    }
}