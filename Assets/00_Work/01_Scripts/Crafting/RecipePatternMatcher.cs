using _00_Work._01_Scripts.Item.SO;

namespace _00_Work._01_Scripts.Crafting
{
    /// <summary>
    /// Shaped 레시피 패턴 매칭 유틸
    /// 패턴을 최소 바운딩 박스로 정규화해서 위치 무관 비교
    /// </summary>
    public static class RecipePatternMatcher
    {
        /// <summary>
        /// inputGrid(size×size)가 recipe와 매칭되는지 확인
        /// 어느 위치에 배치하든 패턴 형태만 같으면 true
        /// </summary>
        public static bool Matches(CraftingRecipeSO recipe, ItemSO[] inputGrid, int inputSize)
        {
            if (recipe.recipeType == RecipeType.Shapeless)
                return MatchesShapeless(recipe, inputGrid);

            int recipeSize = recipe.GridSize;

            // 레시피 패턴 정규화
            var recipeNorm = Normalize(recipe.ingredients, recipeSize,
                out int rPatW, out int rPatH);

            // 입력 패턴 정규화
            var inputNorm  = Normalize(inputGrid, inputSize,
                out int iPatW, out int iPatH);

            // 크기 불일치
            if (rPatW != iPatW || rPatH != iPatH) return false;

            // 아이템 일치 비교
            for (int i = 0; i < recipeNorm.Length; i++)
            {
                if (recipeNorm[i] != inputNorm[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// 그리드를 최소 바운딩 박스로 잘라낸 1차원 배열 반환
        /// patW, patH: 잘라낸 패턴의 너비/높이
        /// </summary>
        public static ItemSO[] Normalize(ItemSO[] grid, int size,
            out int patW, out int patH)
        {
            int minR = size, maxR = -1, minC = size, maxC = -1;

            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (grid[r * size + c] == null) continue;
                if (r < minR) minR = r;
                if (r > maxR) maxR = r;
                if (c < minC) minC = c;
                if (c > maxC) maxC = c;
            }

            // 빈 그리드
            if (maxR < 0)
            {
                patW = 0; patH = 0;
                return new ItemSO[0];
            }

            patH = maxR - minR + 1;
            patW = maxC - minC + 1;

            var result = new ItemSO[patH * patW];
            for (int r = 0; r < patH; r++)
            for (int c = 0; c < patW; c++)
                result[r * patW + c] = grid[(minR + r) * size + (minC + c)];

            return result;
        }

        static bool MatchesShapeless(CraftingRecipeSO recipe, ItemSO[] inputGrid)
        {
            // Shapeless — 재료 종류·수량만 같으면 됨
            var required = new System.Collections.Generic.Dictionary<ItemSO, int>();
            foreach (var item in recipe.shapelessIngredients)
            {
                if (item == null) continue;
                if (!required.ContainsKey(item)) required[item] = 0;
                required[item]++;
            }

            foreach (var item in inputGrid)
            {
                if (item == null) continue;
                if (!required.ContainsKey(item)) return false;
                required[item]--;
                if (required[item] < 0) return false;
            }

            foreach (var kv in required)
                if (kv.Value != 0) return false;

            return true;
        }
    }
}