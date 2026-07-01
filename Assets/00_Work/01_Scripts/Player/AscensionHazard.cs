using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _00_Work._01_Scripts.Player
{
    /// <summary>
    /// 상승 부하 시스템
    /// 위로 이동할 때만 깊이 단계별 부하 발동
    ///
    /// 1단계: 이동속도 감소 + 체력 서서히 감소
    /// 2단계: 이동속도 더 감소 + 체력 빠르게 감소 + 빨간 비네트
    /// 3단계: 이동속도 대폭 감소 + 체력 급감 + 강한 비네트 → 사망
    /// </summary>
    public class AscensionHazard : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerMovement   playerMovement;
        [SerializeField] private PlayerStats      playerStats;
        [SerializeField] private PlayerStatSO     stat;
        [SerializeField] private UIDepthIndicator depthIndicator;
        [SerializeField] private Volume           mainVolume;

        [Header("1단계 설정")]
        [Tooltip("이동속도 배율 (0~1)")]
        [SerializeField] private float speedMul1     = 0.7f;
        [Tooltip("칸마다 체력 감소량")]
        [SerializeField] private int   hpDrain1      = 2;

        [Header("2단계 설정")]
        [SerializeField] private float speedMul2     = 0.45f;
        [SerializeField] private int   hpDrain2      = 4;
        [SerializeField] private float vigIntensity2 = 0.35f;

        [Header("3단계 설정")]
        [SerializeField] private float speedMul3     = 0.2f;
        [SerializeField] private int   hpDrain3      = 8;
        [SerializeField] private float vigIntensity3 = 0.65f;

        [Header("비네트 전환 속도")]
        [SerializeField] private float vigTransitionSpeed = 2.5f;

        // ── PP ──────────────────────────────────────────────────────
        private Vignette _vignette;
        private float    _origVigIntensity;

        // ── 원본 이동속도 ────────────────────────────────────────────
        private float _origWalkSpeed;
        private float _origSprintSpeed;
        private bool  _origSaved;

        [Header("상승 데미지 기준 (칸)")]
        [Tooltip("1단계: 이 칸 이상 올라갈 때마다 데미지")]
        [SerializeField] private float damageDistInterval1 = 10f;
        [Tooltip("2단계")]
        [SerializeField] private float damageDistInterval2 = 7f;
        [Tooltip("3단계")]
        [SerializeField] private float damageDistInterval3 = 5f;

        // ── 상태 ────────────────────────────────────────────────────
        private float _targetVig;
        private bool  _wasAscending;
        private float _hpDrainAccum;     // 누적 상승 거리
        private float _lastDamageY;      // 마지막 데미지 시점 y좌표
        private bool  _lastDamageYSet;

        // ──────────────────────────────────────────────

        void Awake()
        {
            if (mainVolume != null)
                mainVolume.profile.TryGet(out _vignette);
        }

        void Start()
        {
            // Start에서 저장 — stat 초기화 완료 후
            SaveOriginals();
        }

        void OnDestroy() => RestoreOriginals();
        void OnDisable() => RestoreOriginals();

        void Update()
        {
            if (playerMovement == null || depthIndicator == null || stat == null) return;

            var  hazard      = depthIndicator.CurrentHazard;
            bool isAscending = playerMovement.VerticalVelocity > 0.1f;

            bool hazardActive = isAscending &&
                                hazard != UIDepthIndicator.HazardLevel.Safe;

            if (!hazardActive)
            {
                RestoreSpeed();
                _targetVig      = _origVigIntensity;
                
                // Safe 상태일 때만 리셋
                if (hazard == UIDepthIndicator.HazardLevel.Safe)
                    _lastDamageYSet = false;
            }
            else
            {
                ApplyHazard(hazard);
            }

            // 비네트 부드럽게 전환
            if (_vignette != null)
            {
                float next = Mathf.MoveTowards(
                    _vignette.intensity.value,
                    _targetVig,
                    vigTransitionSpeed * Time.deltaTime);
                _vignette.intensity.Override(next);
            }

            _wasAscending = isAscending;
        }

        // ──────────────────────────────────────────────

        void ApplyHazard(UIDepthIndicator.HazardLevel hazard)
        {
            switch (hazard)
            {
                case UIDepthIndicator.HazardLevel.Hazard1:
                    SetSpeed(speedMul1);
                    DrainHP(hazard);
                    _targetVig = _origVigIntensity;
                    break;

                case UIDepthIndicator.HazardLevel.Hazard2:
                    SetSpeed(speedMul2);
                    DrainHP(hazard);
                    _targetVig = vigIntensity2;
                    break;

                case UIDepthIndicator.HazardLevel.Hazard3:
                    SetSpeed(speedMul3);
                    DrainHP(hazard);
                    _targetVig = vigIntensity3;
                    break;
            }
        }

        void DrainHP(UIDepthIndicator.HazardLevel hazard)
        {
            if (playerStats == null || playerMovement == null) return;

            float currentY = playerMovement.transform.position.y;
            float interval = hazard switch
            {
                UIDepthIndicator.HazardLevel.Hazard1 => damageDistInterval1,
                UIDepthIndicator.HazardLevel.Hazard2 => damageDistInterval2,
                UIDepthIndicator.HazardLevel.Hazard3 => damageDistInterval3,
                _                                    => float.MaxValue
            };

            // 첫 진입 시 기준점 설정
            if (!_lastDamageYSet)
            {
                _lastDamageY    = currentY;
                _lastDamageYSet = true;
                return;
            }

            float ascended = currentY - _lastDamageY;
            
            if (ascended < interval) return;

            // interval만큼 올라갈 때마다 데미지
            int times = Mathf.FloorToInt(ascended / interval);
            _lastDamageY += interval * times;

            int dmg = hazard switch
            {
                UIDepthIndicator.HazardLevel.Hazard1 => hpDrain1,
                UIDepthIndicator.HazardLevel.Hazard2 => hpDrain2,
                UIDepthIndicator.HazardLevel.Hazard3 => hpDrain3,
                _                                    => 0
            };

            playerStats.TakeDamage(dmg * times);
        }

        // ──────────────────────────────────────────────

        void SaveOriginals()
        {
            if (_origSaved || stat == null) return;
            _origWalkSpeed    = stat.walkSpeed;
            _origSprintSpeed  = stat.sprintSpeed;
            _origVigIntensity = _vignette?.intensity.value ?? 0f;
            _targetVig        = _origVigIntensity;
            _origSaved        = true;
        }

        void SetSpeed(float mul)
        {
            if (!_origSaved || stat == null) return;
            stat.walkSpeed   = _origWalkSpeed   * mul;
            stat.sprintSpeed = _origSprintSpeed * mul;
        }

        void RestoreSpeed()
        {
            if (!_origSaved || stat == null) return;
            stat.walkSpeed   = _origWalkSpeed;
            stat.sprintSpeed = _origSprintSpeed;
        }

        void RestoreOriginals()
        {
            if (!_origSaved) return;
            RestoreSpeed();
            if (_vignette != null)
                _vignette.intensity.Override(_origVigIntensity);
        }
    }
}