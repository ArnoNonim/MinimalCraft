using System.Collections.Generic;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISettingsKeybindings : UIPopup
    {
        [SerializeField] private PlayerInputSO playerInput;
        
        [Header("키 바인딩 UI")]
        [SerializeField] private Transform scrollContent;
        [SerializeField] private GameObject keybindRowPrefab;

        [Header("InputActionAsset")]
        [SerializeField] private InputActionAsset actionAsset;

        [Header("액션 이름 오버라이드")]
        [SerializeField] private List<ActionNameOverride> nameOverrides = new();

        [System.Serializable]
        public class ActionNameOverride
        {
            public string actionId;
            public string displayName;
        }

        private PlayerSettingsData _data;
        private InputActionRebindingExtensions.RebindingOperation _rebindOp;
        private bool _isRebinding;
        private bool _built;

        private InputActionAsset ActionAsset => playerInput?.InputAsset;
        
        void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }
        
        public void InitData(PlayerSettingsData data)
        {
            _data = data;
            if (!string.IsNullOrEmpty(_data.keybindOverrides))
                ActionAsset?.LoadBindingOverridesFromJson(_data.keybindOverrides);
            if (!_built) { BuildRows(); _built = true; }
        }

        void BuildRows()
        {
            if (scrollContent == null || keybindRowPrefab == null || ActionAsset == null) return;

            foreach (Transform child in scrollContent) Destroy(child.gameObject);

            var playerMap = ActionAsset.FindActionMap("Player");
            if (playerMap == null) return;

            foreach (var action in playerMap.actions)
                for (int b = 0; b < action.bindings.Count; b++)
                {
                    var binding = action.bindings[b];
                    if (binding.isComposite) continue;

                    var row      = Instantiate(keybindRowPrefab, scrollContent);
                    var dispName = GetDisplayName(action);
                    if (binding.isPartOfComposite) dispName += $" ({binding.name})";

                    var nameText = row.transform.Find("ButtonName")?.GetComponent<TMP_Text>();
                    if (nameText) nameText.text = dispName;

                    int  bi  = b;
                    var  act = action;
                    var keyText = row.transform.Find("BindArea")?.GetComponentInChildren<TMP_Text>();
                    if (keyText) keyText.text = InputControlPath.ToHumanReadableString(
                        binding.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);

                    row.transform.Find("BindArea")?.GetComponent<Button>()
                        ?.onClick.AddListener(() => StartRebind(act, bi, keyText));
                    
                    var resetBtn = row.transform.Find("Reset")?.GetComponent<Button>();
                    resetBtn?.onClick.AddListener(() =>
                    {
                        action.RemoveBindingOverride(bi);
                        if (keyText) keyText.text = InputControlPath.ToHumanReadableString(
                            act.bindings[bi].effectivePath,
                            InputControlPath.HumanReadableStringOptions.OmitDevice);
                        if (_data != null) _data.keybindOverrides = ActionAsset.SaveBindingOverridesAsJson();
                    });
                }
            
            var grid = scrollContent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                int rowCount = scrollContent.childCount;
                float height = rowCount * grid.cellSize.y + grid.spacing.y * (rowCount - 1) + 200f;
                var rect = scrollContent.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
        }

        void StartRebind(InputAction action, int bindingIndex, TMP_Text display)
        {
            if (_isRebinding) return;
            _isRebinding = true;

            playerInput.IsInputBlocked = true;
            action.Disable();

            _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => FinishRebind(action, bindingIndex, display))
                .OnCancel(_  => CancelRebind(action))
                .Start();
        }

        void FinishRebind(InputAction action, int bindingIndex, TMP_Text display)
        {
            _rebindOp?.Dispose(); _rebindOp = null; _isRebinding = false;
            action.Enable();
            playerInput.IsInputBlocked = false;
            if (display) display.text = InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
            if (_data != null) _data.keybindOverrides = ActionAsset.SaveBindingOverridesAsJson();
        }

        void CancelRebind(InputAction action)
        {
            _rebindOp?.Dispose(); _rebindOp = null; _isRebinding = false;
            action?.Enable();
            playerInput.IsInputBlocked = false;
        }
        string GetDisplayName(InputAction action)
        {
            foreach (var ov in nameOverrides)
                if (ov.actionId == action.id.ToString()) return ov.displayName;
            return action.name;
        }

        void OnDisable() { if (_isRebinding) CancelRebind(null); }
    }
}