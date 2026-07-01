using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 세이브 삭제 확인 팝업.
    /// UIDeleteSave가 Open() 호출 → 예/아니오 선택.
    /// </summary>
    public class UIAreYouSure : UIPopup
    {
        [SerializeField] private Button yesBtn;
        [SerializeField] private Button noBtn;

        // 예 선택 시 호출할 콜백 (UIDeleteSave가 주입)
        private System.Action _onConfirm;

        protected override void Awake()
        {
            base.Awake();
            yesBtn?.onClick.AddListener(OnYes);
            noBtn?.onClick.AddListener(OnNo);
        }

        private void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }

        public void OpenWithCallback(System.Action onConfirm)
        {
            _onConfirm = onConfirm;
            Open();
        }

        private void OnYes()
        {
            Close();
            _onConfirm?.Invoke();
        }

        private void OnNo()
        {
            Close();
        }
    }
}