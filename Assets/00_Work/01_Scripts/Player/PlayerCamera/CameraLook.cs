using _00_Work._01_Scripts.Player.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Player.PlayerCamera
{
    public class CameraLook : MonoBehaviour
    {
        [Header("3인칭")]
        public bool isThirdPerson;
        
        [Header("참조")]
        public PlayerInputSO   playerInput;
        public PlayerETCStatSO stat;
        public Transform       playerBody;
        public Transform       headPivot;
        public PlayerMovement  playerMovement;
        
        public Transform firstPersonCameraTransform;
        public Transform thirdPersonCameraTransform;
        
        [Header("각도 제한")]
        public float minXAngle = -90f;
        public float maxXAngle =  90f;

        [Header("머리 회전 제한")]
        public float headMaxYAngle = 60f;
        public float headMaxXAngle = 60f;
        public float bodyMaxYAngle = 45f;
        public float bodyRotSpeed  = 8f;

        private Transform _cameraHolder;
        
        public void SetDeadState(bool isDead) => _isDead = isDead;
        private bool _isDead;

        private float _xRotation;
        private float _targetXRotation;
        private float _targetYRotation;
        private float _currentYRotation;
        private float _bodyYRotation;

        private float   _bobbingTimer;
        private Vector3 _initialCameraPos;
        private Vector2 _moveInput;
        private bool    _isSprinting;

        private float _currentLeanAngle;
        
        private MeshRenderer _meshRenderer;
        private MeshRenderer _hatMeshRenderer;

        // ── 스무스 헬퍼 ─────────────────────────────────────────────
        float SmoothLerp(float current, float target, float speed)
            => stat.useSmoothSpeed
                ? Mathf.Lerp(current, target, Time.deltaTime * speed)
                : target;

        Quaternion SmoothSlerp(Quaternion current, Quaternion target, float speed)
            => stat.useSmoothSpeed
                ? Quaternion.Slerp(current, target, Time.deltaTime * speed)
                : target;

        // ──────────────────────────────────────────────

        private void Awake()
        {
            _cameraHolder    = gameObject.transform;
            _meshRenderer    = headPivot.GetComponentInChildren<MeshRenderer>();
            _hatMeshRenderer = headPivot.Find("Head/Hat")?.GetComponent<MeshRenderer>();
        }
        
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            if (_cameraHolder != null)
                _initialCameraPos = _cameraHolder.localPosition;

            _bodyYRotation = playerBody.eulerAngles.y;

            if (isThirdPerson) SwitchToThirdPerson();
            else               SwitchToFirstPerson();
        }

        void OnEnable()
        {
            playerInput.OnLookAction    += OnLook;
            playerInput.OnMovement      += OnMove;
            playerInput.OnSprintKeyDown += OnSprintStart;
            playerInput.OnSprintKeyUp   += OnSprintCancel;
        }

        void OnDisable()
        {
            playerInput.OnLookAction    -= OnLook;
            playerInput.OnMovement      -= OnMove;
            playerInput.OnSprintKeyDown -= OnSprintStart;
            playerInput.OnSprintKeyUp   -= OnSprintCancel;
        }

        void LateUpdate()
        {
            if (isThirdPerson) ApplyThirdPersonRotation();
            else               ApplySmoothedRotation();

            ApplyHeadRotation();
            ApplyBobbing();
        }
        
        void ApplyThirdPersonRotation()
        {
            if (_isDead) return;
            
            if (!playerInput.IsInputBlocked)
            {
                _xRotation        = SmoothLerp(_xRotation,        _targetXRotation, stat.smoothSpeed);
                _currentYRotation = SmoothLerp(_currentYRotation, _targetYRotation, stat.smoothSpeed);
            }

            transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            _cameraHolder.rotation  = Quaternion.Euler(0f, _currentYRotation, 0f);

            if (playerMovement.MoveDir.magnitude > 0.1f)
            {
                float moveAngle = Mathf.Atan2(
                    playerMovement.MoveDir.x,
                    playerMovement.MoveDir.z) * Mathf.Rad2Deg;

                float delta = Mathf.DeltaAngle(_bodyYRotation, moveAngle);
                if (Mathf.Abs(delta) < 0.1f) _bodyYRotation  = moveAngle;
                else                          _bodyYRotation += delta * Time.deltaTime * bodyRotSpeed;
            }

            playerBody.rotation = Quaternion.Euler(0f, _bodyYRotation, 0f);
        }

        void OnLook(Vector2 input)
        {
            if (playerInput.IsInputBlocked)
            {
                _targetXRotation = _xRotation;
                _targetYRotation = _currentYRotation;
                return;
            }

            _targetXRotation -= input.y * stat.sensitivity;
            _targetXRotation  = Mathf.Clamp(_targetXRotation, minXAngle, maxXAngle);
            _targetYRotation += input.x * stat.sensitivity;
        }

        void OnMove(Vector2 input)  => _moveInput   = input;
        void OnSprintStart()        => _isSprinting = true;
        void OnSprintCancel()       => _isSprinting = false;

        void ApplySmoothedRotation()
        {
            if (_isDead) return;
            
            _xRotation        = SmoothLerp(_xRotation,        _targetXRotation, stat.smoothSpeed);
            _currentYRotation = SmoothLerp(_currentYRotation, _targetYRotation, stat.smoothSpeed);

            if (playerMovement.MoveDir.magnitude > 0.1f)
            {
                float moveAngle = Mathf.Atan2(
                    playerMovement.MoveDir.x,
                    playerMovement.MoveDir.z) * Mathf.Rad2Deg;

                float relativeMoveAngle = Mathf.DeltaAngle(_currentYRotation, moveAngle);
                float targetBodyOffset  = Mathf.Clamp(relativeMoveAngle, -bodyMaxYAngle, bodyMaxYAngle);
                float targetBodyY       = _currentYRotation + targetBodyOffset;
                float delta             = Mathf.DeltaAngle(_bodyYRotation, targetBodyY);

                if (Mathf.Abs(delta) < 0.1f) _bodyYRotation  = targetBodyY;
                else                          _bodyYRotation += delta * Time.deltaTime * bodyRotSpeed;
            }
            else
            {
                float deltaY = Mathf.DeltaAngle(_bodyYRotation, _currentYRotation);
                if (Mathf.Abs(deltaY) > headMaxYAngle)
                {
                    float excess = deltaY - Mathf.Sign(deltaY) * headMaxYAngle;
                    if (Mathf.Abs(excess) < 0.1f) _bodyYRotation += excess;
                    else                           _bodyYRotation += excess * Time.deltaTime * bodyRotSpeed;
                }
            }

            playerBody.rotation = Quaternion.Euler(0f, _bodyYRotation, 0f);

            float relativeY = Mathf.DeltaAngle(_bodyYRotation, _currentYRotation);
            if (firstPersonCameraTransform != null)
                firstPersonCameraTransform.localRotation =
                    Quaternion.Euler(_xRotation, relativeY, _currentLeanAngle);
        }

        void ApplyHeadRotation()
        {
            if (headPivot == null) return;

            if (isThirdPerson)
            {
                if (thirdPersonCameraTransform == null) return;

                Quaternion worldCameraRot = thirdPersonCameraTransform.rotation;
                Quaternion bodyInverse    = Quaternion.Inverse(Quaternion.Euler(0f, _bodyYRotation, 0f));
                Quaternion localHeadRot   = bodyInverse * worldCameraRot;
                Vector3    localEuler     = localHeadRot.eulerAngles;

                float localX = Mathf.Clamp(Mathf.DeltaAngle(0f, localEuler.x), -headMaxXAngle, headMaxXAngle);
                float localY = Mathf.Clamp(Mathf.DeltaAngle(0f, localEuler.y), -headMaxYAngle, headMaxYAngle);

                headPivot.localRotation = SmoothSlerp(
                    headPivot.localRotation,
                    Quaternion.Euler(localX, localY, 0f),
                    stat.smoothSpeed * 2f);
                return;
            }

            float fpLocalX = Mathf.Clamp(_xRotation, -headMaxXAngle, headMaxXAngle);
            float fpLocalY = Mathf.Clamp(
                Mathf.DeltaAngle(_bodyYRotation, _currentYRotation),
                -headMaxYAngle, headMaxYAngle);

            headPivot.localRotation = SmoothSlerp(
                headPivot.localRotation,
                Quaternion.Euler(fpLocalX, fpLocalY, 0f),
                stat.smoothSpeed * 2f);
        }
        
        public void SwitchToFirstPerson()
        {
            isThirdPerson = false;

            float bodyY = playerBody.eulerAngles.y;
            _bodyYRotation    = bodyY;
            _currentYRotation = bodyY;
            _targetYRotation  = bodyY;
            _xRotation        = 0f;
            _targetXRotation  = 0f;
            _currentLeanAngle = 0f;

            _cameraHolder.rotation = Quaternion.Euler(0f, bodyY, 0f);

            if (firstPersonCameraTransform != null)
                firstPersonCameraTransform.rotation = Quaternion.Euler(0f, bodyY, 0f);

            headPivot.localRotation = Quaternion.identity;

            if (_meshRenderer    != null) _meshRenderer.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            if (_hatMeshRenderer != null) _hatMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        public void SwitchToThirdPerson()
        {
            isThirdPerson = true;

            _bodyYRotation    = playerBody.eulerAngles.y;
            _currentYRotation = _bodyYRotation;
            _targetYRotation  = _bodyYRotation;

            if (_meshRenderer    != null) _meshRenderer.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.On;
            if (_hatMeshRenderer != null) _hatMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        void ApplyBobbing()
        {
            if (_cameraHolder == null || _isDead) return;
            
            bool isMoving = _moveInput.magnitude > 0.1f;

            if (isMoving)
            {
                float speed   = _isSprinting ? stat.sprintBobbingSpeed   : stat.walkBobbingSpeed;
                float amountX = _isSprinting ? stat.sprintBobbingAmountX : stat.walkBobbingAmountX;
                float amountY = _isSprinting ? stat.sprintBobbingAmountY : stat.walkBobbingAmountY;

                _bobbingTimer += Time.deltaTime * speed;

                float newX = Mathf.Sin(_bobbingTimer) * amountX;
                float newY = Mathf.Abs(Mathf.Sin(_bobbingTimer)) * amountY;

                _cameraHolder.localPosition = Vector3.Lerp(
                    _cameraHolder.localPosition,
                    _initialCameraPos + new Vector3(newX, newY, 0f),
                    Time.deltaTime * speed);
            }
            else
            {
                _bobbingTimer = 0f;
                _cameraHolder.localPosition = Vector3.Lerp(
                    _cameraHolder.localPosition,
                    _initialCameraPos,
                    Time.deltaTime * stat.bobbingReturnSpeed);
            }
        }
    }
}