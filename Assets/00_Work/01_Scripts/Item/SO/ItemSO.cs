using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "Item", menuName = "Item/Item", order = 0)]
    public class ItemSO : ScriptableObject
    {
        public string   itemName;
        [TextArea(4, 10)]
        public string   description;
        public int      maxStack = 64;
        public ItemType itemType;
        
        [Header("연료 설정")]
        public bool  isFuel   = false;
        [Tooltip("연료로 사용 시 제련 가능 시간 (초)")]
        public float burnTime = 0f;

        // ── 저장 경로 ────────────────────────────────────────────────
        [Header("저장 경로 (빌드 직렬화용)")]
        [SerializeField] private string _resourcePath;

        /// <summary>
        /// Resources 폴더 기준 상대경로 (확장자 없음)
        /// 예) "Items/Tools/WoodPickaxe"
        /// Inspector 우클릭 → '경로 자동 세팅'으로 한 번만 채워주면 됨
        /// </summary>
        public string ResourcePath => _resourcePath;

        #if UNITY_EDITOR
        // Inspector에서 ItemSO 우클릭 → '경로 자동 세팅' 선택 시 실행
        [UnityEditor.MenuItem("CONTEXT/ItemSO/경로 자동 세팅")]
        private static void AutoSetResourcePath(UnityEditor.MenuCommand cmd)
        {
            var so   = cmd.context as ItemSO;
            string path = UnityEditor.AssetDatabase.GetAssetPath(so);

            int idx = path.IndexOf("Resources/");
            if (idx < 0)
            {
                UnityEngine.Debug.LogError(
                    $"[ItemSO] '{so.name}' 이 Resources 폴더 밖에 있음!\n{path}");
                return;
            }

            path = path.Substring(idx + "Resources/".Length);

            int ext = path.LastIndexOf('.');
            if (ext >= 0) path = path.Substring(0, ext);

            so._resourcePath = path;
            UnityEditor.EditorUtility.SetDirty(so);
            UnityEngine.Debug.Log($"[ItemSO] '{so.name}' 경로 세팅 완료: {path}");
        }

        // 프로젝트 내 모든 ItemSO 경로를 한 번에 세팅하는 툴 메뉴
        [UnityEditor.MenuItem("Tools/MinimalCraft/모든 ItemSO 경로 자동 세팅")]
        private static void AutoSetAllResourcePaths()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO");
            int count = 0;

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(assetPath);
                if (so == null) continue;

                int idx = assetPath.IndexOf("Resources/");
                if (idx < 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ItemSO] Resources 폴더 밖 스킵: {assetPath}");
                    continue;
                }

                string rel = assetPath.Substring(idx + "Resources/".Length);
                int ext = rel.LastIndexOf('.');
                if (ext >= 0) rel = rel.Substring(0, ext);

                so._resourcePath = rel;
                UnityEditor.EditorUtility.SetDirty(so);
                count++;
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[ItemSO] 경로 세팅 완료: {count}개");
        }
        #endif
    }

    public enum ItemType
    {
        Block,
        Tool,
        Material,
    }
}