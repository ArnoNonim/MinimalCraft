using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class HandItemRenderer : MonoBehaviour
    {
        [Header("참조")]
        public UIHotbar    hotbar;
        public BlockDataSO blockData;
        public Transform   rightHandBone;
        public Material    toolMaterial;
        public Material    materialItemMaterial; // 일반 아이템용 머터리얼

        [Header("블록 아이템 설정")]
        public Vector3 blockItemOffset   = new Vector3(0.05f, -0.02f, 0.1f);
        public Vector3 blockItemRotation = new Vector3(0f, 0f, 0f);
        public float   blockItemScale    = 0.3f;

        [Header("도구 아이템 설정")]
        public Vector3 toolItemOffset   = new Vector3(0.05f, -0.02f, 0.1f);
        public Vector3 toolItemRotation = new Vector3(0f, 0f, 0f);
        public float   toolItemScale    = 1.0f;

        [Header("일반 아이템 설정")]
        public Vector3 materialItemOffset   = new Vector3(0.05f, -0.02f, 0.1f);
        public Vector3 materialItemRotation = new Vector3(15f, 200f, 0f);
        public float   materialItemScale    = 0.8f;
        
        [Header("사용 아이템 설정")]
        public Vector3 usableItemOffset   = new Vector3(0.05f, -0.02f, 0.1f);
        public Vector3 usableItemRotation = new Vector3(15f, 200f, 0f);
        public float   usableItemScale    = 0.8f;

        private GameObject _currentItemObj;
        private int        _lastSlotIndex = -1;
        private ItemSO _lastItem;   

        // ──────────────────────────────────────────────
        // Unity 이벤트
        // ──────────────────────────────────────────────

        void Update()
        {
            var currentStack = hotbar.GetSelectedItem();
            if (!HasChanged(currentStack)) return;

            _lastItem      = currentStack?.item;
            _lastSlotIndex = hotbar.SelectedIndex;

            UpdateHandItem(currentStack);
        }

        void OnDestroy()
        {
            if (_currentItemObj != null)
                Destroy(_currentItemObj);
        }

        // ──────────────────────────────────────────────
        // 내부 로직
        // ──────────────────────────────────────────────

        bool HasChanged(ItemStack current)
        {
            if (_lastSlotIndex != hotbar.SelectedIndex) return true;
            return current?.item != _lastItem;
        }

        void UpdateHandItem(ItemStack stack)
        {
            if (_currentItemObj != null)
                Destroy(_currentItemObj);

            if (stack == null || stack.IsEmpty) return;

            _currentItemObj = new GameObject("HandItem");
            _currentItemObj.transform.SetParent(rightHandBone, false);
            
            SetLayerRecursive(_currentItemObj, LayerMask.NameToLayer("HoldingItem"));

            var mf = _currentItemObj.AddComponent<MeshFilter>();
            var mr = _currentItemObj.AddComponent<MeshRenderer>();

            if (stack.item is BlockItemSO blockItem)
            {
                SetTransform(blockItemOffset, blockItemRotation, blockItemScale);
                mf.mesh     = WorldItemMeshBuilder.Build(blockItem.blockType, blockData);
                mr.material = blockData.atlasMaterial;
            }
            else if (stack.item is ToolItemSO toolItem && toolItem.toolTexture != null)
            {
                SetTransform(toolItemOffset, toolItemRotation, toolItemScale);
                mf.mesh     = PixelMeshBuilder.Build(toolItem.toolTexture, toolItem.meshThickness);
                mr.material = CreateTexturedMaterial(toolMaterial, toolItem.toolTexture);
            }
            else if (stack.item is MaterialItemSO matItem && matItem.itemTexture != null)
            {
                SetTransform(materialItemOffset, materialItemRotation, materialItemScale);
                mf.mesh     = PixelMeshBuilder.Build(matItem.itemTexture, matItem.meshThickness);
                mr.material = CreateTexturedMaterial(materialItemMaterial, matItem.itemTexture);
            }
            else if (stack.item is UsableItemSO usable && usable.itemTexture != null)
            {
                SetTransform(usableItemOffset, usableItemRotation, usableItemScale);
                mf.mesh     = PixelMeshBuilder.Build(usable.itemTexture, usable.meshThickness);
                mr.material = CreateTexturedMaterial(materialItemMaterial, usable.itemTexture);
            }
            else if (stack.item is ArtifactItemSO artifact && artifact.handPrefab != null)
            {
                var obj = Instantiate(artifact.handPrefab, _currentItemObj.transform);
                obj.transform.localPosition = materialItemOffset;
                obj.transform.localEulerAngles = materialItemRotation;
                obj.transform.localScale = Vector3.one * 0.15f; // 크기 조절
                // MeshFilter/MeshRenderer 직접 안 붙이고 프리팹 그대로
                Destroy(_currentItemObj.GetComponent<MeshFilter>());
                Destroy(_currentItemObj.GetComponent<MeshRenderer>());
            }
        }
        
        void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>손에 든 아이템 트랜스폼 일괄 설정</summary>
        void SetTransform(Vector3 offset, Vector3 rotation, float scale)
        {
            _currentItemObj.transform.localPosition    = offset;
            _currentItemObj.transform.localEulerAngles = rotation;
            _currentItemObj.transform.localScale       = Vector3.one * scale;
        }

        /// <summary>
        /// 베이스 머터리얼을 복제하고 텍스처를 적용한 새 머터리얼 반환
        /// _BaseMap(URP) → mainTexture 순서로 시도
        /// </summary>
        Material CreateTexturedMaterial(Material baseMat, Texture2D texture)
        {
            var mat = new Material(baseMat);

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
            else
                mat.mainTexture = texture;

            return mat;
        }
    }
}