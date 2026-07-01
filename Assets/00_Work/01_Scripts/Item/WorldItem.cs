using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class WorldItem : MonoBehaviour
    {
        [Header("데이터")]
        public ItemSO item;
        public int    count = 1;

        [Header("설정")]
        public float pickupDelay       = 0.5f;
        public float moveSpeed         = 8f;
        public float itemScale         = 0.2f;
        public float materialItemScale = 1f;
        public float attractRadius     = 4f;
        public float pickupRadius      = 1.2f;

        [Header("자동 소멸")]
        public float lifetime          = 180f; // 3분
        public float dissolveStartTime = 10f;  // 소멸 몇 초 전부터 디졸브 시작
        public Material dissolveMaterial;      // WorldItemDissolve 머티리얼

        [Header("머티리얼")]
        public Material itemMaterial;

        [Header("레이어")]
        public LayerMask playerLayer;

        private float     _spawnTime;
        private Transform _targetPlayer;
        private Rigidbody _rb;
        private bool      _isAttracting;
        private bool      _isDissolving;
        private Renderer  _renderer;
        private Material  _dissolveMat;

        static readonly int DissolveAmountProp = Shader.PropertyToID("_DissolveAmount");

        void Awake()
        {
            _rb        = GetComponent<Rigidbody>();
            _spawnTime = Time.time;
            _renderer  = GetComponent<Renderer>();

            if (_rb != null)
            {
                _rb.mass                   = 0.5f;
                _rb.linearDamping          = 0.3f;
                _rb.angularDamping         = 0.5f;
                _rb.interpolation          = RigidbodyInterpolation.Interpolate;
                _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rb.freezeRotation         = false;
                _rb.useGravity             = true;
            }
        }

        public void Initialize(ItemSO itemData, BlockDataSO blockData, int itemCount = 1)
        {
            item  = itemData;
            count = itemCount;

            MeshFilter   mf = GetComponent<MeshFilter>()   ?? gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
            _renderer = mr;

            if (itemData is BlockItemSO blockItem)
            {
                mf.mesh     = WorldItemMeshBuilder.Build(blockItem.blockType, blockData);
                mr.material = blockData.atlasMaterial;
                transform.localScale = Vector3.one * itemScale;
            }
            else if (itemData is ToolItemSO toolItem && toolItem.toolTexture != null)
            {
                mf.mesh     = PixelMeshBuilder.Build(toolItem.toolTexture, toolItem.meshThickness);
                mr.material = CreateTexturedMaterial(toolItem.toolTexture);
                transform.localScale = Vector3.one * materialItemScale;
            }
            else if (itemData is MaterialItemSO matItem && matItem.itemTexture != null)
            {
                mf.mesh     = PixelMeshBuilder.Build(matItem.itemTexture, matItem.meshThickness);
                mr.material = CreateTexturedMaterial(matItem.itemTexture);
                transform.localScale = Vector3.one * materialItemScale;
            }
            else if (itemData is UsableItemSO usable && usable.itemTexture != null)
            {
                mf.mesh     = PixelMeshBuilder.Build(usable.itemTexture, usable.meshThickness);
                mr.material = CreateTexturedMaterial(usable.itemTexture);
                transform.localScale = Vector3.one * materialItemScale;
            }
            else if (itemData is ArtifactItemSO artifact && artifact.handPrefab != null)
            {
                // 기존 MeshFilter/MeshRenderer 안 쓰고 프리팹 자식으로 붙이기
                Destroy(mf);
                Destroy(mr);
    
                var obj = Instantiate(artifact.handPrefab, transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale    = Vector3.one;
    
                transform.localScale = Vector3.one * itemScale;
    
                // 콜라이더는 SphereCollider로 대체
                foreach (var col in GetComponents<Collider>()) Destroy(col);
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.3f;
    
                if (_rb != null) _rb.isKinematic = false;
                return; // 아래 MeshCollider 추가 로직 스킵
            }

            // MeshCollider
            foreach (var col in GetComponents<Collider>())
                Destroy(col);

            var mc        = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.mesh;
            mc.convex     = true;

            if (_rb != null) _rb.isKinematic = false;
        }

        Material CreateTexturedMaterial(Texture2D texture)
        {
            var mat = new Material(itemMaterial);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
            else
                mat.mainTexture = texture;
            return mat;
        }

        void Update()
        {
            TickLifetime();

            if (!CanPickup) return;

            var cols = Physics.OverlapSphere(
                transform.position, attractRadius, playerLayer);

            if (cols.Length == 0)
            {
                if (_isAttracting)
                {
                    _isAttracting = false;
                    _targetPlayer = null;
                    if (_rb != null) _rb.isKinematic = false;
                }
                return;
            }

            Transform player = cols[0].transform;
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= attractRadius && !_isAttracting)
            {
                _isAttracting = true;
                _targetPlayer = player;
                if (_rb != null) _rb.isKinematic = true;
            }

            if (_isAttracting && _targetPlayer != null)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _targetPlayer.position,
                    moveSpeed * Time.deltaTime);

                if (dist <= pickupRadius)
                {
                    var pickup = player.GetComponentInChildren<ItemPickup>();
                    if (pickup != null && pickup.TryPickup(this))
                        Destroy(gameObject);
                }
            }
        }

        // ──────────────────────────────────────────────
        // 자동 소멸
        // ──────────────────────────────────────────────

        void TickLifetime()
        {
            float age      = Time.time - _spawnTime;
            float timeLeft = lifetime - age;

            // 소멸 시작
            if (timeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 디졸브 시작 시점
            if (!_isDissolving && timeLeft <= dissolveStartTime)
            {
                StartDissolve();
            }

            // 디졸브 진행
            if (_isDissolving && _dissolveMat != null)
            {
                float t = 1f - (timeLeft / dissolveStartTime);
                _dissolveMat.SetFloat(DissolveAmountProp, Mathf.Clamp01(t));
            }
        }

        void StartDissolve()
        {
            _isDissolving = true;

            if (dissolveMaterial == null || _renderer == null) return;

            // 디졸브 머티리얼로 교체 (기존 텍스처 유지)
            _dissolveMat = new Material(dissolveMaterial);

            // 기존 텍스처 복사
            var origMat = _renderer.material;
            if (origMat.HasProperty("_BaseMap"))
                _dissolveMat.SetTexture("_BaseMap", origMat.GetTexture("_BaseMap"));
            else if (origMat.mainTexture != null)
                _dissolveMat.mainTexture = origMat.mainTexture;

            _renderer.material = _dissolveMat;
        }

        public bool CanPickup => Time.time - _spawnTime >= pickupDelay;
    }
}