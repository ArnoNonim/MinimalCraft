using _00_Work._01_Scripts.Item;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIInventory : UIPopup
    {
        [Header("참조")]
        public Inventory inventory;
        public UICraftingQuick craftingQuick;
        
        [Header("슬롯 설정")]
        public GameObject slotPrefab;
        public Transform  inventorySlotsParent;
        public Transform  hotbarSlotsParent;
        public int        slotSize = 27;
        
        private UISlot[] _inventorySlots;
        private UISlot[] _hotbarSlots;

        protected override void Awake()
        {
            base.Awake();

            // 인벤토리 슬롯 자동 생성
            if (_inventorySlots == null || _inventorySlots.Length == 0)
            {
                _inventorySlots = UISlotFactory.CreateSlots(
                    inventorySlotsParent != null
                        ? inventorySlotsParent : transform,
                    Inventory.InventorySize,
                    slotPrefab,
                    slotSize);
            }

            // 핫바 슬롯 자동 생성
            if (_hotbarSlots == null || _hotbarSlots.Length == 0)
            {
                _hotbarSlots = UISlotFactory.CreateSlots(
                    hotbarSlotsParent != null
                        ? hotbarSlotsParent : transform,
                    Inventory.HotbarSize,
                    slotPrefab,
                    slotSize);
            }

            // 슬롯 설정 자동화
            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                _inventorySlots[i].slotType = UISlot.SlotType.Inventory;
                _inventorySlots[i].SlotIndex      = i;
                _inventorySlots[i].Inventory      = inventory;
            }

            for (int i = 0; i < _hotbarSlots.Length; i++)
            {
                _inventorySlots[i].slotType = UISlot.SlotType.Inventory;
                _hotbarSlots[i].SlotIndex      = Inventory.InventorySize + i;
                _hotbarSlots[i].Inventory      = inventory;
            }
        }
        
        void OnEnable()
        {
            inventory.OnChanged += Refresh;


            Refresh();
        }

        void OnDisable()
        {
            inventory.OnChanged -= Refresh;
        }

        void Refresh()
        {
            for (int i = 0; i < Inventory.InventorySize; i++)
            {
                _inventorySlots[i].SlotIndex = i;
                _inventorySlots[i].Inventory = inventory;
                _inventorySlots[i].UpdateSlot(inventory.slots[i]);
            }

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                int idx = Inventory.InventorySize + i;
                _hotbarSlots[i].SlotIndex = idx;
                _hotbarSlots[i].Inventory = inventory;
                _hotbarSlots[i].UpdateSlot(inventory.slots[idx]);
            }
        }
        
        protected override void OnOpen()
        {

        }

        protected override void OnClose()
        {
            craftingQuick.ReturnItems();
        }
    }
}