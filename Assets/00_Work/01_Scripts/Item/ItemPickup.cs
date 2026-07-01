using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class ItemPickup : MonoBehaviour
    {
        public Inventory inventory;

        public bool TryPickup(WorldItem worldItem)
        {
            if (!worldItem.CanPickup) return false;
            return inventory.AddItem(worldItem.item, worldItem.count);
        }
    }
}