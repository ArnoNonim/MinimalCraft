using System.Collections;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    /// <summary>
    /// 깊이 HUD
    /// 플레이어 y좌표 기반으로 균열 깊이 표시
    /// 부하 단계에 따라 색상/아이콘 변화
    /// </summary>
    public class UIDepthIndicator : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Transform playerTransform;

        [Header("UI")]
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text hazardText;
        [SerializeField] private Image    hazardIcon;
        [SerializeField] private Image    depthBarFill;

        [Header("단계별 아이콘 스프라이트")]
        [SerializeField] private Sprite spriteSafe;
        [SerializeField] private Sprite spriteHazard1;
        [SerializeField] private Sprite spriteHazard2;
        [SerializeField] private Sprite spriteHazard3;

        [Header("깊이 단계 설정")]
        [SerializeField] private float surfaceY  =  55f;
        [SerializeField] private float hazard1Y  =  35f;  // 지표면 -20m
        [SerializeField] private float hazard2Y  =  -5f;  // 지표면 -60m
        [SerializeField] private float hazard3Y  = -45f;  // 지표면 -100m
        [SerializeField] private float maxDepthY = -113f; // 지표면 -168m

        [Header("단계별 색상")]
        [SerializeField] private Color colorSafe    = new Color(0.4f, 1.0f, 0.4f);
        [SerializeField] private Color colorHazard1 = new Color(1.0f, 0.9f, 0.2f);
        [SerializeField] private Color colorHazard2 = new Color(1.0f, 0.5f, 0.1f);
        [SerializeField] private Color colorHazard3 = new Color(1.0f, 0.1f, 0.1f);

        [Header("단계별 텍스트")]
        [SerializeField] private string textSafe    = "";
        [SerializeField] private string textHazard1 = "▲ 상승 부하 1단계";
        [SerializeField] private string textHazard2 = "▲▲ 상승 부하 2단계";
        [SerializeField] private string textHazard3 = "▲▲▲ 귀환 불가 구역";

        [Header("갱신 설정")]
        [SerializeField] private float updateInterval = 0.1f;

        public enum HazardLevel { Safe, Hazard1, Hazard2, Hazard3 }
        public HazardLevel CurrentHazard { get; private set; }

        private float        _timer;
        private int          _lastHazard = -1;
        private Color        _currentColor;
        private MotionHandle _colorHandle;
        private MotionHandle _iconHandle;

        // ──────────────────────────────────────────────

        void Awake()
        {
            _currentColor = colorSafe;
            if (hazardText != null) hazardText.text = textSafe;
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
            Refresh();
        }

        // ──────────────────────────────────────────────

        void Refresh()
        {
            if (playerTransform == null) return;

            float y     = playerTransform.position.y;
            float depth = Mathf.Max(0f, surfaceY - y);

            if (depthText != null)
                depthText.text = y >= surfaceY
                    ? "지표면"
                    : $"깊이  {depth:F0}m";

            if (depthBarFill != null)
                depthBarFill.fillAmount =
                    Mathf.Clamp01(Mathf.InverseLerp(surfaceY, maxDepthY, y));

            HazardLevel hazard = GetHazardLevel(y);
            CurrentHazard = hazard;

            if ((int)hazard == _lastHazard) return;
            _lastHazard = (int)hazard;

            UpdateHazardUI(hazard);
        }

        HazardLevel GetHazardLevel(float y)
        {
            if (y <= hazard3Y) return HazardLevel.Hazard3;
            if (y <= hazard2Y) return HazardLevel.Hazard2;
            if (y <= hazard1Y) return HazardLevel.Hazard1;
            return HazardLevel.Safe;
        }

        void UpdateHazardUI(HazardLevel hazard)
        {
            Color targetColor = hazard switch
            {
                HazardLevel.Hazard1 => colorHazard1,
                HazardLevel.Hazard2 => colorHazard2,
                HazardLevel.Hazard3 => colorHazard3,
                _                   => colorSafe
            };

            string targetText = hazard switch
            {
                HazardLevel.Hazard1 => textHazard1,
                HazardLevel.Hazard2 => textHazard2,
                HazardLevel.Hazard3 => textHazard3,
                _                   => textSafe
            };

            Sprite targetSprite = hazard switch
            {
                HazardLevel.Hazard1 => spriteHazard1,
                HazardLevel.Hazard2 => spriteHazard2,
                HazardLevel.Hazard3 => spriteHazard3,
                _                   => spriteSafe
            };

            if (hazardText != null)
                hazardText.text = targetText;

            // 색상 전환 (텍스트 + 바)
            if (_colorHandle.IsActive()) _colorHandle.Cancel();

            _colorHandle = LMotion.Create(_currentColor, targetColor, 0.5f)
                .WithEase(Ease.OutCubic)
                .Bind(c =>
                {
                    _currentColor = c;
                    if (depthText    != null) depthText.color    = c;
                    if (hazardText   != null) hazardText.color   = c;
                    if (depthBarFill != null) depthBarFill.color = c;
                });

            // 아이콘 페이드 아웃 → 교체 → 페이드 인
            if (hazardIcon != null)
            {
                if (_iconHandle.IsActive()) _iconHandle.Cancel();

                _iconHandle = LMotion.Create(hazardIcon.color.a, 0f, 0.2f)
                    .WithEase(Ease.OutCubic)
                    .Bind(a =>
                    {
                        var c = hazardIcon.color;
                        hazardIcon.color = new Color(c.r, c.g, c.b, a);
                    });

                StartCoroutine(SwapAndFadeIn(targetSprite, targetColor, hazard));
            }
        }

        IEnumerator SwapAndFadeIn(Sprite sprite, Color color, HazardLevel hazard)
        {
            yield return new WaitForSecondsRealtime(0.22f);

            if (hazardIcon == null) yield break;

            // 스프라이트 + 색상 교체
            if (sprite != null) hazardIcon.sprite = sprite;
            hazardIcon.color = new Color(color.r, color.g, color.b, 0f);

            // 페이드 인
            if (_iconHandle.IsActive()) _iconHandle.Cancel();

            _iconHandle = LMotion.Create(0f, 1f, 0.25f)
                .WithEase(Ease.InCubic)
                .Bind(a =>
                {
                    if (hazardIcon == null) return;
                    var c = hazardIcon.color;
                    hazardIcon.color = new Color(c.r, c.g, c.b, a);
                });

            // 3단계면 페이드 인 완료 후 깜빡임
            if (hazard == HazardLevel.Hazard3)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                if (_iconHandle.IsActive()) _iconHandle.Cancel();
                _iconHandle = LMotion.Create(1f, 0.2f, 0.6f)
                    .WithEase(Ease.InOutSine)
                    .WithLoops(-1, LoopType.Yoyo)
                    .BindToColorA(hazardIcon);
            }
        }
    }
}