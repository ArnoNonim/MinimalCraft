#if UNITY_EDITOR
using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEditor;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure.Editor
{
    [CustomEditor(typeof(StructureSO))]
    public class StructureSOEditor : UnityEditor.Editor
    {
        private bool       _showPreview  = true;
        private float      _cellSize     = 20f;
        private int        _previewLayer = 0; // 현재 Y 레이어
        private Vector2    _scrollPos;

        // 블록 색상 매핑
        static readonly Dictionary<BlockType, Color> BlockColors
            = new Dictionary<BlockType, Color>
            {
                { BlockType.Air,    new Color(0,0,0,0)         },
                { BlockType.Grass,  new Color(0.3f, 0.7f, 0.2f)},
                { BlockType.Dirt,   new Color(0.6f, 0.4f, 0.2f)},
                { BlockType.Stone,  new Color(0.5f, 0.5f, 0.5f)},
                { BlockType.Sand,   new Color(0.9f, 0.8f, 0.5f)},
                { BlockType.Snow,   new Color(0.9f, 0.9f, 1.0f)},
                { BlockType.Log,    new Color(0.5f, 0.3f, 0.1f)},
                { BlockType.Leaves, new Color(0.1f, 0.5f, 0.1f)},
                { BlockType.Cactus, new Color(0.2f, 0.6f, 0.2f)},
            };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var so = (StructureSO)target;
            if (so.blocks == null || so.blocks.Count == 0) return;

            GUILayout.Space(10);

            // 프리뷰 토글
            _showPreview = EditorGUILayout.Foldout(
                _showPreview, "구조물 프리뷰", true);

            if (!_showPreview) return;

            // 범위 계산
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (var b in so.blocks)
            {
                minX = Mathf.Min(minX, b.offset.x);
                maxX = Mathf.Max(maxX, b.offset.x);
                minY = Mathf.Min(minY, b.offset.y);
                maxY = Mathf.Max(maxY, b.offset.y);
                minZ = Mathf.Min(minZ, b.offset.z);
                maxZ = Mathf.Max(maxZ, b.offset.z);
            }

            int width  = maxX - minX + 1;
            int height = maxY - minY + 1;
            int depth  = maxZ - minZ + 1;

            // 구조물 정보
            EditorGUILayout.HelpBox(
                $"크기: {width} x {height} x {depth}  /  " +
                $"블록 수: {so.blocks.Count}",
                MessageType.Info);

            // Y 레이어 슬라이더
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Y 레이어: {_previewLayer + minY}",
                GUILayout.Width(100));
            _previewLayer = (int)GUILayout.HorizontalSlider(
                _previewLayer, 0, height - 1);
            GUILayout.EndHorizontal();

            // 셀 크기 슬라이더
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("셀 크기:", GUILayout.Width(60));
            _cellSize = GUILayout.HorizontalSlider(_cellSize, 10f, 40f);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 범례
            DrawLegend(so);

            GUILayout.Space(5);

            // 그리드 프리뷰
            DrawGrid(so, minX, minY, minZ, width, depth);

            GUILayout.Space(5);

            // 버튼
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 레이어 보기"))
                _previewLayer = -1;
            if (GUILayout.Button("가운데 레이어"))
                _previewLayer = height / 2;
            GUILayout.EndHorizontal();

            // 빠른 추가 버튼
            GUILayout.Space(5);
            DrawQuickAdd(so);
        }

        void DrawGrid(
            StructureSO so,
            int minX, int minY, int minZ,
            int width, int depth)
        {
            // 현재 Y 레이어 블록만 필터링
            int targetY = _previewLayer + minY;

            var blockMap = new Dictionary<Vector2Int, BlockType>();
            foreach (var b in so.blocks)
            {
                if (_previewLayer >= 0 && b.offset.y != targetY) continue;
                var key = new Vector2Int(
                    b.offset.x - minX,
                    b.offset.z - minZ);
                blockMap[key] = b.block;
            }

            // 그리드 그리기
            float totalW = width  * _cellSize;
            float totalH = depth  * _cellSize;

            Rect gridRect = GUILayoutUtility.GetRect(
                totalW + 2, totalH + 2);

            // 배경
            EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                var key = new Vector2Int(x, z);
                var cellRect = new Rect(
                    gridRect.x + x * _cellSize + 1,
                    gridRect.y + z * _cellSize + 1,
                    _cellSize - 1,
                    _cellSize - 1);

                // 블록 색상
                Color cellColor = new Color(0.3f, 0.3f, 0.3f);
                BlockType blockType = BlockType.Air;

                if (blockMap.TryGetValue(key, out blockType))
                    BlockColors.TryGetValue(blockType, out cellColor);

                EditorGUI.DrawRect(cellRect, cellColor);

                // 블록 이름 (셀 크기 충분할 때)
                if (_cellSize >= 25 && blockType != BlockType.Air)
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize  = 8,
                        normal    = { textColor = Color.white }
                    };
                    GUI.Label(cellRect,
                        blockType.ToString().Substring(0, 
                            Mathf.Min(3, blockType.ToString().Length)),
                        labelStyle);
                }

                // 원점 표시
                if (x == -minX && z == -minZ)
                {
                    var originStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal    = { textColor = Color.yellow }
                    };
                    GUI.Label(cellRect, "○", originStyle);
                }
            }

            // 클릭으로 블록 추가/제거
            HandleGridClick(gridRect, so, minX, minY, minZ);
        }

        void HandleGridClick(
            Rect gridRect, StructureSO so,
            int minX, int minY, int minZ)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown) return;
            if (!gridRect.Contains(e.mousePosition)) return;

            int x = (int)((e.mousePosition.x - gridRect.x) / _cellSize) + minX;
            int z = (int)((e.mousePosition.y - gridRect.y) / _cellSize) + minZ;
            int y = _previewLayer + minY;

            // 우클릭 — 블록 제거
            if (e.button == 1)
            {
                Undo.RecordObject(so, "Remove Block");
                so.blocks.RemoveAll(b =>
                    b.offset == new Vector3Int(x, y, z));
                EditorUtility.SetDirty(so);
                e.Use();
                return;
            }

            // 좌클릭 — 블록 추가
            if (e.button == 0)
            {
                Undo.RecordObject(so, "Add Block");
                so.blocks.RemoveAll(b =>
                    b.offset == new Vector3Int(x, y, z));
                so.blocks.Add(new StructureSO.StructureBlock
                {
                    offset = new Vector3Int(x, y, z),
                    block  = _selectedBlock
                });
                EditorUtility.SetDirty(so);
                e.Use();
            }

            Repaint();
        }

        private BlockType _selectedBlock = BlockType.Log;

        void DrawLegend(StructureSO so)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("배치 블록:", GUILayout.Width(70));
            _selectedBlock = (BlockType)EditorGUILayout.EnumPopup(
                _selectedBlock, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            // 색상 범례
            GUILayout.BeginHorizontal();
            foreach (var kvp in BlockColors)
            {
                if (kvp.Key == BlockType.Air) continue;
                var rect = GUILayoutUtility.GetRect(
                    12, 12, GUILayout.Width(12));
                EditorGUI.DrawRect(rect, kvp.Value);
                GUILayout.Label(
                    kvp.Key.ToString().Substring(0, 
                        Mathf.Min(3, kvp.Key.ToString().Length)),
                    GUILayout.Width(28));
            }
            GUILayout.EndHorizontal();
        }

        void DrawQuickAdd(StructureSO so)
        {
            EditorGUILayout.LabelField(
                "빠른 템플릿", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("일반 나무"))
            {
                Undo.RecordObject(so, "Add Oak Tree Template");
                AddOakTreeTemplate(so);
                EditorUtility.SetDirty(so);
            }

            if (GUILayout.Button("침엽수"))
            {
                Undo.RecordObject(so, "Add Pine Tree Template");
                AddPineTreeTemplate(so);
                EditorUtility.SetDirty(so);
            }

            if (GUILayout.Button("선인장"))
            {
                Undo.RecordObject(so, "Add Cactus Template");
                AddCactusTemplate(so);
                EditorUtility.SetDirty(so);
            }

            if (GUILayout.Button("전체 초기화"))
            {
                if (EditorUtility.DisplayDialog(
                        "초기화", "모든 블록을 삭제할까요?", "삭제", "취소"))
                {
                    Undo.RecordObject(so, "Clear Structure");
                    so.blocks.Clear();
                    EditorUtility.SetDirty(so);
                }
            }

            GUILayout.EndHorizontal();
        }

        void AddOakTreeTemplate(StructureSO so)
        {
            so.blocks.Clear();

            // 기둥
            for (int i = 0; i < 5; i++)
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(0, i, 0),
                    block  = BlockType.Log });

            // 잎 꼭대기
            so.blocks.Add(new StructureSO.StructureBlock
            { offset = new Vector3Int(0, 5, 0),
                block  = BlockType.Leaves });

            // 잎 위 레이어
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(x, 4, z),
                    block  = BlockType.Leaves });

            // 잎 아래 레이어
            for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
            {
                if (Mathf.Abs(x) == 2 && Mathf.Abs(z) == 2) continue;
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(x, 3, z),
                    block  = BlockType.Leaves });
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(x, 2, z),
                    block  = BlockType.Leaves });
            }
        }

        void AddPineTreeTemplate(StructureSO so)
        {
            so.blocks.Clear();

            for (int i = 0; i < 8; i++)
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(0, i, 0),
                    block  = BlockType.Log });

            so.blocks.Add(new StructureSO.StructureBlock
            { offset = new Vector3Int(0, 8, 0),
                block  = BlockType.Leaves });

            int[] radii = { 1, 1, 2, 2, 3 };
            for (int layer = 0; layer < radii.Length; layer++)
            {
                int r    = radii[layer];
                int leafY = 7 - layer;
                for (int x = -r; x <= r; x++)
                for (int z = -r; z <= r; z++)
                {
                    if (Mathf.Abs(x) == r && Mathf.Abs(z) == r) continue;
                    so.blocks.Add(new StructureSO.StructureBlock
                    { offset = new Vector3Int(x, leafY, z),
                        block  = BlockType.Leaves });
                }
            }
        }

        void AddCactusTemplate(StructureSO so)
        {
            so.blocks.Clear();
            for (int i = 0; i < 3; i++)
                so.blocks.Add(new StructureSO.StructureBlock
                { offset = new Vector3Int(0, i, 0),
                    block  = BlockType.Cactus });
        }
    }
}
#endif