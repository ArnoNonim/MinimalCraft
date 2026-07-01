using _00_Work._01_Scripts.Block;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.Player;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Tool;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIHotbar : MonoBehaviour
    {
        public static UIHotbar Instance { get; private set; }
        
        [Header("슬롯 설정")]
        public GameObject slotPrefab;   // 프리팹 있으면 연결, 없으면 자동 생성
        public Transform  slotsParent;  // 슬롯 부모 오브젝트
        public int        slotSize = 9;
        
        [Header("참조")]
        public Inventory inventory;
        public BlockBreaker blockBreaker;
        public GameObject    selector;
        public PlayerInputSO playerInput;
        public PlayerStatSO playerStat;
        
        public ToolDataSO    fistsTool;

        private UISlot[]  _hotbarSlots;
        private int _selectedIndex = 0;
        public int SelectedIndex => _selectedIndex;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (_hotbarSlots == null || _hotbarSlots.Length == 0)
            {
                _hotbarSlots = UISlotFactory.CreateSlots(
                    slotsParent != null ? slotsParent : transform,
                    Inventory.HotbarSize,
                    slotPrefab,
                    slotSize);
            }

            // 슬롯 설정 자동화
            for (int i = 0; i < _hotbarSlots.Length; i++)
            {
                _hotbarSlots[i].slotType = UISlot.SlotType.Inventory;
                _hotbarSlots[i].SlotIndex      = Inventory.InventorySize + i;
                _hotbarSlots[i].Inventory      = inventory;
            }
        }
        
        void Update()
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll == 0f || playerInput.IsInputBlocked) return;

            int dir      = scroll > 0f ? -1 : 1;
            int newIndex = _selectedIndex + dir;

            if (newIndex < 0)
                newIndex = Inventory.HotbarSize - 1;
            else if (newIndex >= Inventory.HotbarSize)
                newIndex = 0;

            OnHotbarSelect(newIndex);
        }
        
        void OnEnable()
        {
            inventory.OnChanged += Refresh;
            playerInput.OnHotbarSelect += OnHotbarSelect;
            Refresh();
        }

        void OnDisable()
        {
            inventory.OnChanged    -= Refresh;
            playerInput.OnHotbarSelect -= OnHotbarSelect;
        }

        void Refresh()
        {
            for (int i = 0; i < Inventory.HotbarSize; i++)
                _hotbarSlots[i].UpdateSlot(
                    inventory.slots[Inventory.InventorySize + i]);
            UpdateCurrentTool();
        }

        void OnHotbarSelect(int index)
        {
            _selectedIndex = index;
            UpdateSelector();
            UpdateCurrentTool();
        }
        
        void UpdateCurrentTool()
        {
            if (blockBreaker == null) return;

            var stack = GetSelectedItem();

            if (stack == null || stack.IsEmpty)
            {
                // 빈 손 — fistsTool 사용
                blockBreaker.currentTool  = fistsTool;
                playerStat.curEfficiency  = fistsTool != null
                    ? fistsTool.miningSpeed : 1;
                return;
            }

            if (stack.item is ToolItemSO toolItem)
            {
                blockBreaker.currentTool = toolItem.toolData;
                playerStat.curEfficiency = toolItem.toolData.miningSpeed;
            }
            else
            {
                // 블록 아이템 — 맨손과 동일
                blockBreaker.currentTool = fistsTool;
                playerStat.curEfficiency = fistsTool != null
                    ? fistsTool.miningSpeed : 1;
            }
        }

        void UpdateSelector()
        {
            if (selector == null) return;
            selector.transform.position =
                _hotbarSlots[_selectedIndex].transform.position;
        }

        // 현재 선택된 슬롯 아이템
        public ItemStack GetSelectedItem()
            => inventory.slots[Inventory.InventorySize + _selectedIndex];
    }
}