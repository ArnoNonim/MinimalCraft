using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    /// <summary>
    /// UI 팝업 베이스 클래스
    /// - CanvasGroup 알파 + 스케일 방식으로 표시/숨김
    /// - 씬에서 항상 활성화 상태로 두어도 됨 (Awake 타이밍 문제 없음)
    /// - 시작 시 자동으로 숨김 처리
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPopup : MonoBehaviour, IPopup
    {
        [Header("팝업 애니메이션")]
        [SerializeField] private float animDuration = 0.18f;
        [SerializeField] private Ease  openEase     = Ease.OutCubic;
        [SerializeField] private Ease  closeEase    = Ease.InCubic;

        [Tooltip("열릴 때 스케일 시작값 (1이면 스케일 없음)")]
        [SerializeField] private float openScaleFrom = 0.92f;

        public bool IsOpen { get; private set; }

        private CanvasGroup   _group;
        private RectTransform _rect;
        private MotionHandle  _alphaHandle;
        private MotionHandle  _scaleHandle;

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _rect  = GetComponent<RectTransform>();

            // 씬 시작 시 즉시 숨김 (애니메이션 없이)
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            CancelHandles();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        public void Toggle()
        {
            if (IsOpen) Close();
            else        Open();
        }

        public virtual void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            _group.interactable   = true;
            _group.blocksRaycasts = true;

            CancelHandles();

            _alphaHandle = LMotion
                .Create(_group.alpha, 1f, animDuration)
                .WithEase(openEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);

            if (_rect != null &&
                !Mathf.Approximately(openScaleFrom, 1f))
            {
                _rect.localScale = Vector3.one * openScaleFrom;

                _scaleHandle = LMotion
                    .Create(openScaleFrom, 1f, animDuration)
                    .WithEase(openEase)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(s => _rect.localScale = Vector3.one * s);
            }

            OnOpen();

            UIManager.Instance?.RefreshCursorAndUI();
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            _group.interactable   = false;
            _group.blocksRaycasts = false;

            CancelHandles();

            _alphaHandle = LMotion
                .Create(_group.alpha, 0f, animDuration)
                .WithEase(closeEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);

            OnClose();

            UIManager.Instance?.RefreshCursorAndUI();
        }

        // ──────────────────────────────────────────────
        // 오버라이드 포인트
        // ──────────────────────────────────────────────

        protected virtual void OnOpen()  { }
        protected virtual void OnClose() { }

        // ──────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────

        private void CancelHandles()
        {
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
        }
    }
}