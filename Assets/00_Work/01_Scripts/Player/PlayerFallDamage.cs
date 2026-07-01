using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerFallDamage : MonoBehaviour
    {
        [Tooltip("이 속도(m/s) 이하로 착지하면 데미지 없음")]
        [SerializeField] private float safeVelocity = 10f;

        [Tooltip("안전 속도 초과분 1m/s당 데미지 배율")]
        [SerializeField] private float damageMultiplier = 1.5f;

        [Tooltip("1회 낙하로 받을 수 있는 최대 데미지")]
        [SerializeField] private int maxFallDamage = 20;

        private PlayerStats    _stats;
        private PlayerMovement _movement;

        private bool  _wasGrounded;
        private float _peakFallVelocity; // 낙하 중 최대 하강 속도 추적

        private void Awake()
        {
            _stats    = GetComponent<PlayerStats>();
            _movement = GetComponentInParent<PlayerMovement>();
        }

        private void Update()
        {
            bool isGrounded = _movement.IsGrounded;
            float verticalVel = _movement.VerticalVelocity;

            // 공중에 있을 때 최대 하강 속도 기록
            if (!isGrounded && verticalVel < 0f)
                _peakFallVelocity = Mathf.Max(_peakFallVelocity, Mathf.Abs(verticalVel));

            // 공중 → 착지 순간 감지
            if (!_wasGrounded && isGrounded)
                OnLanded();

            _wasGrounded = isGrounded;
        }

        private void OnLanded()
        {
            if (_peakFallVelocity > safeVelocity)
            {
                float excess = _peakFallVelocity - safeVelocity;
                int damage   = Mathf.Min(
                    Mathf.RoundToInt(excess * damageMultiplier),
                    maxFallDamage);

                _stats.TakeDamage(damage);
            }

            _peakFallVelocity = 0f;
        }
    }
}