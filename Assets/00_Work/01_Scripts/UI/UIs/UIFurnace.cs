using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Save;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIFurnace : UIPopup
    {
        [Header("참조")]
        public Inventory inventory;

        [Header("슬롯 부모")]
        public Transform fuelSlotParent;
        public Transform inputSlotParent;
        public Transform resultSlotParent;

        [Header("인벤토리")]
        public Transform inventorySlotsParent;
        public Transform hotbarSlotsParent;

        [Header("프리팹")]
        public GameObject slotPrefab;

        [Header("설정")]
        public int slotSize = 27;

        [Header("UI")]
        public Image progressFill;
        public Image fuelFill;

        private UISlot   _fuelSlot;
        private UISlot   _inputSlot;
        private UISlot   _resultSlot;
        private UISlot[] _inventorySlots;
        private UISlot[] _hotbarSlots;

        private Vector3Int  _currentPos;
        private FurnaceData _data;

        protected override void Awake()
        {
            base.Awake();
            CreateFurnaceSlots();
            CreateInventorySlots();
        }

        void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }

        void OnDestroy()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.UnregisterPopup(this);
            if (FurnaceManager.Instance != null)
                FurnaceManager.Instance.OnFurnaceChanged -= OnFurnaceChanged;
        }

        void OnEnable()
        {
            inventory.OnChanged += RefreshInventory;
            if (FurnaceManager.Instance != null)
                FurnaceManager.Instance.OnFurnaceChanged += OnFurnaceChanged;
        }

        void OnDisable()
        {
            inventory.OnChanged -= RefreshInventory;
            if (FurnaceManager.Instance != null)
                FurnaceManager.Instance.OnFurnaceChanged -= OnFurnaceChanged;
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        public void OpenAt(Vector3Int pos)
        {
            _currentPos = pos;
            _data       = FurnaceManager.Instance.GetOrCreate(pos);

            // 구독 보장 — OnEnable 시점에 Instance가 없었을 경우 대비
            FurnaceManager.Instance.OnFurnaceChanged -= OnFurnaceChanged;
            FurnaceManager.Instance.OnFurnaceChanged += OnFurnaceChanged;

            Open();
        }

        public override void Open()
        {
            base.Open();
            RefreshAllSlots();
            RefreshInventory();
            UIManager.Instance?.RefreshCursorAndUI();
        }

        // ──────────────────────────────────────────────
        // FurnaceManager 변경 알림
        // ──────────────────────────────────────────────

        void OnFurnaceChanged(Vector3Int pos)
        {
            if (!IsOpen || pos != _currentPos) return;
            RefreshAllSlots();
        }

        // ──────────────────────────────────────────────
        // UI 갱신
        // ──────────────────────────────────────────────

        void RefreshAllSlots()
        {
            if (_data == null) return;

            _fuelSlot.SetCraftingItemSilent(_data.fuel);
            _inputSlot.SetCraftingItemSilent(_data.input);
            _resultSlot.SetResultItem(_data.result);

            RefreshBars();
        }

        void RefreshBars()
        {
            if (_data == null) return;

            if (fuelFill != null)
                fuelFill.fillAmount = _data.maxFuel > 0
                    ? _data.remainingFuel / _data.maxFuel
                    : 0f;

            if (progressFill != null)
            {
                var recipe = _data.input != null
                    ? FurnaceSystem.Instance?.GetRecipe(_data.input.item)
                    : null;

                progressFill.fillAmount = recipe != null && recipe.smeltTime > 0
                    ? _data.smeltTimer / recipe.smeltTime
                    : 0f;
            }
        }

        void RefreshInventory()
        {
            if (inventory == null) return;
            for (int i = 0; i < Inventory.InventorySize; i++)
                _inventorySlots[i].UpdateSlot(inventory.slots[i]);
            for (int i = 0; i < Inventory.HotbarSize; i++)
                _hotbarSlots[i].UpdateSlot(inventory.slots[Inventory.InventorySize + i]);
        }

        // ──────────────────────────────────────────────
        // 슬롯 이벤트
        // ──────────────────────────────────────────────

        void OnFuelSlotChanged(int _, Item.SO.ItemSO item)
        {
            if (_data == null) return;
            _data.fuel = item != null
                ? new ItemStack(item, _fuelSlot.CraftingCount)
                : null;
        }

        void OnInputSlotChanged(int _, Item.SO.ItemSO item)
        {
            if (_data == null) return;
            _data.input = item != null
                ? new ItemStack(item, _inputSlot.CraftingCount)
                : null;
        }

        void OnResultTaken(int amount, bool isShift)
        {
            if (_data?.result == null) return;

            if (isShift && inventory != null)
                inventory.AddItem(_data.result.item, amount);

            _data.result.count -= amount;
            if (_data.result.count <= 0)
                _data.result = null;
        }

        // ──────────────────────────────────────────────
        // 슬롯 생성
        // ──────────────────────────────────────────────

        void CreateFurnaceSlots()
        {
            _fuelSlot   = UISlotFactory.Create(fuelSlotParent,   slotPrefab, slotSize);
            _inputSlot  = UISlotFactory.Create(inputSlotParent,  slotPrefab, slotSize);
            _resultSlot = UISlotFactory.Create(resultSlotParent, slotPrefab, slotSize);

            _fuelSlot.Inventory  = null;
            _inputSlot.Inventory = null;

            _fuelSlot.slotType   = UISlot.SlotType.FurnaceFuel;
            _inputSlot.slotType  = UISlot.SlotType.FurnaceInput;
            _resultSlot.slotType = UISlot.SlotType.Result;

            _resultSlot.Inventory    = inventory;
            _resultSlot.OnResultTaken += OnResultTaken;

            CenterSlot(_fuelSlot);
            CenterSlot(_inputSlot);
            CenterSlot(_resultSlot);

            _fuelSlot.OnItemChanged  += OnFuelSlotChanged;
            _inputSlot.OnItemChanged += OnInputSlotChanged;
        }

        void CenterSlot(UISlot slot)
        {
            var rect              = slot.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(slotSize, slotSize);
        }

        void CreateInventorySlots()
        {
            _inventorySlots = UISlotFactory.CreateSlots(
                inventorySlotsParent, Inventory.InventorySize, slotPrefab, slotSize);
            _hotbarSlots = UISlotFactory.CreateSlots(
                hotbarSlotsParent, Inventory.HotbarSize, slotPrefab, slotSize);

            for (int i = 0; i < Inventory.InventorySize; i++)
            {
                _inventorySlots[i].Inventory = inventory;
                _inventorySlots[i].SlotIndex = i;
            }
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                _hotbarSlots[i].Inventory = inventory;
                _hotbarSlots[i].SlotIndex = Inventory.InventorySize + i;
            }
        }
    }
}