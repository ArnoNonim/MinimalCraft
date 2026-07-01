using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    /// <summary>
    /// 채널링 진행 UI.
    /// UIPopup 상속 — Open/Close 애니메이션 자동 처리.
    /// </summary>
    public class UIChannelBar : UIPopup
    {
        public static UIChannelBar Instance { get; private set; }

        [Header("채널바 참조")]
        [SerializeField] private Image    fillImage;
        [SerializeField] private TMP_Text labelText;

        private MotionHandle _fillHandle;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_fillHandle.IsActive()) _fillHandle.Cancel();
            if (Instance == this) Instance = null;
        }

        // ──────────────────────────────────────────────

        /// <summary>채널링 시작 — 바를 열고 fill 애니메이션 시작</summary>
        public void StartChannel(string label, float duration)
        {
            if (fillImage == null) return;

            fillImage.fillAmount = 0f;
            if (labelText != null) labelText.text = label;

            Open();

            if (_fillHandle.IsActive()) _fillHandle.Cancel();

            _fillHandle = LMotion
                .Create(0f, 1f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToFillAmount(fillImage);
        }

        /// <summary>채널링 취소 — 바 닫기</summary>
        public void CancelChannel()
        {
            if (_fillHandle.IsActive()) _fillHandle.Cancel();
            fillImage.fillAmount = 0f;
            Close();
        }
    }
}