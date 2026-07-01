using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class ItemIconRenderer : MonoBehaviour
    {
        public static ItemIconRenderer Instance;

        [Header("설정")]
        public Camera previewCamera;
        public BlockDataSO blockData;
        public int textureSize = 64;
        public Material toolIconMaterial;
        public Material materialIconMaterial;

        [Header("전체 아이템 목록")]
        [SerializeField] private ItemSO[] allItems;

        private readonly Dictionary<BlockType, RenderTexture> _blockCache = new();
        private readonly Dictionary<int, RenderTexture> _toolCache = new();
        private readonly Dictionary<int, RenderTexture> _materialCache = new();
        private readonly Dictionary<int, RenderTexture> _usableCache = new();
        private readonly Dictionary<int, RenderTexture> _artifactCache = new();

        private GameObject _previewObj;
        private MeshFilter _previewMF;
        private MeshRenderer _previewMR;

        void Awake()
        {
            Instance = this;

            _previewObj = new GameObject("ItemPreviewMesh");
            _previewObj.hideFlags = HideFlags.HideAndDontSave;
            _previewObj.layer = LayerMask.NameToLayer("ItemPreview");

            _previewMF = _previewObj.AddComponent<MeshFilter>();
            _previewMR = _previewObj.AddComponent<MeshRenderer>();

            _previewObj.transform.position = new Vector3(0, -1000, 0);

            previewCamera.transform.position =
                new Vector3(0, -1000, 2f);

            previewCamera.transform.LookAt(
                new Vector3(0, -1000, 0));

            previewCamera.cullingMask =
                1 << _previewObj.layer;

            previewCamera.clearFlags =
                CameraClearFlags.SolidColor;

            previewCamera.backgroundColor =
                new Color(0, 0, 0, 0);

            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.forceIntoRenderTexture = true;
        }

        void Start()
        {
            allItems = Resources.LoadAll<ItemSO>("Item");
            GenerateAllIcons();
        }

        public void GenerateAllIcons()
        {
            foreach (var item in allItems)
            {
                if (item == null)
                    continue;

                GetIconAuto(item);
            }
        }

        public RenderTexture GetIcon(BlockType blockType)
        {
            if (_blockCache.TryGetValue(blockType, out var cached))
                return cached;

            return RenderBlockIcon(blockType);
        }

        public RenderTexture GetToolIcon(ToolItemSO toolItem)
        {
            int key = toolItem.GetInstanceID();

            if (_toolCache.TryGetValue(key, out var cached))
                return cached;

            return RenderToolIcon(toolItem);
        }

        public RenderTexture GetMaterialIcon(MaterialItemSO item)
        {
            int key = item.GetInstanceID();

            if (_materialCache.TryGetValue(key, out var cached))
                return cached;

            return RenderMaterialIcon(item);
        }

        public RenderTexture GetIconAuto(ItemSO item)
        {
            if (item == null) return null;

            if (item is BlockItemSO block)     return GetIcon(block.blockType);
            if (item is ToolItemSO tool)       return GetToolIcon(tool);
            if (item is MaterialItemSO mat)    return GetMaterialIcon(mat);
            if (item is UsableItemSO usable && usable.itemTexture != null)
                return GetUsableIcon(usable);
            if (item is ArtifactItemSO artifact && artifact.handPrefab != null)
                return GetArtifactIcon(artifact);

            return null;
        }
        
        public RenderTexture GetArtifactIcon(ArtifactItemSO item)
        {
            int key = item.GetInstanceID();
            if (_artifactCache.TryGetValue(key, out var cached)) return cached;

            _previewObj.SetActive(false);

            var obj = Instantiate(item.handPrefab);
            obj.transform.position   = new Vector3(0, -1000, 0);
            obj.transform.rotation   = Quaternion.Euler(15f, 200f, 0f);
            obj.transform.localScale = Vector3.one * 0.5f;

            foreach (var t in obj.GetComponentsInChildren<Transform>())
                t.gameObject.layer = LayerMask.NameToLayer("ItemPreview");

            previewCamera.orthographicSize = 0.5f;
            var rt = Render();
            _artifactCache[key] = rt;

            obj.SetActive(false); // ← Destroy 대신 즉시 비활성화
            Destroy(obj);
            _previewObj.SetActive(true);
            return rt;
        }
        
        public RenderTexture GetUsableIcon(UsableItemSO item)
        {
            int key = item.GetInstanceID();
            if (_usableCache.TryGetValue(key, out var cached)) return cached;

            _previewMF.mesh = PixelMeshBuilder.Build(item.itemTexture, item.meshThickness);

            var mat = new Material(materialIconMaterial != null ? materialIconMaterial : toolIconMaterial);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", item.itemTexture);
            else mat.mainTexture = item.itemTexture;
            _previewMR.material = mat;

            _previewObj.transform.rotation   = Quaternion.Euler(15f, 200f, 0f);
            _previewObj.transform.localScale = Vector3.one;
            previewCamera.orthographicSize   = 0.5f;

            var rt = Render();
            _usableCache[key] = rt;
            return rt;
        }

        RenderTexture RenderBlockIcon(BlockType blockType)
        {
            _previewMF.mesh =
                WorldItemMeshBuilder.Build(blockType, blockData);

            _previewMR.material =
                blockData.atlasMaterial;

            _previewObj.transform.rotation =
                Quaternion.Euler(20f, 45f, 0f);

            _previewObj.transform.localScale =
                Vector3.one * 0.7f;

            previewCamera.orthographicSize = 0.7f;

            var rt = Render();

            _blockCache[blockType] = rt;

            return rt;
        }

        RenderTexture RenderToolIcon(ToolItemSO toolItem)
        {
            _previewMF.mesh =
                PixelMeshBuilder.Build(
                    toolItem.toolTexture,
                    toolItem.meshThickness);

            var mat = new Material(toolIconMaterial);

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", toolItem.toolTexture);
            else
                mat.mainTexture = toolItem.toolTexture;

            _previewMR.material = mat;

            _previewObj.transform.rotation =
                Quaternion.Euler(0f, 180f, 0f);

            _previewObj.transform.localScale =
                Vector3.one;

            previewCamera.orthographicSize = 0.5f;

            var rt = Render();

            _toolCache[toolItem.GetInstanceID()] = rt;

            return rt;
        }

        RenderTexture RenderMaterialIcon(MaterialItemSO item)
        {
            if (item.itemTexture == null)
                return null;

            _previewMF.mesh =
                PixelMeshBuilder.Build(
                    item.itemTexture,
                    item.meshThickness);

            var mat = new Material(
                materialIconMaterial != null
                    ? materialIconMaterial
                    : toolIconMaterial);

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", item.itemTexture);
            else
                mat.mainTexture = item.itemTexture;

            _previewMR.material = mat;

            _previewObj.transform.rotation =
                Quaternion.Euler(15f, 200f, 0f);

            _previewObj.transform.localScale =
                Vector3.one;

            previewCamera.orthographicSize = 0.5f;

            var rt = Render();

            _materialCache[item.GetInstanceID()] = rt;

            return rt;
        }

        RenderTexture Render()
        {
            RenderTexture rt = new RenderTexture(
                textureSize,
                textureSize,
                24,
                RenderTextureFormat.ARGB32);

            rt.name = "ItemIconRT";
            rt.filterMode = FilterMode.Point;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.Create();

            var prevRT = RenderTexture.active;

            RenderTexture.active = rt;

            GL.Clear(
                true,
                true,
                new Color(0, 0, 0, 0));

            RenderTexture.active = prevRT;

            previewCamera.targetTexture = rt;
            previewCamera.Render();
            previewCamera.targetTexture = null;

            return rt;
        }

        void OnDestroy()
        {
            foreach (var rt in _blockCache.Values)
                if (rt != null)
                    rt.Release();

            foreach (var rt in _toolCache.Values)
                if (rt != null)
                    rt.Release();

            foreach (var rt in _materialCache.Values)
                if (rt != null)
                    rt.Release();
            
            foreach (var rt in _usableCache.Values)
                if (rt != null)
                    rt.Release();
            
            foreach (var rt in _artifactCache.Values)
                if (rt != null)
                    rt.Release();
        }
    }
}