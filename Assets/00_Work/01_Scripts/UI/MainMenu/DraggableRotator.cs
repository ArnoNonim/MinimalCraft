using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    public class DraggableRotator : MonoBehaviour
    {
        [Header("회전 설정")]
        public float rotateSpeed = 0.5f;
        public Transform targetTransform;

        [Header("스포트라이트 설정")]
        public Light spotlight;
        public float lightOnIntensity = 1000f;
        public float lightOnDuration  = 0.4f;
        public float lightOffDuration = 0.6f;

        [Header("레이캐스트 설정")]
        public Camera raycastCamera;          // 비우면 Camera.main 사용
        public LayerMask playerLayerMask;     // 플레이어 레이어만 체크

        private bool _isDragging = false;
        private bool _isHovering = false;
        private float _lastMouseX;

        private MotionHandle _lightHandle;

        private void Awake()
        {
            if (targetTransform == null)
                targetTransform = transform;

            if (raycastCamera == null)
                raycastCamera = Camera.main;

            if (spotlight != null)
                spotlight.intensity = 0f;
        }

        private void Update()
        {
            CheckHover();

            if (_isDragging)
            {
                // 마우스 버튼 떼면 드래그 종료
                if (!Mouse.current.leftButton.isPressed)
                {
                    _isDragging = false;
                    if (!_isHovering)
                        AnimateLight(0f, lightOffDuration);
                    return;
                }

                float current = Mouse.current.position.x.ReadValue();
                float delta   = current - _lastMouseX;
                _lastMouseX   = current;

                targetTransform.Rotate(Vector3.up, -delta * rotateSpeed, Space.World);
            }
            else
            {
                // 플레이어 위에서 클릭 시작
                if (_isHovering && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _isDragging = true;
                    _lastMouseX = Mouse.current.position.x.ReadValue();
                }
            }
        }

        private void CheckHover()
        {
            Ray ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hit = Physics.Raycast(ray, out _, Mathf.Infinity, playerLayerMask);

            // 호버 상태 변화 감지
            if (hit && !_isHovering)
            {
                _isHovering = true;
                AnimateLight(lightOnIntensity, lightOnDuration);
            }
            else if (!hit && _isHovering)
            {
                _isHovering = false;
                if (!_isDragging)
                    AnimateLight(0f, lightOffDuration);
            }
        }

        private void AnimateLight(float targetIntensity, float duration)
        {
            if (spotlight == null) return;

            if (_lightHandle.IsActive())
                _lightHandle.Cancel();

            _lightHandle = LMotion
                .Create(spotlight.intensity, targetIntensity, duration)
                .WithEase(Ease.OutCubic)
                .Bind(v => spotlight.intensity = v);
        }
    }
}