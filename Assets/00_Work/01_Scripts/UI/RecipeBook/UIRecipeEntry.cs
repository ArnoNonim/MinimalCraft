using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.RecipeBook
{
    /// <summary>
    /// 레시피 북 목록의 각 레시피 카드.
    /// 2x2 / 3x3 그리드 전환 + 재료·결과 아이콘 표시.
    /// </summary>
    public class UIRecipeEntry : MonoBehaviour
    {
        [Header("2x2")]
        [SerializeField] private GameObject grid2x2;
        [SerializeField] private RawImage[] slots2x2; // 4개
        [SerializeField] private GameObject arrow2x2;

        [Header("3x3")]
        [SerializeField] private GameObject grid3x3;
        [SerializeField] private RawImage[] slots3x3; // 9개
        [SerializeField] private GameObject arrow3x3;

        [Header("결과")]
        [SerializeField] private RawImage resultSlot;
        [SerializeField] private TMP_Text resultName;

        [Header("선택 배경")]
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor   = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.28f, 0.15f, 0.9f);

        public CraftingRecipeSO Recipe { get; private set; }

        // ──────────────────────────────────────────────

        public void Setup(CraftingRecipeSO recipe)
        {
            Recipe = recipe;

            bool is2x2 = recipe.minTableSize == TableSize.Table2x2;

            // 그리드 전환
            if (grid2x2 != null) grid2x2.SetActive(is2x2);
            if (arrow2x2 != null) arrow2x2.SetActive(is2x2);
            if (grid3x3 != null) grid3x3.SetActive(!is2x2);
            if (arrow3x3 != null) arrow3x3.SetActive(!is2x2);

            // 재료 슬롯 초기화
            ClearSlots(slots2x2);
            ClearSlots(slots3x3);

            // 재료 아이콘 채우기
            var ingredients = recipe.recipeType == RecipeType.Shaped
                ? recipe.ingredients
                : recipe.shapelessIngredients;

            if (is2x2)
                FillSlots(slots2x2, ingredients, 4);
            else
                FillSlots(slots3x3, ingredients, 9);

            // 결과 아이콘
            if (resultSlot != null && recipe.resultItem != null)
                ApplyIcon(resultSlot, recipe.resultItem);
            if (resultName != null && recipe.resultItem != null)
                resultName.text = recipe.resultItem.itemName;   

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
        }

        // ──────────────────────────────────────────────

        private void ClearSlots(RawImage[] slots)
        {
            if (slots == null) return;
            foreach (var s in slots)
            {
                if (s == null) continue;
                s.enabled = false;
                // RawImage 부모(Slot)에서 Amount 찾기
                var tmp = s.transform.parent.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = string.Empty;
            }
        }

        private void FillSlots(RawImage[] slots, ItemSO[] ingredients, int maxCount)
        {
            if (slots == null || ingredients == null) return;
            int len = Mathf.Min(slots.Length, Mathf.Min(ingredients.Length, maxCount));
            for (int i = 0; i < len; i++)
            {
                if (slots[i] == null || ingredients[i] == null) continue;
                ApplyIcon(slots[i], ingredients[i]);
            }
        }

        private void ApplyIcon(RawImage target, ItemSO item)
        {
            if (ItemIconRenderer.Instance == null) return;
            var rt = ItemIconRenderer.Instance.GetIconAuto(item);
            if (rt == null) return;
            target.texture = rt;
            target.enabled = true;
            var tmp = target.transform.parent.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = string.Empty;
        }
    }
}