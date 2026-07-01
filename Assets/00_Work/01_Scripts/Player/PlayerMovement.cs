using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Player.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("참조")]
        public PlayerInputSO playerInput;
        public PlayerStatSO  stat;
        public PlayerETCStatSO  etcStat;
        public Transform     cameraTransform;
        public Transform playerBody;
        public ChunkManager chunkManager;

        [Header("Physics Material")]
        [SerializeField] private PhysicsMaterial groundMaterial;
        [SerializeField] private PhysicsMaterial airMaterial;
        
        [Header("지면 감지")]
        public float     groundCheckRadius   = 0.3f;  // 캡슐 반지름에 맞게
        public float     groundCheckDistance = 0.1f;
        public LayerMask groundLayer;

        [Header("낙하 가속도")]
        public float fallMultiplier     = 2.5f; // 떨어질 때 중력 배수
        public float lowJumpMultiplier  = 2f;   // 점프 짧게 눌렀을 때
        
        [Header("물 속 설정")]
        public float swimSpeed      = 3f;
        public float swimUpSpeed    = 2f;
        
        [Header("웅크리기")]
        [SerializeField] private float crouchHeight         = 1.0f;
        [SerializeField] private float crouchCenterY        = 0.5f;
        [SerializeField] private float crouchSpeedMul       = 0.5f;
        [SerializeField] private float crouchCameraY        = 0.6f;
        [SerializeField] private float crouchTransitionSpeed = 8f;
        [SerializeField] private Transform cameraHolder;

        public Vector3 MoveDir { get; private set; }
        public float VerticalVelocity => _rb.linearVelocity.y;

        private Rigidbody _rb;
        private Vector2   _moveInput;
        private bool      _isSprinting;
        private bool      _isGrounded;
        private bool      _isJumping;
        
        private float _standHeight;
        private float _standCenterY;
        private float _standCameraY;
        private bool  _isCrouching;
        private bool  _crouchHeld;
        public bool IsCrouching => _isCrouching;
        
        private CapsuleCollider _capsuleCollider;
        
        private bool _isInWater;
        public bool IsInWater => _isInWater;
        public bool IsGrounded  => _isGrounded;
        public bool IsSprinting => _isSprinting;
        public bool IsJumping   => _isJumping;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _rb.interpolation  = RigidbodyInterpolation.Interpolate;
            _capsuleCollider   = GetComponent<CapsuleCollider>();
            
            _standHeight  = _capsuleCollider.height;
            _standCenterY = _capsuleCollider.center.y;
            _standCameraY = cameraHolder != null ? cameraHolder.localPosition.y : 1.6f;
        }

        void OnEnable()
        {
            playerInput.OnMovement       += OnMove;
            playerInput.OnJumpKeyPressed += OnJump;
            playerInput.OnSprintKeyDown  += OnSprintStart;
            playerInput.OnSprintKeyUp    += OnSprintCancel;
            playerInput.OnCrouchKeyDown  += OnCrouchDown;
            playerInput.OnCrouchKeyUp    += OnCrouchUp;
        }

        void OnDisable()
        {
            playerInput.OnMovement       -= OnMove;
            playerInput.OnJumpKeyPressed -= OnJump;
            playerInput.OnSprintKeyDown  -= OnSprintStart;
            playerInput.OnSprintKeyUp    -= OnSprintCancel;
            playerInput.OnCrouchKeyDown  -= OnCrouchDown;
            playerInput.OnCrouchKeyUp    -= OnCrouchUp;
        }

        void Update()
        {
            CheckGround();
            CheckWater();
            ControlDrag();
            UpdateCrouch();
        }
        void FixedUpdate()
        {
            MovePlayer();
            ApplyFallGravity();
        }
        
        void OnCrouchDown() => _crouchHeld = true;
        void OnCrouchUp()   => _crouchHeld = false;

        void UpdateCrouch()
        {
            bool wantCrouch = _crouchHeld && _isGrounded;

            if (wantCrouch != _isCrouching)
            {
                if (wantCrouch)
                {
                    _isCrouching            = true;
                    _capsuleCollider.height = crouchHeight;
                    _capsuleCollider.center = new Vector3(0f, crouchCenterY, 0f);
                }
                else if (CanStandUp())
                {
                    _isCrouching            = false;
                    _capsuleCollider.height = _standHeight;
                    _capsuleCollider.center = new Vector3(0f, _standCenterY, 0f);
                }
            }

            // 카메라 부드럽게 전환
            if (cameraHolder != null)
            {
                float targetY = _isCrouching ? crouchCameraY : _standCameraY;
                var lp = cameraHolder.localPosition;
                lp.y = Mathf.Lerp(lp.y, targetY, crouchTransitionSpeed * Time.deltaTime);
                cameraHolder.localPosition = lp;
            }
        }
        
        bool CanStandUp()
        {
            Vector3 origin = transform.position + Vector3.up * crouchHeight;
            return !Physics.SphereCast(origin, groundCheckRadius, Vector3.up,
                out _, _standHeight - crouchHeight, groundLayer);
        }
        
        void CheckWater()
        {
            if (chunkManager == null) return;

            // 플레이어 발 위치와 가슴 위치 둘 다 체크
            Vector3 feetPos  = transform.position + Vector3.up * 0.3f;
            Vector3 chestPos = transform.position + Vector3.up * 1.0f;

            byte feetBlock  = chunkManager.GetBlockAt(feetPos);
            byte chestBlock = chunkManager.GetBlockAt(chestPos);

            _isInWater = feetBlock  == (byte)BlockType.Water ||
                         chestBlock == (byte)BlockType.Water;
        }

        void MovePlayer()
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right   = cameraTransform.right;
            forward.y = 0f;
            right.y   = 0f;

            if (forward.magnitude > 0.001f) forward.Normalize();
            else forward = playerBody.forward;
            if (right.magnitude > 0.001f) right.Normalize();
            else right = playerBody.right;

            MoveDir = forward * _moveInput.y + right * _moveInput.x;

            if (_isInWater)
            {
                // 물 속 이동
                _rb.linearDamping = 3f;
                _rb.AddForce(MoveDir * swimSpeed * 10f, ForceMode.Force);

                // 스페이스바로 수영
                if (_isJumping)
                    _rb.AddForce(Vector3.up * swimUpSpeed * 10f, ForceMode.Force);

                // 가라앉지 않게 중력 감소
                _rb.AddForce(Vector3.up * Physics.gravity.magnitude * 0.8f,
                    ForceMode.Acceleration);
            }
            else
            {
                float baseSpeed = _isSprinting ? stat.sprintSpeed : stat.walkSpeed;
                float speed     = _isCrouching ? baseSpeed * crouchSpeedMul : baseSpeed;

                if (_isGrounded)
                    _rb.AddForce(MoveDir * speed * 10f, ForceMode.Force);
                else
                    _rb.AddForce(MoveDir * speed * 4f,  ForceMode.Force);
            }

            LimitSpeed();
        }

        void ApplyFallGravity()
        {
            if (_rb.linearVelocity.y < 0f)
            {
                // 떨어질 때 중력 추가
                _rb.AddForce(Vector3.down * Physics.gravity.magnitude 
                    * (fallMultiplier - 1f), ForceMode.Acceleration);
            }
            else if (_rb.linearVelocity.y > 0f && !_isJumping)
            {
                // 점프 버튼 일찍 떼면 빠르게 내려옴
                _rb.AddForce(Vector3.down * Physics.gravity.magnitude
                    * (lowJumpMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        void LimitSpeed()
        {
            Vector3 flatVel = new Vector3(
                _rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            float maxSpeed = _isSprinting ? stat.sprintSpeed : stat.walkSpeed;

            if (flatVel.magnitude > maxSpeed)
            {
                Vector3 limited = flatVel.normalized * maxSpeed;
                _rb.linearVelocity = new Vector3(
                    limited.x, _rb.linearVelocity.y, limited.z);
            }
        }

        void CheckGround()
        {
            // 레이 대신 SphereCast — 캡슐 바닥 전체를 감지
            Vector3 origin = transform.position + Vector3.down * 0.1f;
            _isGrounded = Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out _,
                groundCheckDistance,
                groundLayer);

            if (_isGrounded) _isJumping = false;
        }

        void ControlDrag()
        {
            bool onGround = _isGrounded && !_isJumping;
    
            _rb.linearDamping = onGround ? etcStat.groundDrag : etcStat.airDrag;

            if (_capsuleCollider != null)
            {
                _capsuleCollider.material = onGround ? groundMaterial : airMaterial;
            }
        }

        void OnMove(Vector2 input)  => _moveInput   = input;

        void OnJump()
        {
            if (!_isGrounded) return;
            _isJumping = true;
            _rb.linearVelocity = new Vector3(
                _rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * stat.jumpForce, ForceMode.Impulse);
        }

        void OnSprintStart()  => _isSprinting = true;
        void OnSprintCancel() => _isSprinting = false;

        // 씬 뷰에서 지면 감지 범위 시각화
        void OnDrawGizmos()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(
                transform.position + Vector3.down * (0.1f + groundCheckDistance),
                groundCheckRadius);
        }
    }
}