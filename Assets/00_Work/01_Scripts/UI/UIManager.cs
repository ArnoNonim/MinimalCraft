using System.Collections.Generic;
using System.Linq;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI.RecipeBook;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        [field: SerializeField]
        public PlayerInputSO PlayerInput { get; set; }

        [Header("모드")]
        [Tooltip("메인메뉴 씬이면 체크 — 커서 항상 활성화, ESCMenu 없이 Settings만 작동")]
        public bool isMainMenu;

        [Header("공통 UI")]
        [SerializeField] private GameObject crosshair;

        [Header("팝업 목록")]
        [SerializeField] private UIInventory uiInventory;
        [SerializeField] private UIEscMenu   uiEscMenu;
        [SerializeField] private UISettings  uiSettings;
        [SerializeField] private UIRecipeBook uiRecipeBook;

        private readonly List<UIPopup> _allPopups = new();

        public UIPopup CurPopup
        {
            get
            {
                for (int i = _allPopups.Count - 1; i >= 0; i--)
                {
                    var popup = _allPopups[i];
                    if (popup != null && popup.IsOpen)
                        return popup;
                }
                return null;
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            TryRegister(uiInventory);
            TryRegister(uiEscMenu);
            TryRegister(uiSettings);
            TryRegister(uiRecipeBook);

            if (PlayerInput != null)
                PlayerInput.IsInputBlocked = false;
        }

        private void Start()
        {
            RefreshCursorAndUI();
        }

        private void OnEnable()
        {
            if (PlayerInput == null) return;
            PlayerInput.OnTryOpenInventory += TryOpenInventory;
            PlayerInput.OnTryOpenESCMenu   += TryOpenEscMenu;
            PlayerInput.OnTryOpenRecipeBook += TryOpenRecipeBook;
        }

        private void OnDisable()
        {
            if (PlayerInput == null) return;
            PlayerInput.OnTryOpenInventory -= TryOpenInventory;
            PlayerInput.OnTryOpenESCMenu   -= TryOpenEscMenu;
            PlayerInput.OnTryOpenRecipeBook -= TryOpenRecipeBook;
        }

        public void RegisterPopup(UIPopup popup)
        {
            if (popup == null || _allPopups.Contains(popup)) return;
            _allPopups.Add(popup);
        }

        public void UnregisterPopup(UIPopup popup)
        {
            _allPopups.Remove(popup);
        }

        public void CloseAllPopups()
        {
            for (int i = 0; i < _allPopups.Count; i++)
            {
                var popup = _allPopups[i];
                if (popup != null && popup.IsOpen)
                    popup.Close();
            }
        }

        public void RefreshCursorAndUI()
        {
            // 메인메뉴는 커서 항상 활성화
            if (isMainMenu)
            {
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
                if (PlayerInput != null) PlayerInput.IsInputBlocked = false;
                return;
            }

            bool anyOpen = _allPopups.Any(p => p != null && p.IsOpen);

            if (PlayerInput != null)
                PlayerInput.IsInputBlocked = anyOpen;

            Cursor.visible   = anyOpen;
            Cursor.lockState = anyOpen ? CursorLockMode.None : CursorLockMode.Locked;

            if (crosshair != null)
                crosshair.SetActive(!anyOpen);
        }

        // ── 메인메뉴용 Public API ──────────────────────────────────────
        public void OpenSettings() => uiSettings?.Open();

        // ──────────────────────────────────────────────

        private void TryOpenInventory()
        {
            if (isMainMenu) return;
            if (CurPopup != null && CurPopup != uiInventory) return;
            uiInventory?.Toggle();
            RefreshCursorAndUI();
        }

        private void TryOpenEscMenu()
        {
            UIPopup popup = CurPopup;

            if (popup != null)
            {
                popup.Close();
                return;
            }

            // 메인메뉴면 ESCMenu 대신 Settings 닫기
            if (isMainMenu) return;

            uiEscMenu?.Open();
        }
        
        private void TryOpenRecipeBook()
        {
            if (isMainMenu) return;
            uiRecipeBook?.Toggle();
            RefreshCursorAndUI();
        }

        private void TryRegister(UIPopup popup)
        {
            if (popup != null && !_allPopups.Contains(popup))
                _allPopups.Add(popup);
        }
    }
}