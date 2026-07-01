using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIInteractPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private CanvasGroup  _group;
        private MotionHandle _handle;
        private bool         _visible;

        [SerializeField] private float fadeIn  = 0.15f;
        [SerializeField] private float fadeOut = 0.1f;

        private void Awake()
        {
            _group       = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void Show(string message)
        {
            text.text = message;   // ← 텍스트는 항상 갱신
            if (_visible) return;
            _visible = true;
            Fade(1f, fadeIn);
        }

        public void Hide()
        {
            if (!_visible) return; // ← 이미 숨겨져있으면 스킵
            _visible = false;
            Fade(0f, fadeOut);
        }

        private void Fade(float target, float duration)
        {
            if (_handle.IsActive()) _handle.Cancel();
            _handle = LMotion
                .Create(_group.alpha, target, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_group);
        }

        private void OnDestroy()
        {
            if (_handle.IsActive()) _handle.Cancel();
        }
    }
}