using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Crafting
{
    [CreateAssetMenu(fileName = "FurnaceRecipe", menuName = "Recipe/Furnace Recipe")]
    public class FurnaceRecipeSO : ScriptableObject
    {
        public ItemSO inputItem;

        public ItemSO resultItem;

        public float smeltTime = 5f;
    }
}