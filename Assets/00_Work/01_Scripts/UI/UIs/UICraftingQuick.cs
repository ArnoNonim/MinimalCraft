using System;
using System.Collections.Generic;
using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UICraftingQuick : MonoBehaviour
    {
        [Header("슬롯 설정")]
        public GameObject slotPrefab;
        public Transform  craftingSlotsParent;
        public Transform  resultSlotParent;
        public int        slotSize = 50;

        [Header("참조")]
        public Inventory inventory;

        private UISlot   _resultSlot;
        private UISlot[] _craftingSlots;

        private const int TableSize = 2;

        private readonly ItemSO[]     _grid = new ItemSO[4];
        private CraftingRecipeSO      _currentRecipe;
        private Action<int, ItemSO>[] _slotCallbacks;

        void Awake()
        {
            _craftingSlots = UISlotFactory.CreateSlots(
                craftingSlotsParent != null ? craftingSlotsParent : transform,
                TableSize * TableSize,
                slotPrefab,
                slotSize);

            for (int i = 0; i < _craftingSlots.Length; i++)
            {
                _craftingSlots[i].slotType = UISlot.SlotType.Crafting;
                _craftingSlots[i].SlotIndex      = i;
            }

            _resultSlot = UISlotFactory.Create(
                resultSlotParent != null ? resultSlotParent : transform,
                slotPrefab,
                slotSize);

            var resultRect              = _resultSlot.GetComponent<RectTransform>();
            resultRect.anchorMin        = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax        = new Vector2(0.5f, 0.5f);
            resultRect.pivot            = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = Vector2.zero;
            resultRect.sizeDelta        = new Vector2(slotSize, slotSize);

            _resultSlot.slotType = UISlot.SlotType.Result;
            _resultSlot.Inventory      = inventory;
        }

        void Start()
        {
            foreach (var slot in _craftingSlots)
                slot.UpdateSlot(null);

            _resultSlot.SetResultItem(null);
            _resultSlot.OnResultTaken += (_, isShift) =>
            {
                if (_currentRecipe == null) return;
    
                var recipe = _currentRecipe; // ← 먼저 캡처
    
                ConsumeIngredients(); // 재료 차감 + RefreshResult
    
                if (isShift && inventory != null)
                    inventory.AddItem(recipe.resultItem, recipe.resultCount); // ← 차감 후 추가
            };
        }

        void OnEnable()
        {
            _slotCallbacks = new Action<int, ItemSO>[_craftingSlots.Length];
            for (int i = 0; i < _craftingSlots.Length; i++)
            {
                int idx = i;
                _slotCallbacks[i] = (_, item) => OnSlotChanged(idx, item);
                _craftingSlots[i].OnItemChanged += _slotCallbacks[i];
            }
        }

        void OnDisable()
        {
            if (_slotCallbacks == null) return;
            for (int i = 0; i < _craftingSlots.Length; i++)
                _craftingSlots[i].OnItemChanged -= _slotCallbacks[i];
            _slotCallbacks = null;
        }
        
        public void ReturnItems()
        {
            if (inventory == null) return;

            for (int i = 0; i < _craftingSlots.Length; i++)
            {
                if (_craftingSlots[i].CraftingItem == null) continue;
                inventory.AddItem(_craftingSlots[i].CraftingItem, _craftingSlots[i].CraftingCount);
                _craftingSlots[i].SetCraftingItem(null, 0);
            }

            Array.Clear(_grid, 0, _grid.Length);
            RefreshResult();
        }

        void OnSlotChanged(int index, ItemSO item)
        {
            _grid[index] = item;
            RefreshResult();
        }

        void RefreshResult()
        {
            if (CraftingSystem.Instance == null) return;

            _currentRecipe = CraftingSystem.Instance.TryGetRecipe(_grid, TableSize);

            if (_currentRecipe != null)
            {
                if (_currentRecipe.resultItem is BlockItemSO b)
                    ItemIconRenderer.Instance?.GetIcon(b.blockType);
                else if (_currentRecipe.resultItem is ToolItemSO t)
                    ItemIconRenderer.Instance?.GetToolIcon(t);

                _resultSlot.SetResultItem(new ItemStack(
                    _currentRecipe.resultItem,
                    _currentRecipe.resultCount));
            }
            else
            {
                _resultSlot.SetResultItem(null);
                _currentRecipe = null;
            }
        }

        // ── 결과 슬롯 클릭 처리 ───────────────────────────────────────────


        // ── 핵심 로직 ──────────────────────────────────────────────────────

        /// <summary>
        /// 재료 슬롯당 1개씩 차감
        /// required 딕셔너리로 아이템 종류별 필요 수량 추적
        /// </summary>
        void ConsumeIngredients()
        {
            if (_currentRecipe == null) return;

            // 이벤트 일시 해제 — 차감 중 RefreshResult 중복 호출 방지
            for (int i = 0; i < _craftingSlots.Length; i++)
                _craftingSlots[i].OnItemChanged -= _slotCallbacks[i];

            var ingredients = _currentRecipe.recipeType == RecipeType.Shaped
                ? _currentRecipe.ingredients
                : _currentRecipe.shapelessIngredients;

            // 아이템 종류별 필요 수량 계산
            var required = new Dictionary<ItemSO, int>();
            foreach (var ing in ingredients)
            {
                if (ing == null) continue;
                if (!required.ContainsKey(ing)) required[ing] = 0;
                required[ing]++;
            }

            // 슬롯당 1개씩 차감
            for (int i = 0; i < _grid.Length; i++)
            {
                ItemSO gridItem = _grid[i];
                if (gridItem == null) continue;
                if (!required.TryGetValue(gridItem, out int need) || need <= 0) continue;

                int remaining = _craftingSlots[i].CraftingCount - 1;

                if (remaining <= 0)
                {
                    _grid[i] = null;
                    _craftingSlots[i].SetCraftingItem(null, 0);
                }
                else
                    _craftingSlots[i].SetCraftingItem(gridItem, remaining);

                required[gridItem]--;
            }

            // 이벤트 재등록
            for (int i = 0; i < _craftingSlots.Length; i++)
                _craftingSlots[i].OnItemChanged += _slotCallbacks[i];

            for (int i = 0; i < _craftingSlots.Length; i++)
                _grid[i] = _craftingSlots[i].CraftingItem;

            RefreshResult();
        }
    }
}