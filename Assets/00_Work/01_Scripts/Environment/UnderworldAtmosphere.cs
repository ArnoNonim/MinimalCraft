using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _00_Work._01_Scripts.Environment
{
    /// <summary>
    /// 지하 분위기 시스템
    ///
    /// [판정] 플레이어 머리 위 스캔 + 깊이(Y좌표) 복합 판정
    ///        → 깊을수록 빛이 위에서 내려와도 어두워짐
    /// [연출] 기존 Volume 오버라이드 값을 저장 후
    ///        지하 진입 시 Inspector 설정값으로 MoveTowards 전환
    ///        지상 복귀 시 저장된 원본값으로 복원
    /// </summary>
    public class UnderworldAtmosphere : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Transform    playerTransform;
        [SerializeField] private ChunkManager chunkManager;

        [Tooltip("평소에 사용 중인 Global Volume (PP 설정 저장/복원 대상)")]
        [SerializeField] private Volume mainVolume;

        [Header("지하 판정")]
        [SerializeField] private float checkInterval   = 0.3f;
        [SerializeField] private int   scanRange       = 80;
        [SerializeField] private int   minBlockedCount = 3;

        [Tooltip("이 Y 이하면 스캔 결과 무관하게 무조건 지하 취급 (깊이 강제 어둠)")]
        [SerializeField] private float forceUndergroundY = 0f;

        [Header("전환 속도")]
        [SerializeField] private float transitionSpeed = 0.8f;

        [Header("─── 지하 화면 왜곡 설정 ───")]
        [Tooltip("지하 최대 왜곡 강도 (0이면 비활성화)")]
        [SerializeField] private float undergroundDistortionMax = 2.5f;

        [Header("─── 지하 PP 설정값 (Inspector에서 자유롭게 조절) ───")]

        [Tooltip("밝기 조정 (음수일수록 어두움, -5 이하 권장)")]
        [SerializeField] private float undergroundExposure = -5f;

        [Tooltip("대비 (-100~100, 높을수록 명암 강조)")]
        [SerializeField] private float undergroundContrast = 30f;

        [Tooltip("채도 (-100~100, 음수일수록 무채색)")]
        [SerializeField] private float undergroundSaturation = -40f;

        [Tooltip("색조 보정 (어두운 파랑/초록 느낌 주려면 음수)")]
        [SerializeField] private Color undergroundColorFilter = new Color(0.5f, 0.6f, 0.8f);

        [Tooltip("비네트 강도 (0~1, 주변부 어둠)")]
        [SerializeField] private float undergroundVignetteIntensity = 0.55f;

        [Tooltip("깊이별 최소 어둠 — 이 Y 이하면 지상에서 빛이 내려와도 이 값 이상 유지")]
        [SerializeField] private float deepY    = -15f;
        [Tooltip("이 Y 이상은 지하여도 약하게만 어두워짐")]
        [SerializeField] private float shallowY = 40f;

        // ── 저장된 원본값 ──────────────────────────────────────────────
        private float _origExposure;
        private float _origContrast;
        private float _origSaturation;
        private Color _origColorFilter;
        private float _origVignetteIntensity;
        private bool  _origSaved;

        // ── URP 컴포넌트 참조 ─────────────────────────────────────────
        private ColorAdjustments _colorAdj;
        private Vignette         _vignette;

        // ── 상태 ──────────────────────────────────────────────────────
        private float _checkTimer;
        private float _currentT; // 현재 보간값 0~1
        private float _targetT;  // 목표 보간값 0~1

        // ──────────────────────────────────────────────
        // Unity 이벤트
        // ──────────────────────────────────────────────

        void Awake()
        {
            if (mainVolume == null) return;
            if (!mainVolume.profile.TryGet(out _colorAdj)) _colorAdj = null;
            if (!mainVolume.profile.TryGet(out _vignette))  _vignette = null;

            SaveOriginalValues();
        }

        void OnDestroy() => RestoreOriginalValues();
        void OnDisable() => RestoreOriginalValues();

        void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= checkInterval)
            {
                _checkTimer = 0f;
                UpdateTarget();
            }

            // 매 프레임 현재값을 목표로 부드럽게 추적
            _currentT = Mathf.MoveTowards(
                _currentT, _targetT,
                transitionSpeed * Time.deltaTime);

            ApplyPP(_currentT);
        }

        // ──────────────────────────────────────────────
        // 원본 저장 / 복원
        // ──────────────────────────────────────────────

        void SaveOriginalValues()
        {
            if (_origSaved) return;

            _origExposure         = _colorAdj != null ? _colorAdj.postExposure.value    : 0f;
            _origContrast         = _colorAdj != null ? _colorAdj.contrast.value        : 0f;
            _origSaturation       = _colorAdj != null ? _colorAdj.saturation.value      : 0f;
            _origColorFilter      = _colorAdj != null ? _colorAdj.colorFilter.value     : Color.white;
            _origVignetteIntensity = _vignette != null ? _vignette.intensity.value       : 0f;

            _origSaved = true;
        }

        void RestoreOriginalValues()
        {
            if (!_origSaved) return;
            ApplyValues(
                _origExposure, _origContrast, _origSaturation,
                _origColorFilter, _origVignetteIntensity);

            // 왜곡 0으로 복원
            if (UnderworldDistortionFeature.Instance != null)
                UnderworldDistortionFeature.Instance.SetIntensity(0f);

            _currentT = 0f;
            _targetT  = 0f;
        }

        // ──────────────────────────────────────────────
        // 판정
        // ──────────────────────────────────────────────

        void UpdateTarget()
        {
            if (chunkManager == null || playerTransform == null)
            {
                _targetT = 0f;
                return;
            }

            Vector3 pos = playerTransform.position;

            // ── 깊이 강제 판정 ────────────────────────────────────────
            // forceUndergroundY 이하면 스캔 무관하게 깊이 기반 T 적용
            float depthT = Mathf.Clamp01(
                Mathf.InverseLerp(shallowY, deepY, pos.y));

            if (pos.y <= forceUndergroundY)
            {
                // 깊을수록 강하게, 최소 0.6 보장
                _targetT = Mathf.Max(0.6f, depthT);
                return;
            }

            // ── 스카이 스캔 ──────────────────────────────────────────
            int startY       = Mathf.FloorToInt(pos.y) + 2;
            int endY         = startY + scanRange;
            int blockedCount = 0;

            for (int y = startY; y <= endY; y++)
            {
                byte block = chunkManager.GetBlockAt(new Vector3(pos.x, y, pos.z));
                if (block != (byte)BlockType.Air  &&
                    block != (byte)BlockType.Water &&
                    block != 0)
                {
                    blockedCount++;
                    if (blockedCount >= minBlockedCount) break;
                }
            }

            bool underground = blockedCount >= minBlockedCount;

            if (!underground)
            {
                // 지상 — 완전 복원 (0)
                _targetT = 0f;
                return;
            }

            // 지하 — 깊이 기반 T, 최소 0.4 보장
            _targetT = Mathf.Max(0.4f, depthT);
        }

        // ──────────────────────────────────────────────
        // PP 적용
        // ──────────────────────────────────────────────

        /// <summary>t=0: 원본값, t=1: 지하 설정값으로 보간해 적용</summary>
        void ApplyPP(float t)
        {
            ApplyValues(
                Mathf.Lerp(_origExposure,          undergroundExposure,          t),
                Mathf.Lerp(_origContrast,           undergroundContrast,          t),
                Mathf.Lerp(_origSaturation,         undergroundSaturation,        t),
                Color.Lerp(_origColorFilter,        undergroundColorFilter,       t),
                Mathf.Lerp(_origVignetteIntensity,  undergroundVignetteIntensity, t)
            );

            // 화면 왜곡 강도 연동
            if (UnderworldDistortionFeature.Instance != null)
                UnderworldDistortionFeature.Instance.SetIntensity(
                    Mathf.Lerp(0f, undergroundDistortionMax, t));
        }

        void ApplyValues(float exposure, float contrast, float saturation,
                         Color colorFilter, float vignetteIntensity)
        {
            if (_colorAdj != null)
            {
                _colorAdj.postExposure.Override(exposure);
                _colorAdj.contrast.Override(contrast);
                _colorAdj.saturation.Override(saturation);
                _colorAdj.colorFilter.Override(colorFilter);
            }

            if (_vignette != null)
                _vignette.intensity.Override(vignetteIntensity);
        }
    }
}