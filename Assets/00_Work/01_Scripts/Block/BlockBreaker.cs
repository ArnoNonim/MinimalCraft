using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.Player;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Save;
using _00_Work._01_Scripts.Tool;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    public class BlockBreaker : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ArtifactBreaker artifactBreaker;
        public PlayerInputSO    playerInput;
        public ChunkManager     chunkManager;
        public BlockHighlighter highlighter;
        public BlockDataSO      blockData;
        public ParticleSystem   breakParticlePrefab;

        [Header("현재 도구")]
        public ToolDataSO currentTool;

        [Header("설정")]
        public float baseBreakTime = 1f;

        [Header("드롭")]
        public ItemDropper itemDropper;
        
        [Header("나뭇잎 드롭")]
        [SerializeField] private ItemSO leafDropItem;
        [Range(0f, 1f)]
        [SerializeField] private float  leafDropChance = 0.3f;

        private bool       _isHoldingBreak;
        private bool       _isMiningStarted;
        private Vector3    _targetBlockPos;
        private Vector3Int _targetBlockCell;
        private byte       _savedBlockType;
        private float      _currentHardness;
        private float      _breakProgress;

        public float BreakProgress   => _breakProgress;
        public bool  IsMiningStarted => _isMiningStarted;
        public bool  IsBreaking      => _isMiningStarted;
        public Vector3 TargetBlockPos => _targetBlockPos;
        public byte TargetBlockType => _savedBlockType;

        void OnEnable()
        {
            playerInput.OnAttackKeyDown += OnBreakStart;
            playerInput.OnAttackKeyUp   += OnBreakStop;
        }

        void OnDisable()
        {
            playerInput.OnAttackKeyDown -= OnBreakStart;
            playerInput.OnAttackKeyUp   -= OnBreakStop;
        }

        void Update()
        {
            if (!_isHoldingBreak)
            {
                if (_isMiningStarted) CancelBreaking();
                return;
            }

            if (!highlighter.TryGetTargetBlock(out Vector3 blockPos))
            {
                if (_isMiningStarted) CancelBreaking();
                return;
            }

            Vector3Int currentCell = Vector3Int.FloorToInt(blockPos);

            if (_isMiningStarted && currentCell != _targetBlockCell)
                CancelBreaking();

            if (!_isMiningStarted)
                StartBreaking(blockPos);

            if (_isMiningStarted)
                ProgressBreaking();
        }

        void StartBreaking(Vector3 blockPos)
        {
            byte blockType = chunkManager.GetBlockAt(blockPos);
            if (blockType == (byte)BlockType.Air) return;

            // 도구 레벨 체크 — 레벨 부족하면 시작 안 함
            var blockInfo    = blockData.blocks[blockType];
            int currentLevel = currentTool != null ? currentTool.toolLevel : 0;
            if (currentLevel < blockInfo.requiredToolLevel) return;

            _targetBlockPos  = blockPos;
            _targetBlockCell = Vector3Int.FloorToInt(blockPos);
            _isMiningStarted = true;
            _breakProgress   = 0f;
            _savedBlockType  = blockType;
            _currentHardness = blockInfo.hardness;
        }

        void ProgressBreaking()
        {
            float toolSpeed = 1f;

            if (_savedBlockType < blockData.blocks.Length)
            {
                var blockInfo = blockData.blocks[_savedBlockType];

                // 도구 레벨 부족 → 채굴 불가 (진행도 올라가지 않음)
                int requiredLevel = blockInfo.requiredToolLevel;
                int currentLevel  = currentTool != null ? currentTool.toolLevel : 0;

                if (currentLevel < requiredLevel)
                    return; // ← 진행도 증가 안 함

                if (currentTool != null)
                {
                    if (blockInfo.requiredTool == ToolType.None ||
                        blockInfo.requiredTool == currentTool.toolType)
                    {
                        toolSpeed = currentTool.miningSpeed;
                    }
                }
            }

            float blockSpeedMul = _savedBlockType < blockData.blocks.Length
                ? blockData.blocks[_savedBlockType].miningSpeedMul : 1f;

            float breakSpeed = toolSpeed * blockSpeedMul / _currentHardness;

            _breakProgress += Time.deltaTime * breakSpeed / baseBreakTime;
            _breakProgress  = Mathf.Clamp01(_breakProgress);

            if (_breakProgress >= 1f)
                FinishBreaking();
        }

        void FinishBreaking()
        {
            SpawnBreakParticle(_savedBlockType);

            Vector3 dropPos = new Vector3(
                Mathf.Floor(_targetBlockPos.x) + 0.5f,
                Mathf.Floor(_targetBlockPos.y) + 0.5f,
                Mathf.Floor(_targetBlockPos.z) + 0.5f);

            var blockType = (BlockType)_savedBlockType;

            if (blockType == BlockType.Furnace)
            {
                var pos = Vector3Int.FloorToInt(_targetBlockPos);
                FurnaceManager.Instance?.Remove(pos, itemDropper, dropPos);
            }

            // ← 나뭇잎 확률 드롭
            if (blockType == BlockType.Leaves && leafDropItem != null)
            {
                if (Random.value <= leafDropChance)
                    itemDropper.DropItem(leafDropItem, 1, dropPos);
            }
            else
            {
                itemDropper.DropBlock(blockType, dropPos);
            }

            chunkManager.SetBlock(_targetBlockPos, BlockType.Air);
            ResetState();
            ReduceDurability();
        }

        private void ReduceDurability()
        {
            if (currentTool == null) { return; }

            var hotbar = UIHotbar.Instance;
            var stack  = hotbar?.GetSelectedItem();
    
            if (stack == null || !stack.HasDurability) return;

            stack.ReduceDurability();

            if (stack.IsBroken)
            {
                hotbar.inventory
                    .slots[Inventory.InventorySize + hotbar.SelectedIndex]
                    .Clear();
                hotbar.inventory.NotifyChanged();
                currentTool = null;
            }
            else
            {
                hotbar.inventory.NotifyChanged();
            }
        }

        void CancelBreaking()
        {
            ResetState();
        }

        void ResetState()
        {
            _isMiningStarted = false;
            _breakProgress   = 0f;
            _savedBlockType  = 0;
        }

        void SpawnBreakParticle(byte blockType)
        {
            if (breakParticlePrefab == null) return;
            if (blockType >= blockData.blocks.Length) return;

            Vector3 spawnPos = new Vector3(
                Mathf.Floor(_targetBlockPos.x) + 0.5f,
                Mathf.Floor(_targetBlockPos.y) + 0.5f,
                Mathf.Floor(_targetBlockPos.z) + 0.5f);

            Color blockColor = GetBlockAverageColor(blockType);

            var ps = Instantiate(
                breakParticlePrefab, spawnPos, Quaternion.identity);

            ApplyColorToParticle(ps, blockColor);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + 0.5f);
        }

        Color GetBlockAverageColor(byte blockType)
        {
            Vector2[] uvs   = blockData.GetUVs((BlockType)blockType, 0);
            Texture2D atlas = blockData.atlas;
            if (atlas == null) return Color.white;

            float centerU = (uvs[0].x + uvs[2].x) * 0.5f;
            float centerV = (uvs[0].y + uvs[2].y) * 0.5f;

            int   sampleCount = 9;
            Color sum         = Color.black;
            float tileSize    = 1f / blockData.atlasSize;

            for (int i = 0; i < sampleCount; i++)
            {
                float u = centerU + Random.Range(-tileSize * 0.4f, tileSize * 0.4f);
                float v = centerV + Random.Range(-tileSize * 0.4f, tileSize * 0.4f);

                int px = Mathf.Clamp(Mathf.FloorToInt(u * atlas.width),  0, atlas.width  - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt(v * atlas.height), 0, atlas.height - 1);

                sum += atlas.GetPixel(px, py);
            }

            return sum / sampleCount;
        }

        void ApplyColorToParticle(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startColor = color;

            var col     = ps.colorOverLifetime;
            col.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        void OnBreakStart()
        {
            if (artifactBreaker != null && artifactBreaker.IsTargetingArtifact) return;
            _isHoldingBreak = true;
        }

        void OnBreakStop()
        {
            _isHoldingBreak = false;
            if (_isMiningStarted) CancelBreaking();
        }
    }
}