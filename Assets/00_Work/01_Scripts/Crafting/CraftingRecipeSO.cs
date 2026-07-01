using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Crafting
{
    public enum RecipeType  { Shaped, Shapeless }
    public enum TableSize   { Table2x2 = 2, Table3x3 = 3, Table5x5 = 5 }
    
    [CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Recipe/CraftingRecipe")]
    public class CraftingRecipeSO : ScriptableObject
    {
        [Header("레시피 타입")]
        public RecipeType recipeType   = RecipeType.Shaped;
        public TableSize  minTableSize = TableSize.Table2x2; // 최소 필요 조합대

        public ItemSO resultItem;
        public int    resultCount = 1;

        [Header("Shaped 재료 — 최대 5x5")]
        public ItemSO[] ingredients = new ItemSO[25]; // 5x5

        public ItemSO[] shapelessIngredients;

        // 실제 그리드 크기 반환
        public int GridSize => (int)minTableSize;
    }
}
