using System.Collections.Generic;
using UnityEngine;

namespace _00_Work._01_Scripts.Crafting
{
    public class FurnaceSystem : MonoBehaviour
    {
        public static FurnaceSystem Instance;

        [SerializeField]
        private List<FurnaceRecipeSO> recipes =
            new();

        private Dictionary<int, FurnaceRecipeSO>
            _recipeMap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadRecipes();
        }

        void LoadRecipes()
        {
            recipes.Clear();

            recipes.AddRange(
                Resources.LoadAll<FurnaceRecipeSO>(
                    "Recipe"));

            _recipeMap =
                new Dictionary<int, FurnaceRecipeSO>();

            foreach (var recipe in recipes)
            {
                if (recipe == null ||
                    recipe.inputItem == null)
                    continue;

                _recipeMap[
                        recipe.inputItem.GetInstanceID()] =
                    recipe;
            }
        }

        public FurnaceRecipeSO GetRecipe(
            Item.SO.ItemSO input)
        {
            if (input == null)
                return null;

            _recipeMap.TryGetValue(
                input.GetInstanceID(),
                out var recipe);

            return recipe;
        }
    }
}