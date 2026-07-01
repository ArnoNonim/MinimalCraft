using _00_Work._01_Scripts.Item.SO;

namespace _00_Work._01_Scripts.Item
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemSO item;
        public int    count;
        public int    instanceId;
        public int    durability; // ← 내구도를 ItemStack에 직접 저장

        private static int _nextId = 1;

        // ── 일반 생성자 ───────────────────────────────────────────────
        public ItemStack(ItemSO item, int count = 1)
        {
            this.item  = item;
            this.count = count;
            instanceId = item != null ? _nextId++ : 0;

            // 도구면 최대 내구도로 초기화
            if (item is ToolItemSO tool && tool.toolData != null)
                durability = tool.toolData.maxDurability;
            else
                durability = -1; // 내구도 없음
        }

        // ── 빈 슬롯용 ────────────────────────────────────────────────
        private ItemStack()
        {
            item       = null;
            count      = 0;
            instanceId = 0;
            durability = -1;
        }

        public static ItemStack Empty() => new ItemStack();

        // ──────────────────────────────────────────────

        public bool HasDurability =>
            item is ToolItemSO tool &&
            tool.toolData != null  &&
            durability >= 0;

        public int GetDurability() => durability;

        public void ReduceDurability(int amount = 1)
        {
            if (!HasDurability) return;
            durability -= amount;
            if (durability < 0) durability = 0;
        }

        public bool IsBroken => HasDurability && durability <= 0;

        /// <summary>빈 슬롯을 아이템으로 채울 때 — 새 instanceId 발급</summary>
        public void SetItem(ItemSO newItem, int newCount)
        {
            item       = newItem;
            count      = newCount;
            instanceId = (newItem != null) ? _nextId++ : 0;

            if (newItem is ToolItemSO tool && tool.toolData != null)
                durability = tool.toolData.maxDurability;
            else
                durability = -1;
        }

        public bool IsEmpty => item == null || count <= 0;

        public void Clear()
        {
            item       = null;
            count      = 0;
            instanceId = 0;
            durability = -1;
        }
    }
}