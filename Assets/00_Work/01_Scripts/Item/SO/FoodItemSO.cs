using _00_Work._01_Scripts.Player;
using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "FoodItem", menuName = "Item/FoodItem")]
    public class FoodItemSO : UsableItemSO
    {
        [Header("음식 설정")]
        public int hungerRestore  = 6;
        public int healthRestore  = 0;
        public int thirstyRestore = 0;

        public override void OnUse(GameObject user)
        {
            var stats = user.GetComponentInChildren<PlayerStats>();
            if (stats == null) return;

            if (hungerRestore  > 0) stats.EatFood(hungerRestore);
            if (healthRestore  > 0) stats.Heal(healthRestore);
            if (thirstyRestore > 0) stats.Drink(thirstyRestore);
        }
    }
}