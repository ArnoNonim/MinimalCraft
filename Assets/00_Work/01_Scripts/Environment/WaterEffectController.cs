using _00_Work._01_Scripts.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _00_Work._01_Scripts.Environment
{
    /// <summary>
    /// 물속 진입/탈출 시 왜곡 + 안개 + PP 전환
    /// PlayerMovement.IsInWater 참조
    /// </summary>
    public class WaterEffectController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Volume         mainVolume;

        [Header("전환 속도")]
        [SerializeField] private float transitionSpeed = 3f;

        [Header("─── 물속 왜곡 설정 ───")]
        [Tooltip("물속 왜곡 최대 강도")]
        [SerializeField] private float waterDistortionMax = 1.8f;

        [Tooltip("물속 안개 최대 밀도 (0~0.6)")]
        [SerializeField] private float waterFogDensityMax = 0.25f;

        [Tooltip("물속 안개 색상")]
        [SerializeField] private Color waterFogColor = new Color(0.05f, 0.2f, 0.4f, 1f);

        [Header("─── 물속 PP 설정 ───")]
        [SerializeField] private float waterExposure    = -0.8f;
        [SerializeField] private float waterSaturation  = -15f;
        [SerializeField] private Color waterColorFilter = new Color(0.4f, 0.7f, 1.0f);

        // ── 원본 PP값 ────────────────────────────────────────────────
        private float _origExposure;
        private float _origSaturation;
        private Color _origColorFilter;
        private bool  _origSaved;

        private ColorAdjustments _colorAdj;

        // ── 상태 ────────────────────────────────────────────────────
        private float _currentT;
        private float _targetT;

        // ──────────────────────────────────────────────

        void Awake()
        {
            if (mainVolume != null)
                mainVolume.profile.TryGet(out _colorAdj);

            SaveOriginals();

            // 물속 안개 색상 초기 세팅
            if (WaterDistortionFeature.Instance != null)
                WaterDistortionFeature.Instance.settings.fogColor = waterFogColor;
        }

        void OnDestroy() => RestoreOriginals();
        void OnDisable() => RestoreOriginals();

        void Update()
        {
            if (playerMovement == null) return;

            _targetT = playerMovement.IsInWater ? 1f : 0f;

            _currentT = Mathf.MoveTowards(
                _currentT, _targetT,
                transitionSpeed * Time.deltaTime);

            ApplyEffects(_currentT);
        }

        // ──────────────────────────────────────────────

        void SaveOriginals()
        {
            if (_origSaved || _colorAdj == null) return;
            _origExposure    = _colorAdj.postExposure.value;
            _origSaturation  = _colorAdj.saturation.value;
            _origColorFilter = _colorAdj.colorFilter.value;
            _origSaved = true;
        }

        void RestoreOriginals()
        {
            if (!_origSaved || _colorAdj == null) return;
            _colorAdj.postExposure.Override(_origExposure);
            _colorAdj.saturation.Override(_origSaturation);
            _colorAdj.colorFilter.Override(_origColorFilter);

            if (WaterDistortionFeature.Instance != null)
                WaterDistortionFeature.Instance.SetWaterEffect(0f, 0f);

            _currentT = 0f;
            _targetT  = 0f;
        }

        void ApplyEffects(float t)
        {
            // 왜곡 + 안개
            if (WaterDistortionFeature.Instance != null)
                WaterDistortionFeature.Instance.SetWaterEffect(
                    Mathf.Lerp(0f, waterDistortionMax, t),
                    Mathf.Lerp(0f, waterFogDensityMax, t));

            // PP
            if (_colorAdj != null)
            {
                _colorAdj.postExposure.Override(
                    Mathf.Lerp(_origExposure,   waterExposure,   t));
                _colorAdj.saturation.Override(
                    Mathf.Lerp(_origSaturation, waterSaturation, t));
                _colorAdj.colorFilter.Override(
                    Color.Lerp(_origColorFilter, waterColorFilter, t));
            }
        }
    }
}