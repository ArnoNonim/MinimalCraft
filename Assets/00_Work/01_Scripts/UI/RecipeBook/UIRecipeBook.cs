using System.Collections.Generic;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.UI.UIs;
using TMPro;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.RecipeBook
{
    /// <summary>
    /// 레시피 북 팝업.
    /// 제작 가능한 레시피 카드 목록만 표시.
    /// 각 카드(UIRecipeEntry)가 그리드 + 결과 아이콘을 자체적으로 표시.
    /// </summary>
    public class UIRecipeBook : UIPopup
    {
        [Header("목록")]
        [SerializeField] private Transform        entryParent;
        [SerializeField] private GameObject       entryPrefab;
        [SerializeField] private TMP_Text         emptyText;

        [Header("참조")]
        [SerializeField] private Inventory        inventory;
        [SerializeField] private RecipeBookSystem recipeBookSystem;

        private readonly List<UIRecipeEntry> _entries = new();

        // ──────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            UIManager.Instance?.RegisterPopup(this);
        }

        private void OnDestroy()
        {
            UIManager.Instance?.UnregisterPopup(this);
            if (inventory != null) inventory.OnChanged -= RefreshList;
        }

        protected override void OnOpen()
        {
            inventory.OnChanged += RefreshList;
            RefreshList();
        }

        protected override void OnClose()
        {
            inventory.OnChanged -= RefreshList;
        }

        // ──────────────────────────────────────────────

        private void RefreshList()
        {
            foreach (var e in _entries)
                if (e != null) Destroy(e.gameObject);
            _entries.Clear();

            var craftable = recipeBookSystem.GetCraftableRecipes();

            if (emptyText != null)
                emptyText.gameObject.SetActive(craftable.Count == 0);

            foreach (var recipe in craftable)
            {
                var go    = Instantiate(entryPrefab, entryParent);
                var entry = go.GetComponent<UIRecipeEntry>();
                entry.Setup(recipe);
                _entries.Add(entry);
            }
        }
    }
}