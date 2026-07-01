using System;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class Inventory : MonoBehaviour
    {
        public const int InventorySize = 27;
        public const int HotbarSize    = 9;
        public const int TotalSize     = InventorySize + HotbarSize;

        public ItemStack[] slots;

        public event Action OnChanged;

        public Inventory()
        {
            slots = new ItemStack[TotalSize];
            for (int i = 0; i < TotalSize; i++)
                slots[i] = ItemStack.Empty(); // ← id 소비 안 함
        }

        // ──────────────────────────────────────────────
        // 아이템 추가
        // ──────────────────────────────────────────────

        public bool AddItem(ItemSO item, int count = 1)
        {
            // 1. 핫바 기존 스택 합치기
            if (TryAddToRange(item, ref count, InventorySize, TotalSize))
                if (count <= 0) return true;

            // 2. 인벤 기존 스택 합치기
            if (TryAddToRange(item, ref count, 0, InventorySize))
                if (count <= 0) return true;

            // 3. 핫바 빈 슬롯
            if (TryAddToEmptySlot(item, ref count, InventorySize, TotalSize))
                if (count <= 0) return true;

            // 4. 인벤 빈 슬롯
            if (TryAddToEmptySlot(item, ref count, 0, InventorySize))
                if (count <= 0) return true;

            if (count > 0)
            {
                Debug.Log("인벤토리가 가득 찼습니다.");
                return false;
            }
            return true;
        }

        bool TryAddToRange(ItemSO item, ref int count, int from, int to)
        {
            // 도구는 instanceId 보존 필요 — 스택 합치기 금지
            if (item is ToolItemSO) return false;

            for (int i = from; i < to; i++)
            {
                var slot = slots[i];
                if (slot.item != item) continue;
                if (slot.count >= item.maxStack) continue;

                int canAdd = item.maxStack - slot.count;
                int adding = Mathf.Min(count, canAdd);
                slot.count += adding;
                count      -= adding;
                OnChanged?.Invoke();
                if (count <= 0) return true;
            }
            return false;
        }

        bool TryAddToEmptySlot(ItemSO item, ref int count, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                if (!slots[i].IsEmpty) continue;

                // SetItem으로 새 instanceId 발급
                int give = Mathf.Min(count, item.maxStack);
                slots[i].SetItem(item, give);
                count -= give;
                OnChanged?.Invoke();
                if (count <= 0) return true;
            }
            return false;
        }

        // ──────────────────────────────────────────────
        // 아이템 제거
        // ──────────────────────────────────────────────

        public bool RemoveItem(ItemSO item, int count = 1)
        {
            for (int i = 0; i < TotalSize; i++)
            {
                var slot = slots[i];
                if (slot.item != item) continue;

                int removing = Mathf.Min(count, slot.count);
                slot.count -= removing;
                if (slot.count <= 0) slot.Clear();
                count -= removing;
                OnChanged?.Invoke();
                if (count <= 0) return true;
            }
            return false;
        }

        // ──────────────────────────────────────────────
        // 유틸
        // ──────────────────────────────────────────────

        public bool IsFull()
        {
            foreach (var slot in slots)
                if (slot.IsEmpty) return false;
            return true;
        }

        public void NotifyChanged() => OnChanged?.Invoke();
    }
}