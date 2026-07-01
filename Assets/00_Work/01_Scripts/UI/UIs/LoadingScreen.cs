using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    /// <summary>
    /// 초기 로딩 화면 — UIPopup 구조와 동일한 CanvasGroup 방식
    /// SetActive 없이 alpha로만 표시/숨김 처리
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private Image progressFill;
        [SerializeField] private LoadingTips loadingTips;

        [Header("페이드 설정")]
        [SerializeField] private float fadeInDuration  = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        [Header("진척도 설정")]
        [SerializeField] private float progressSmooth = 0.2f;

        private CanvasGroup  _group;
        private MotionHandle _fadeHandle;
        private MotionHandle _progressHandle;

        // ──────────────────────────────────────────────
        // Unity 이벤트
        // ──────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _group = GetComponent<CanvasGroup>();

            // 시작 시 즉시 표시 (로딩 중이므로)
            _group.alpha          = 1f;
            _group.interactable   = true;
            _group.blocksRaycasts = true;

            if (progressFill != null)
                progressFill.fillAmount = 0f;
            
            loadingTips?.StartTips();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>로딩 진척도 설정 (0 ~ 1)</summary>
        public void SetProgress(float value)
        {
            if (progressFill == null) return;

            value = Mathf.Clamp01(value);

            if (_progressHandle.IsActive()) _progressHandle.Cancel();

            _progressHandle = LMotion
                .Create(progressFill.fillAmount, value, progressSmooth)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToFillAmount(progressFill);
        }

        /// <summary>로딩 완료 — 진척도 1 채운 뒤 페이드아웃</summary>
        public void Hide()
        {
            SetProgress(1f);

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();

            _group.interactable   = false;
            _group.blocksRaycasts = false;

            _fadeHandle = LMotion
                .Create(_group.alpha, 0f, fadeOutDuration)
                .WithDelay(progressSmooth + 0.1f)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);
        }

        /// <summary>로딩 화면 다시 표시 (재시작 등)</summary>
        public void Show()
        {
            if (progressFill != null)
                progressFill.fillAmount = 0f;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();

            _group.interactable   = true;
            _group.blocksRaycasts = true;

            _fadeHandle = LMotion
                .Create(_group.alpha, 1f, fadeInDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);
        }
    }
}