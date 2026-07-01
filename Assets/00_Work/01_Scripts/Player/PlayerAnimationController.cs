using _00_Work._01_Scripts.Block;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Sound;
using UnityEngine;
using UnityEngine.Serialization;

namespace _00_Work._01_Scripts.Player
{
    internal class PlayerAnimationController : MonoBehaviour
    {
        [Header("참조")]
        public ArtifactBreaker artifactBreaker;
        public Animator       animator;
        public PlayerMovement playerMovement;
        public PlayerStatSO   stat;
        public PlayerInputSO  playerInput;
        public BlockBreaker   blockBreaker;

        [Header("설정")]
        public float animationDampTime = 0.1f;

        [Header("사운드")]
        public string walkSound;
        public string jumpSound;
        public string landSound;
        public string swingSound;

        private static readonly int MovementSpeedHash =
            Animator.StringToHash("MovementSpeed");
        private static readonly int IsMiningHash =
            Animator.StringToHash("IsMining");
        private static readonly int PunchHash =
            Animator.StringToHash("Punch");
        private static readonly int IsJumpingHash =
            Animator.StringToHash("IsJumping");
        private static readonly int IsFallingHash =
            Animator.StringToHash("IsFalling");
        private static readonly int IsCrouchingHash =
            Animator.StringToHash("IsCrouching");

        private bool _isSprinting;
        
        private bool _wasGrounded = true;
        
        private bool _wasBreakPressed;
        private bool _attackTriggered;
        void OnSprintStart()  => _isSprinting = true;
        void OnSprintCancel() => _isSprinting = false;

        void Update()
        {
            UpdateMovementAnimation();
            UpdateUpperBodyAnimation();
        }

        void UpdateMovementAnimation()
        {
            float speed = new Vector2(
                playerMovement.MoveDir.x,
                playerMovement.MoveDir.z).magnitude;

            bool isMoving = speed > 0.1f;

            float normalizedSpeed;
            if (isMoving && _isSprinting)
                normalizedSpeed = 0.5f;
            else if (isMoving && playerMovement.IsCrouching)
                normalizedSpeed = 0.15f; // ← 웅크리기 이동은 더 느리게
            else if (isMoving)
                normalizedSpeed = 0.3f;
            else
                normalizedSpeed = 0f;

            animator.SetFloat(
                MovementSpeedHash,
                normalizedSpeed,
                animationDampTime,
                Time.deltaTime);

            animator.speed = (isMoving && _isSprinting)
                ? stat.sprintSpeed / stat.walkSpeed
                : 1f;

            bool isGrounded = playerMovement.IsGrounded;
            float verticalVel = playerMovement.VerticalVelocity;

            // 상승 중
            bool isRising = !isGrounded && verticalVel > 0.1f;
            // 하강 중
            bool isFalling = !isGrounded && verticalVel < -0.1f;

            animator.SetBool(IsJumpingHash, isRising);
            animator.SetBool(IsFallingHash, isFalling);
            animator.SetBool(IsCrouchingHash, playerMovement.IsCrouching);

            _wasGrounded = isGrounded;
        }

        void UpdateUpperBodyAnimation()
        {
            bool isBreaking = blockBreaker.IsBreaking || (artifactBreaker != null && artifactBreaker.IsBreaking);
            // 채굴 중 — IsMining
            animator.SetBool(IsMiningHash, isBreaking);

            // 채굴 시작되면 Attack 트리거 취소
            if (isBreaking)
            {
                _attackTriggered = true; // 채굴 중엔 트리거 막음
                animator.ResetTrigger(PunchHash);
                return;
            }

            // 허공 클릭 — 아직 트리거 안 됐고 마우스 눌린 상태
            if (_wasBreakPressed && !_attackTriggered)
            {
                _attackTriggered = true;
                animator.SetTrigger(PunchHash);
            }
        }

        void OnAttackDown()
        {
            _wasBreakPressed = true;
            _attackTriggered = false;
        }

        void OnAttackUp() => _wasBreakPressed = false;

        // 애니메이션 이벤트에서 호출
        void Step()
        {
            if (playerMovement.MoveDir.magnitude <= 0.1f || !playerMovement.IsGrounded) return;
            
            string sound = walkSound;

            SoundManager.Instance.PlaySFXAt(
                sound, transform,
                volume: _isSprinting ? 0.4f : 0.3f);
        }
        
        void Jump()
        {
            SoundManager.Instance.PlaySFXAt(jumpSound, transform, 0.6f);
        }
        
        void Land()
        {
            SoundManager.Instance.PlaySFXAt(landSound, transform, 0.3f);
        }

        void Swing()
        {
            SoundManager.Instance.PlaySFXAt(swingSound, transform);
        }
        
        void OnEnable()
        {
            playerInput.OnSprintKeyDown  += OnSprintStart;
            playerInput.OnSprintKeyUp    += OnSprintCancel;
            playerInput.OnAttackKeyDown  += OnAttackDown;
            playerInput.OnAttackKeyUp    += OnAttackUp;
        }

        void OnDisable()
        {
            playerInput.OnSprintKeyDown  -= OnSprintStart;
            playerInput.OnSprintKeyUp    -= OnSprintCancel;
            playerInput.OnAttackKeyDown  -= OnAttackDown;
            playerInput.OnAttackKeyUp    -= OnAttackUp;
        }
    }
}