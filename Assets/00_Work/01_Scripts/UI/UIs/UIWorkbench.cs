using System;
using System.Collections.Generic;
using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIWorkbench : UIPopup
    {
        [Header("참조")]
        public Inventory inventory;

        [Header("Crafting")]
        public Transform craftingSlotsParent; // GridLayoutGroup
        public Transform resultSlotParent;    // 빈 부모

        [Header("Inventory")]
        public Transform inventorySlotsParent; // GridLayoutGroup
        public Transform hotbarSlotsParent;    // GridLayoutGroup

        [Header("Slot")]
        public GameObject slotPrefab;
        public int slotSize = 27;

        private UISlot[] _craftingSlots;
        private UISlot[] _inventorySlots;
        private UISlot[] _hotbarSlots;
        private UISlot   _resultSlot;

        private readonly ItemSO[] _grid = new ItemSO[9];

        private CraftingRecipeSO _currentRecipe;
        private Action<int, ItemSO>[] _craftCallbacks;

        protected override void Awake()
        {
            base.Awake();

            CreateCraftingSlots();
            CreateResultSlot();
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
        }

        void OnEnable()
        {
            inventory.OnChanged += RefreshInventory;

            _craftCallbacks = new Action<int, ItemSO>[9];

            for (int i = 0; i < 9; i++)
            {
                int idx = i;

                _craftCallbacks[i] =
                    (_, item) => OnCraftingSlotChanged(idx, item);

                _craftingSlots[i].OnItemChanged +=
                    _craftCallbacks[i];

                _craftingSlots[i].UpdateSlot(null);
            }

            _resultSlot.SetResultItem(null);

            RefreshInventory();
        }

        void OnDisable()
        {
            if (inventory != null)
                inventory.OnChanged -= RefreshInventory;

            if (_craftCallbacks == null)
                return;

            for (int i = 0; i < 9; i++)
                _craftingSlots[i].OnItemChanged -= _craftCallbacks[i];
        }

        public override void Open()
        {
            base.Open();

            RefreshAllSlots();
            ForceRefreshAllIcons();

            UIManager.Instance?.RefreshCursorAndUI();
        }
        
        public override void Close()
        {
            ReturnCraftItems();

            base.Close();
        }
        
        void ForceRefreshAllIcons()
        {
            if (ItemIconRenderer.Instance == null)
                return;

            for (int i = 0; i < Inventory.InventorySize; i++)
            {
                var stack = inventory.slots[i];

                if (stack == null ||
                    stack.IsEmpty ||
                    stack.item == null)
                    continue;

                ItemIconRenderer.Instance
                    .GetIconAuto(stack.item);

                _inventorySlots[i]
                    .UpdateSlot(stack);
            }

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                int idx = Inventory.InventorySize + i;

                var stack = inventory.slots[idx];

                if (stack == null ||
                    stack.IsEmpty ||
                    stack.item == null)
                    continue;

                ItemIconRenderer.Instance
                    .GetIconAuto(stack.item);

                _hotbarSlots[i]
                    .UpdateSlot(stack);
            }

            foreach (var slot in _craftingSlots)
            {
                if (slot.CraftingItem == null)
                    continue;

                ItemIconRenderer.Instance
                    .GetIconAuto(slot.CraftingItem);

                slot.UpdateSlot(
                    new ItemStack(
                        slot.CraftingItem,
                        slot.CraftingCount));
            }

            if (_currentRecipe != null)
            {
                ItemIconRenderer.Instance
                    .GetIconAuto(
                        _currentRecipe.resultItem);

                _resultSlot.SetResultItem(
                    new ItemStack(
                        _currentRecipe.resultItem,
                        _currentRecipe.resultCount));
            }
        }

        void CreateCraftingSlots()
        {
            _craftingSlots = UISlotFactory.CreateSlots(
                craftingSlotsParent,
                9,
                slotPrefab,
                slotSize);

            for (int i = 0; i < _craftingSlots.Length; i++)
            {
                _craftingSlots[i].slotType = UISlot.SlotType.Crafting;
                _craftingSlots[i].Inventory      = null;
                _craftingSlots[i].SlotIndex      = i;

                _craftingSlots[i].SetCraftingItem(null, 0);
            }
        }

        void CreateResultSlot()
        {
            _resultSlot = UISlotFactory.Create(
                resultSlotParent,
                slotPrefab,
                slotSize);

            _resultSlot.slotType = UISlot.SlotType.Result;
            _resultSlot.Inventory      = inventory;

            _resultSlot.SetResultItem(null);

            _resultSlot.OnResultTaken += (count, isShift) =>
            {
                if (isShift)
                    inventory.AddItem(_currentRecipe.resultItem, _currentRecipe.resultCount);
                ConsumeIngredients();
            };
            
            var rect = _resultSlot.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(slotSize, slotSize);
        }

        void CreateInventorySlots()
        {
            _inventorySlots = UISlotFactory.CreateSlots(
                inventorySlotsParent,
                Inventory.InventorySize,
                slotPrefab,
                slotSize);

            _hotbarSlots = UISlotFactory.CreateSlots(
                hotbarSlotsParent,
                Inventory.HotbarSize,
                slotPrefab,
                slotSize);

            for (int i = 0; i < Inventory.InventorySize; i++)
            {
                _inventorySlots[i].slotType = UISlot.SlotType.Inventory;
                _inventorySlots[i].Inventory      = inventory;
                _inventorySlots[i].SlotIndex      = i;
            }

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                _hotbarSlots[i].slotType = UISlot.SlotType.Inventory;
                _hotbarSlots[i].Inventory      = inventory;
                _hotbarSlots[i].SlotIndex      = Inventory.InventorySize + i;
            }
        }

        void OnCraftingSlotChanged(int index, ItemSO item)
        {
            _grid[index] = item;
            RefreshCraftingResult();
        }

        void RefreshCraftingResult()
        {
            if (CraftingSystem.Instance == null) return;

            _currentRecipe = CraftingSystem.Instance.TryGetRecipe(_grid, 3);

            if (_currentRecipe == null)
            {
                _resultSlot.SetResultItem(null);
                return;
            }

            var resultStack = new ItemStack(
                _currentRecipe.resultItem,
                _currentRecipe.resultCount);
            
            _resultSlot.SetResultItem(resultStack);
        }
        
        public void RefreshAllSlots()
        {
            RefreshInventory();

            foreach (var slot in _craftingSlots)
            {
                if (slot.CraftingItem == null)
                {
                    slot.UpdateSlot(null);
                    continue;
                }

                slot.UpdateSlot(
                    new ItemStack(
                        slot.CraftingItem,
                        slot.CraftingCount));
            }

            RefreshCraftingResult();
        }

        void RefreshInventory()
        {
            for (int i = 0; i < Inventory.InventorySize; i++)
            {
                _inventorySlots[i].SlotIndex = i;
                _inventorySlots[i].Inventory = inventory;

                _inventorySlots[i]
                    .UpdateSlot(inventory.slots[i]);
            }

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                int idx = Inventory.InventorySize + i;

                _hotbarSlots[i].SlotIndex = idx;
                _hotbarSlots[i].Inventory = inventory;

                _hotbarSlots[i]
                    .UpdateSlot(inventory.slots[idx]);
            }
        }

        void ConsumeIngredients()
        {
            if (_currentRecipe == null)
                return;

            for (int i = 0; i < _craftingSlots.Length; i++)
                _craftingSlots[i].OnItemChanged -= _craftCallbacks[i];

            var ingredients =
                _currentRecipe.recipeType == RecipeType.Shaped
                    ? _currentRecipe.ingredients
                    : _currentRecipe.shapelessIngredients;

            var required =
                new Dictionary<ItemSO, int>();

            foreach (var ing in ingredients)
            {
                if (ing == null)
                    continue;

                if (!required.ContainsKey(ing))
                    required[ing] = 0;

                required[ing]++;
            }

            for (int i = 0; i < _grid.Length; i++)
            {
                ItemSO gridItem = _grid[i];

                if (gridItem == null)
                    continue;

                if (!required.TryGetValue(gridItem, out int need))
                    continue;

                if (need <= 0)
                    continue;

                int remain =
                    _craftingSlots[i].CraftingCount - 1;

                if (remain <= 0)
                {
                    _grid[i] = null;
                    _craftingSlots[i].SetCraftingItem(null, 0);
                }
                else
                {
                    _craftingSlots[i].SetCraftingItem(
                        gridItem,
                        remain);
                }

                required[gridItem]--;
            }

            for (int i = 0; i < _craftingSlots.Length; i++)
                _grid[i] = _craftingSlots[i].CraftingItem;

            for (int i = 0; i < _craftingSlots.Length; i++)
                _craftingSlots[i].OnItemChanged += _craftCallbacks[i];

            RefreshCraftingResult();
        }

        void ReturnCraftItems()
        {
            if (inventory == null)
                return;

            foreach (var slot in _craftingSlots)
            {
                if (slot.CraftingItem == null)
                    continue;

                inventory.AddItem(
                    slot.CraftingItem,
                    slot.CraftingCount);

                slot.SetCraftingItem(null, 0);
            }

            Array.Clear(_grid, 0, _grid.Length);

            RefreshCraftingResult();
        }
    }
}
