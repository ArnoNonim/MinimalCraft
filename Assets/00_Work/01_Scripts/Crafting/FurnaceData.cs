using _00_Work._01_Scripts.Item;

namespace _00_Work._01_Scripts.Crafting
{
    [System.Serializable]
    public class FurnaceData
    {
        public ItemStack input;
        public ItemStack fuel;
        public ItemStack result;

        public float smeltTimer;
        public float maxFuel;
        public float remainingFuel;
    }
}