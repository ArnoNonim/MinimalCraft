using System.Collections.Generic;
using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.RecipeBook
{
    /// <summary>
    /// 현재 인벤토리 기준으로 제작 가능한 레시피를 계산.
    /// </summary>
    public class RecipeBookSystem : MonoBehaviour
    {
        public static RecipeBookSystem Instance { get; private set; }

        [SerializeField] private Inventory inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ─────────────────────────────────────────────────

        /// <summary>인벤토리 아이템으로 제작 가능한 레시피 목록 반환</summary>
        public List<CraftingRecipeSO> GetCraftableRecipes()
        {
            var result = new List<CraftingRecipeSO>();
            if (CraftingSystem.Instance == null) return result;

            var invCounts = GetInventoryCounts();

            foreach (var recipe in CraftingSystem.Instance.Recipes)
            {
                if (recipe == null) continue;
                if (CanCraft(recipe, invCounts))
                    result.Add(recipe);
            }

            return result;
        }

        /// <summary>특정 레시피가 현재 인벤토리로 제작 가능한지 확인</summary>
        public bool CanCraft(CraftingRecipeSO recipe)
            => CanCraft(recipe, GetInventoryCounts());

        // ── 내부 ───────────────────────────────────────────────────────

        private Dictionary<ItemSO, int> GetInventoryCounts()
        {
            var counts = new Dictionary<ItemSO, int>();
            foreach (var slot in inventory.slots)
            {
                if (slot == null || slot.IsEmpty || slot.item == null) continue;
                if (!counts.ContainsKey(slot.item)) counts[slot.item] = 0;
                counts[slot.item] += slot.count;
            }
            return counts;
        }

        private bool CanCraft(CraftingRecipeSO recipe, Dictionary<ItemSO, int> invCounts)
        {
            // 재료 필요량 계산
            var required = new Dictionary<ItemSO, int>();
            var ingredients = recipe.recipeType == RecipeType.Shaped
                ? recipe.ingredients
                : recipe.shapelessIngredients;

            if (ingredients == null) return false;

            foreach (var ing in ingredients)
            {
                if (ing == null) continue;
                if (!required.ContainsKey(ing)) required[ing] = 0;
                required[ing]++;
            }

            // 인벤토리와 비교
            foreach (var kvp in required)
            {
                invCounts.TryGetValue(kvp.Key, out int have);
                if (have < kvp.Value) return false;
            }

            return true;
        }
    }
}