#if UNITY_EDITOR
using _00_Work._01_Scripts.Item.SO;
using UnityEditor;
using UnityEngine;

namespace _00_Work._01_Scripts.Crafting.Editor
{
    [CustomEditor(typeof(CraftingRecipeSO))]
    public class CraftingRecipeEditor : UnityEditor.Editor
    {
        private const float SlotSize    = 60f;
        private const float SlotPadding = 4f;

        private Texture2D _emptySlotTex;
        private GUIStyle  _nameStyle;

        private void OnEnable()
        {
            _emptySlotTex = MakeTex(1, 1, new Color(0.18f, 0.18f, 0.18f, 1f));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var recipe = (CraftingRecipeSO)target;

            // ── 기본 필드 ────────────────────────────────────────────────
            EditorGUILayout.LabelField("레시피 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recipeType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minTableSize"));
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("결과물", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resultItem"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resultCount"));
            EditorGUILayout.Space(8);

            if (recipe.recipeType == RecipeType.Shaped)
                DrawShapedGrid(recipe);
            else
                DrawShapelessGrid(recipe);

            serializedObject.ApplyModifiedProperties();
        }

        // ──────────────────────────────────────────────────────────────────
        // Shaped 그리드
        // ──────────────────────────────────────────────────────────────────

        void DrawShapedGrid(CraftingRecipeSO recipe)
        {
            int size = recipe.GridSize;

            if (recipe.ingredients == null || recipe.ingredients.Length < size * size)
            {
                var newArr = new ItemSO[size * size];
                if (recipe.ingredients != null)
                    System.Array.Copy(recipe.ingredients, newArr,
                        Mathf.Min(recipe.ingredients.Length, newArr.Length));
                recipe.ingredients = newArr;
                EditorUtility.SetDirty(recipe);
            }

            EditorGUILayout.LabelField($"Shaped 그리드 ({size}×{size})", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            float totalW  = size * (SlotSize + SlotPadding) - SlotPadding;
            float marginL = Mathf.Max((EditorGUIUtility.currentViewWidth - totalW) * 0.5f, 0f);

            for (int row = 0; row < size; row++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginL);

                for (int col = 0; col < size; col++)
                {
                    int idx  = row * size + col;
                    DrawSlot(recipe.ingredients[idx], idx, (item) =>
                    {
                        Undo.RecordObject(recipe, "Set Ingredient");
                        recipe.ingredients[idx] = item;
                        EditorUtility.SetDirty(recipe);
                    });
                    GUILayout.Space(SlotPadding);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(SlotPadding);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("그리드 초기화", GUILayout.Height(24)))
            {
                Undo.RecordObject(recipe, "Clear Grid");
                recipe.ingredients = new ItemSO[size * size];
                EditorUtility.SetDirty(recipe);
            }
            if (GUILayout.Button("패턴 중앙 정렬", GUILayout.Height(24)))
            {
                Undo.RecordObject(recipe, "Center Pattern");
                CenterPattern(recipe);
                EditorUtility.SetDirty(recipe);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "좌클릭: 아이템 선택  |  우클릭: 슬롯 초기화\n위치 무관 패턴 매칭 적용됨",
                MessageType.Info);
        }

        // ──────────────────────────────────────────────────────────────────
        // Shapeless 그리드
        // ──────────────────────────────────────────────────────────────────

        void DrawShapelessGrid(CraftingRecipeSO recipe)
        {
            EditorGUILayout.LabelField("Shapeless 재료", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 현재 재료 목록 가져오기
            var list = recipe.shapelessIngredients != null
                ? new System.Collections.Generic.List<ItemSO>(recipe.shapelessIngredients)
                : new System.Collections.Generic.List<ItemSO>();

            // 슬롯을 가로로 최대 5개씩 줄 바꿈
            const int perRow = 5;
            int total = list.Count + 1; // 마지막에 + 버튼 슬롯

            float totalW  = Mathf.Min(perRow, total) * (SlotSize + SlotPadding) - SlotPadding;
            float marginL = Mathf.Max((EditorGUIUtility.currentViewWidth - totalW) * 0.5f, 0f);

            bool changed = false;
            int  removeIdx = -1;

            for (int i = 0; i < total; i++)
            {
                if (i % perRow == 0)
                {
                    if (i > 0) EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(marginL);
                }

                if (i < list.Count)
                {
                    // 기존 재료 슬롯
                    DrawSlot(list[i], i + 20000, (item) =>
                    {
                        list[i] = item;
                        changed  = true;
                    }, onRightClick: () =>
                    {
                        removeIdx = i;
                        changed   = true;
                    });
                }
                else
                {
                    // + 추가 슬롯
                    DrawAddSlot(() =>
                    {
                        list.Add(null);
                        changed = true;
                    });
                }

                GUILayout.Space(SlotPadding);
            }

            if (total > 0) EditorGUILayout.EndHorizontal();

            if (removeIdx >= 0)
                list.RemoveAt(removeIdx);

            if (changed)
            {
                Undo.RecordObject(recipe, "Edit Shapeless Ingredients");
                recipe.shapelessIngredients = list.ToArray();
                EditorUtility.SetDirty(recipe);
                Repaint();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "좌클릭: 아이템 선택  |  우클릭: 재료 제거  |  + 슬롯: 재료 추가\n순서 무관 매칭 적용됨",
                MessageType.Info);
        }

        // ──────────────────────────────────────────────────────────────────
        // 슬롯 드로어
        // ──────────────────────────────────────────────────────────────────

        void DrawSlot(ItemSO item, int controlId,
            System.Action<ItemSO> onPick,
            System.Action onRightClick = null)
        {
            // 슬롯 영역 (아이콘 + 이름 텍스트)
            float totalH = SlotSize + 16f;
            Rect  area   = GUILayoutUtility.GetRect(SlotSize, totalH,
                GUILayout.Width(SlotSize), GUILayout.Height(totalH));

            Rect iconRect = new Rect(area.x, area.y, SlotSize, SlotSize);
            Rect nameRect = new Rect(area.x, area.y + SlotSize, SlotSize, 16f);

            // 배경
            GUI.DrawTexture(iconRect, _emptySlotTex);

            // 테두리
            DrawBorder(iconRect, item != null
                ? new Color(0.4f, 0.85f, 0.4f)
                : new Color(0.35f, 0.35f, 0.35f), 1f);

            // 아이콘
            if (item != null)
            {
                var icon = AssetPreview.GetAssetPreview(item)
                        ?? AssetPreview.GetMiniThumbnail(item);

                if (icon != null)
                {
                    GUI.DrawTexture(
                        new Rect(iconRect.x + 4, iconRect.y + 4,
                                 iconRect.width - 8, iconRect.height - 8),
                        icon, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Label(iconRect, item.itemName ?? item.name, CenteredWhiteStyle(8));
                }

                // 아이템 이름 — 슬롯 아래 작은 텍스트
                GUI.Label(nameRect,
                    item.itemName ?? item.name,
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.UpperCenter,
                        wordWrap  = false,
                        clipping  = TextClipping.Clip,
                        normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                    });
            }
            else
            {
                GUI.Label(iconRect, "—",
                    CenteredStyle(new Color(0.35f, 0.35f, 0.35f)));
            }

            // 마우스 이벤트
            if (Event.current.type == EventType.MouseDown &&
                iconRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    EditorGUIUtility.ShowObjectPicker<ItemSO>(item, false, "", controlId);
                    Event.current.Use();
                }
                else if (Event.current.button == 1)
                {
                    onRightClick?.Invoke();
                    Event.current.Use();
                }
            }

            // ObjectPicker 결과 수신
            if (Event.current.commandName == "ObjectSelectorUpdated" &&
                EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                onPick?.Invoke(EditorGUIUtility.GetObjectPickerObject() as ItemSO);
                Repaint();
            }
        }

        void DrawAddSlot(System.Action onClick)
        {
            float totalH = SlotSize + 16f;
            Rect  area   = GUILayoutUtility.GetRect(SlotSize, totalH,
                GUILayout.Width(SlotSize), GUILayout.Height(totalH));

            Rect iconRect = new Rect(area.x, area.y, SlotSize, SlotSize);

            GUI.DrawTexture(iconRect, _emptySlotTex);
            DrawBorder(iconRect, new Color(0.3f, 0.5f, 0.3f), 1f);

            GUI.Label(iconRect, "+", CenteredStyle(new Color(0.4f, 0.8f, 0.4f), 20));

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                iconRect.Contains(Event.current.mousePosition))
            {
                onClick?.Invoke();
                Event.current.Use();
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // 유틸
        // ──────────────────────────────────────────────────────────────────

        void CenterPattern(CraftingRecipeSO recipe)
        {
            int size = recipe.GridSize;
            var grid = recipe.ingredients;

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

            if (maxR < 0) return;

            int patH = maxR - minR + 1;
            int patW = maxC - minC + 1;
            int offR = (size - patH) / 2;
            int offC = (size - patW) / 2;

            var newGrid = new ItemSO[size * size];
            for (int r = 0; r < patH; r++)
            for (int c = 0; c < patW; c++)
                newGrid[(offR + r) * size + (offC + c)] =
                    grid[(minR + r) * size + (minC + c)];

            recipe.ingredients = newGrid;
        }

        static GUIStyle CenteredWhiteStyle(int fontSize = 9) =>
            new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true,
                normal    = { textColor = Color.white }
            };

        static GUIStyle CenteredStyle(Color color, int fontSize = 9) =>
            new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = color }
            };

        static void DrawBorder(Rect rect, Color color, float thickness)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        static Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h);
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
#endif