using _00_Work._01_Scripts.Item.SO;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class ItemTooltip : MonoBehaviour
    {
        public static ItemTooltip Instance;

        [SerializeField] private TMP_Text     nameText;
        [SerializeField] private TMP_Text     descText;
        [SerializeField] private RectTransform panel;

        [Header("페이드 설정")]
        [SerializeField] private float fadeInDuration  = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.08f;

        private Canvas       _canvas;
        private CanvasGroup  _group;
        private MotionHandle _fadeHandle;

        void Awake()
        {
            Instance = this;
            _canvas  = GetComponentInParent<Canvas>();

            _group = GetComponent<CanvasGroup>()
                  ?? gameObject.AddComponent<CanvasGroup>();

            // 시작 시 숨김
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
        }

        public void Show(ItemSO item, Vector2 screenPos)
        {
            if (item == null) { Hide(); return; }

            // Show() 내부에서 텍스트 세팅 후 추가
            nameText.text = item.itemName;
            descText.text = string.IsNullOrEmpty(item.description) ? "" : item.description;

// 레이아웃 강제 갱신 — 텍스트 변경 후 즉시 크기 반영
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            // 위치 설정
            if (_canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform,
                    screenPos, _canvas.worldCamera,
                    out Vector2 localPos);
                panel.anchoredPosition = localPos + new Vector2(10f, -10f);
            }

            // 페이드인
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _group.blocksRaycasts = false;
            _group.interactable   = false;

            _fadeHandle = LMotion
                .Create(_group.alpha, 1f, fadeInDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);
        }

        public void Hide()
        {
            if (_group == null || _group.alpha <= 0f) return;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();

            _fadeHandle = LMotion
                .Create(_group.alpha, 0f, fadeOutDuration)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);
        }

        void Update()
        {
            if (_group.alpha <= 0f) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (_canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform,
                    mousePos, _canvas.worldCamera,
                    out Vector2 localPos);
                panel.anchoredPosition = localPos + new Vector2(15f, -15f);
            }
        }
    }
}