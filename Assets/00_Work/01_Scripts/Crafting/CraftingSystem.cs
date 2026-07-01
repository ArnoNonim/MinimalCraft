using System.Collections.Generic;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Crafting
{
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance;

        [Header("레시피 목록")]
        [SerializeField]
        private List<CraftingRecipeSO> recipes = new();

        public IReadOnlyList<CraftingRecipeSO> Recipes => recipes;

        void Awake()
        {
            Instance = this;

            LoadAllRecipes();
        }

        void LoadAllRecipes()
        {
            recipes.Clear();

            var loadedRecipes =
                Resources.LoadAll<CraftingRecipeSO>("Recipe");

            recipes.AddRange(loadedRecipes);
        }

        public CraftingRecipeSO TryGetRecipe(
            ItemSO[] grid,
            int currentTableSize)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null)
                    continue;

                if (currentTableSize < recipe.GridSize)
                    continue;

                if (RecipePatternMatcher.Matches(
                        recipe,
                        grid,
                        currentTableSize))
                {
                    return recipe;
                }
            }

            return null;
        }
    }
}