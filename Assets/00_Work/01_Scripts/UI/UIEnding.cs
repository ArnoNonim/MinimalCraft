using System.Collections;
using _00_Work._01_Scripts.Sound;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI
{
    /// <summary>
    /// 엔딩 UI.
    /// 1. 화면 페이드 인
    /// 2. 텍스트가 아래에서 위로 서서히 올라옴
    /// 3. 다 올라오면 아무 키나 누르면 메인메뉴로
    /// </summary>
    public class UIEnding : MonoBehaviour
    {
        public static UIEnding Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private CanvasGroup   canvasGroup;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private TMP_Text      endingText;
        [SerializeField] private TMP_Text      pressAnyKey;

        [Header("페이드 설정")]
        [SerializeField] private float fadeInDuration = 1.5f;

        [Header("스크롤 설정")]
        [SerializeField] private float scrollDuration = 18f;
        [SerializeField] private float textDelay      = 1f;

        [Header("엔딩 텍스트")]
        [TextArea(6, 20)]
        [SerializeField] private string endingMessage =
            "당신은 심층부의 진실을 발견했습니다.\n\n" +
            "이 유물은 오래전 사라진 문명이 남긴 마지막 증거.\n\n" +
            "그들은 이 행성의 핵에 모든 것을 기록했다.\n\n" +
            "그리고 당신은, 그 끝에 서 있습니다.";

        [Header("메인메뉴")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        private bool         _anyKeyEnabled;
        private MotionHandle _fadeHandle;
        private MotionHandle _scrollHandle;

        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;

            if (pressAnyKey != null) pressAnyKey.alpha = 0f;
        }

        private void Update()
        {
            if (!_anyKeyEnabled) return;
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
            }
        }

        // ──────────────────────────────────────────────

        public void Show()
        {
            SoundManager.Instance?.FadeOutBGM();
            gameObject.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(EndingRoutine());

            if (UIManager.Instance != null)
                UIManager.Instance.PlayerInput.IsInputBlocked = true;
        }

        // ──────────────────────────────────────────────

        private IEnumerator EndingRoutine()
        {
            canvasGroup.interactable   = true;
            canvasGroup.blocksRaycasts = true;

            // 텍스트 세팅
            if (endingText != null)
                endingText.text = endingMessage;

            // 레이아웃 강제 갱신 후 한 프레임 대기 — 높이 계산 완전 반영
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            yield return null;

            // Canvas 기준 화면 높이로 시작/끝 Y 계산
            var canvasRect   = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float screenHeight  = canvasRect.rect.height;
            float contentHeight = scrollContent.rect.height;

            float startY = -(screenHeight * 0.5f + contentHeight);  // 화면 완전 아래
            float endY   =   screenHeight * 0.5f + contentHeight;   // 화면 완전 위

            // 시작 위치 초기화
            var initPos = scrollContent.anchoredPosition;
            initPos.y   = startY;
            scrollContent.anchoredPosition = initPos;

            // 1. 페이드 인
            _fadeHandle = LMotion
                .Create(0f, 1f, fadeInDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(canvasGroup);

            yield return new WaitForSecondsRealtime(fadeInDuration + textDelay);

            // 2. 텍스트 위로 스크롤
            _scrollHandle = LMotion
                .Create(startY, endY, scrollDuration)
                .WithEase(Ease.Linear)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(y =>
                {
                    var p = scrollContent.anchoredPosition;
                    p.y   = y;
                    scrollContent.anchoredPosition = p;
                });

            yield return new WaitForSecondsRealtime(scrollDuration);

            // 3. 아무 키나 텍스트 페이드인
            if (pressAnyKey != null)
            {
                LMotion.Create(0f, 1f, 1f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => pressAnyKey.alpha = a);
            }

            yield return new WaitForSecondsRealtime(1f);
            _anyKeyEnabled = true;
        }

        private void OnDestroy()
        {
            if (_fadeHandle.IsActive())   _fadeHandle.Cancel();
            if (_scrollHandle.IsActive()) _scrollHandle.Cancel();
        }
    }
}
