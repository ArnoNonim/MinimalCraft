using System;
using System.Collections;
using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Biome;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.ChunkSystem.Terrain;
using _00_Work._01_Scripts.Save;
using _00_Work._01_Scripts.Sound;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public class ChunkManager : MonoBehaviour
    {
        
        public static ChunkManager Instance;
        
        [Header("청크 애니메이션")]
        public float chunkSpawnOffsetY = -20f;
        public float chunkAnimDuration =  0.4f;

        [Header("구조물")]
        public List<StructurePlacementSO> structurePlacements;

        public string            chunkLayer;
        public Transform         player;
        public int               renderDistance = 4;
        public BlockDataSO       blockData;
        public TerrainSettingsSO terrainSettings;
        
        public event Action OnChunksLoaded;

        [Header("스폰 설정")]
        [Tooltip("플레이어가 스폰될 최대 반경 (블록 단위)")]
        public int spawnRadius = 32;

        private readonly Dictionary<Vector2Int, ChunkData>  _chunkDataMap
            = new Dictionary<Vector2Int, ChunkData>();
        private readonly Dictionary<Vector2Int, GameObject> _chunkObjects
            = new Dictionary<Vector2Int, GameObject>();

        private Vector2Int _lastPlayerChunk = new Vector2Int(int.MaxValue, 0);

        // 월드 y 범위 상수
        private static readonly int WorldYMin = -ChunkData.YOffset;
        private static readonly int WorldYMax =  ChunkData.Height - ChunkData.YOffset - 1;

        public Dictionary<Vector2Int, ChunkData> LoadedChunks => _chunkDataMap;

        // ──────────────────────────────────────────────
        // 초기화
        // ──────────────────────────────────────────────

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            
            ChunkSerializer.SaveDirectory = System.IO.Path.Combine(
                Application.persistentDataPath, "Chunks");
        }

        void Start()
        {
            if (terrainSettings.randomSeed)
                terrainSettings.seed = Random.Range(0, int.MaxValue);

            ChunkGenerator.Settings                = terrainSettings;
            BiomeGenerator.Settings                = terrainSettings;
            StructureGenerator.Settings            = terrainSettings;
            StructureGenerator.StructurePlacements = structurePlacements;

            Time.timeScale = 0f;
            StartCoroutine(InitialLoad());
        }

        // ──────────────────────────────────────────────
        // 업데이트
        // ──────────────────────────────────────────────

        void Update()
        {
            if (Time.timeScale == 0f) return;

            Vector2Int currentChunk = GetChunkPos(player.position);
            if (currentChunk == _lastPlayerChunk) return;
            _lastPlayerChunk = currentChunk;

            StopAllCoroutines();
            StartCoroutine(UpdateChunks(currentChunk));
        }
        
        public void ForceUpdateChunks()
        {
            StopAllCoroutines();
            _lastPlayerChunk = new Vector2Int(int.MaxValue, 0); // 강제 리셋
            StartCoroutine(UpdateChunks(GetChunkPos(player.position)));
        }

        // ──────────────────────────────────────────────
        // 초기 로딩
        // ──────────────────────────────────────────────

        IEnumerator InitialLoad()
        {
            var rb = player.GetComponent<Rigidbody>()
                     ?? player.GetComponentInChildren<Rigidbody>();

            bool wasKinematic = false;
            RigidbodyConstraints originalConstraints = RigidbodyConstraints.None;

            if (rb != null)
            {
                wasKinematic        = rb.isKinematic;
                originalConstraints = rb.constraints;

                // ★ velocity 클리어는 반드시 kinematic 전환 전에
                if (!rb.isKinematic)
                {
                    rb.linearVelocity  = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            // ── 1단계: 스폰 위치 결정 ─────────────────────────────────────
            bool    hasSave   = SaveManager.Instance != null && SaveManager.Instance.HasSave();
            Vector3 targetPos;

            if (hasSave)
                targetPos = SaveManager.Instance.PeekPosition();
            else if (WorldSaveManager.Instance != null && WorldSaveManager.Instance.HasSpawnPoint)
                targetPos = WorldSaveManager.Instance.SpawnPoint;
            else
                targetPos = Vector3.zero;

            player.position  = targetPos;
            _lastPlayerChunk = GetChunkPos(targetPos);

            // ── 2단계: 청크 로딩 ──────────────────────────────────────────
            yield return StartCoroutine(UpdateChunks(_lastPlayerChunk));

            // ── 3단계: 최종 위치 확정 ─────────────────────────────────────
            if (hasSave)
            {
                SaveManager.Instance.LoadPositionOnly();
                SaveManager.Instance.LoadStatsAndInventory();
            }
            else if (WorldSaveManager.Instance != null && WorldSaveManager.Instance.HasSpawnPoint)
            {
                player.position = WorldSaveManager.Instance.SpawnPoint;
            }
            else
            {
                Vector3 safePos = FindSafeSpawnPosition();
                player.position = safePos;
                WorldSaveManager.Instance?.SetSpawnPointOnce(safePos);
            }

            yield return new WaitForSecondsRealtime(0.1f); // Fixed 루프 무관하게 실시간 대기
            Physics.SyncTransforms();

            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
                rb.constraints = originalConstraints;
            }

            Time.timeScale = 1f;
            LoadingScreen.Instance?.Hide();
            SoundManager.Instance.PlaySFXAt("Teleport_End", player.transform, 0.3f);
        }

        /// <summary>
        /// spawnRadius 내 랜덤 위치에서 물 위가 아닌 안전한 지점을 반환
        /// 모든 y 값은 월드 좌표 기준 (-YOffset ~ WorldYMax)
        /// </summary>
        public Vector3 FindSafeSpawnPosition()
        {
            const int maxTries = 100;

            // 로드된 청크 기준으로 탐색 범위 계산
            int safeRadius = (renderDistance - 1) * ChunkData.Width;

            for (int i = 0; i < maxTries; i++)
            {
                int randX = Random.Range(-safeRadius, safeRadius + 1);
                int randZ = Random.Range(-safeRadius, safeRadius + 1);
                
                Vector2Int chunkPos = GetChunkPos(new Vector3(randX, 0, randZ));
                if (!_chunkDataMap.TryGetValue(chunkPos, out ChunkData chunk)) continue;

                int localX = randX - chunkPos.x * ChunkData.Width;
                int localZ = randZ - chunkPos.y * ChunkData.Width;

                // 가장 높은 비공기 블록 탐색 (월드 y 기준 위→아래)
                int surfaceY = int.MinValue;
                for (int y = WorldYMax; y >= WorldYMin; y--)
                {
                    byte b = chunk.GetBlock(localX, y, localZ);
                    if (b != (byte)BlockType.Air)
                    {
                        surfaceY = y;
                        break;
                    }
                }

                if (surfaceY == int.MinValue) continue;

                // 표면이 물이면 스킵
                byte surfaceBlock = chunk.GetBlock(localX, surfaceY, localZ);
                if (surfaceBlock == (byte)BlockType.Water) continue;

                // 발·머리 공간 공기 확인
                int feetY = Mathf.Min(surfaceY + 1, WorldYMax);
                int headY = Mathf.Min(surfaceY + 2, WorldYMax);

                if (chunk.GetBlock(localX, feetY, localZ) != (byte)BlockType.Air) continue;
                if (chunk.GetBlock(localX, headY, localZ) != (byte)BlockType.Air) continue;

                return new Vector3(randX, surfaceY + 1f, randZ);
            }

            Debug.LogWarning("[ChunkManager] 안전한 스폰 위치를 찾지 못해 원점 근처로 스폰");
            return new Vector3(0, GetFallbackHeight(0, 0), 0);
        }

        int GetFallbackHeight(int worldX, int worldZ)
        {
            Vector2Int chunkPos = GetChunkPos(new Vector3(worldX, 0, worldZ));
            if (!_chunkDataMap.TryGetValue(chunkPos, out ChunkData chunk))
                return 64;

            int localX = worldX - chunkPos.x * ChunkData.Width;
            int localZ = worldZ - chunkPos.y * ChunkData.Width;

            for (int y = WorldYMax; y >= WorldYMin; y--)
                if (chunk.GetBlock(localX, y, localZ) != (byte)BlockType.Air)
                    return y + 1;

            return 64;
        }
        
        public bool IsChunkLoaded(Vector3 worldPos)
        {
            Vector2Int pos = GetChunkPos(worldPos);
            return _chunkObjects.ContainsKey(pos);
        }

        // ──────────────────────────────────────────────
        // 청크 업데이트
        // ──────────────────────────────────────────────

        IEnumerator UpdateChunks(Vector2Int center)
        {
            // ── 1단계: 데이터 생성 (백그라운드) ──────────────────────────────
            var chunksToGenerate = new List<Vector2Int>();

            for (int cx = -renderDistance; cx <= renderDistance; cx++)
            for (int cz = -renderDistance; cz <= renderDistance; cz++)
            {
                if (cx * cx + cz * cz > renderDistance * renderDistance) continue;
                var pos = new Vector2Int(center.x + cx, center.y + cz);
                if (!_chunkDataMap.ContainsKey(pos))
                    chunksToGenerate.Add(pos);
            }

            var task = System.Threading.Tasks.Task.Run(() =>
            {
                foreach (var pos in chunksToGenerate)
                {
                    ChunkData data;
                    if (!ChunkSerializer.TryLoad(pos, out data))
                    {
                        data = new ChunkData();
                        ChunkGenerator.Generate(data, pos);
                    }
                    lock (_chunkDataMap)
                        _chunkDataMap[pos] = data;
                }

                foreach (var pos in chunksToGenerate)
                {
                    if (ChunkSerializer.Exists(pos)) continue;
                    lock (_chunkDataMap)
                    {
                        StructureGenerator.Settings = terrainSettings;
                        StructureGenerator.PlaceStructures(
                            _chunkDataMap[pos], pos, _chunkDataMap);
                        
                        DeepCityGenerator.Generate(
                            _chunkDataMap[pos], pos,
                            terrainSettings.seed,
                            _chunkDataMap);
                    }
                }
            });

            // 데이터 생성 중 진척도 0 → 0.5 표시
            float dataProgress = 0f;
            while (!task.IsCompleted)
            {
                dataProgress = Mathf.Min(dataProgress + Time.unscaledDeltaTime * 0.3f, 0.48f);
                LoadingScreen.Instance?.SetProgress(dataProgress);
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"청크 생성 오류: {task.Exception}");
                yield break;
            }

            // ── 2단계: 메시 생성 + newlyCreated 추적 ─────────────────────────
            var newlyCreated = new HashSet<Vector2Int>();
            int totalToCreate = 0;
            int created = 0;

// 총 생성할 오브젝트 수 먼저 계산
            for (int cx = -renderDistance; cx <= renderDistance; cx++)
            for (int cz = -renderDistance; cz <= renderDistance; cz++)
            {
                if (cx * cx + cz * cz > renderDistance * renderDistance) continue;
                var pos = new Vector2Int(center.x + cx, center.y + cz);
                if (!_chunkObjects.ContainsKey(pos) && _chunkDataMap.ContainsKey(pos))
                    totalToCreate++;
            }

            for (int cx = -renderDistance; cx <= renderDistance; cx++)
            for (int cz = -renderDistance; cz <= renderDistance; cz++)
            {
                if (cx * cx + cz * cz > renderDistance * renderDistance) continue;
                var pos = new Vector2Int(center.x + cx, center.y + cz);

                if (!_chunkObjects.ContainsKey(pos) && _chunkDataMap.ContainsKey(pos))
                {
                    CreateChunkObject(pos);
                    newlyCreated.Add(pos);
                    created++;

                    // 진척도 갱신 (데이터 생성 50% + 메시 생성 50%)
                    if (totalToCreate > 0)
                        LoadingScreen.Instance?.SetProgress(0.5f + 0.5f * ((float)created / totalToCreate));

                    yield return null;
                }
            }

            // ── 3단계: 새 청크 인접 경계면 리빌드 ───────────────────────────
            var borderChunks = new HashSet<Vector2Int>();
            foreach (var pos in newlyCreated)
            {
                borderChunks.Add(new Vector2Int(pos.x + 1, pos.y));
                borderChunks.Add(new Vector2Int(pos.x - 1, pos.y));
                borderChunks.Add(new Vector2Int(pos.x,     pos.y + 1));
                borderChunks.Add(new Vector2Int(pos.x,     pos.y - 1));
            }
            foreach (var pos in newlyCreated)
                borderChunks.Remove(pos);

            foreach (var pos in borderChunks)
                RebuildChunk(pos);

            // ── 4단계: 범위 벗어난 청크 제거 ────────────────────────────────
            var toRemove = new List<Vector2Int>();
            foreach (var pos in _chunkObjects.Keys)
            {
                int dx = pos.x - center.x;
                int dz = pos.y - center.y;
                if (dx * dx + dz * dz > (renderDistance + 1) * (renderDistance + 1))
                    toRemove.Add(pos);
            }

            foreach (var pos in toRemove)
            {
                var obj      = _chunkObjects[pos];
                var animator = obj.GetComponent<ChunkAnimator>();

                if (animator != null)
                    animator.PlayDespawn(() => Destroy(obj));
                else
                    Destroy(obj);

                _chunkObjects.Remove(pos);
                _chunkDataMap.Remove(pos);
            }
            
            OnChunksLoaded?.Invoke();
        }

        // ──────────────────────────────────────────────
        // 청크 오브젝트 생성
        // ──────────────────────────────────────────────

        void CreateChunkObject(Vector2Int pos)
        {
            if (!_chunkDataMap.ContainsKey(pos)) return;

            var obj = new GameObject($"Chunk_{pos.x}_{pos.y}");
            obj.transform.parent   = transform;
            obj.transform.position = new Vector3(
                pos.x * ChunkData.Width, 0, pos.y * ChunkData.Width);
            obj.layer = LayerMask.NameToLayer(chunkLayer);

            var mf = obj.AddComponent<MeshFilter>();
            var mr = obj.AddComponent<MeshRenderer>();

            ChunkMeshBuilder.Build(
                _chunkDataMap[pos], pos, _chunkDataMap, blockData, mf, mr);

            var animator          = obj.AddComponent<ChunkAnimator>();
            animator.startOffsetY = chunkSpawnOffsetY;
            animator.animDuration = chunkAnimDuration;
            animator.PlaySpawn();

            _chunkObjects[pos] = obj;
        }

        // ──────────────────────────────────────────────
        // 유틸
        // ──────────────────────────────────────────────

        Vector2Int GetChunkPos(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / ChunkData.Width),
                Mathf.FloorToInt(worldPos.z / ChunkData.Width));
        }

        public void SetBlock(Vector3 worldPos, BlockType type)
        {
            Vector2Int chunkPos = GetChunkPos(worldPos);
            if (!_chunkDataMap.ContainsKey(chunkPos)) return;

            int localX  = Mathf.FloorToInt(worldPos.x) - chunkPos.x * ChunkData.Width;
            int worldY  = Mathf.FloorToInt(worldPos.y);
            int localZ  = Mathf.FloorToInt(worldPos.z) - chunkPos.y * ChunkData.Width;

            // 월드 y 범위 체크
            if (localX < 0 || localX >= ChunkData.Width ||
                worldY < WorldYMin || worldY > WorldYMax  ||
                localZ < 0 || localZ >= ChunkData.Width) return;

            _chunkDataMap[chunkPos].SetBlock(localX, worldY, localZ, (byte)type);

            RebuildChunk(chunkPos);

            if (localX == 0)                   RebuildChunk(new Vector2Int(chunkPos.x - 1, chunkPos.y));
            if (localX == ChunkData.Width - 1) RebuildChunk(new Vector2Int(chunkPos.x + 1, chunkPos.y));
            if (localZ == 0)                   RebuildChunk(new Vector2Int(chunkPos.x, chunkPos.y - 1));
            if (localZ == ChunkData.Width - 1) RebuildChunk(new Vector2Int(chunkPos.x, chunkPos.y + 1));
        }

        void RebuildChunk(Vector2Int pos)
        {
            if (!_chunkObjects.ContainsKey(pos)) return;
            if (!_chunkDataMap.ContainsKey(pos)) return;

            var obj = _chunkObjects[pos];
            ChunkMeshBuilder.Build(
                _chunkDataMap[pos], pos, _chunkDataMap, blockData,
                obj.GetComponent<MeshFilter>(),
                obj.GetComponent<MeshRenderer>());
        }

        public void MarkChunkDirty(Vector2Int chunkPos)
        {
            if (!_chunkObjects.TryGetValue(chunkPos, out var obj)) return;

            ChunkMeshBuilder.Build(
                _chunkDataMap[chunkPos], chunkPos, _chunkDataMap,
                blockData,
                obj.GetComponent<MeshFilter>(),
                obj.GetComponent<MeshRenderer>());
        }

        public byte GetBlockAt(Vector3 worldPos)
        {
            Vector2Int chunkPos = GetChunkPos(worldPos);
            if (!_chunkDataMap.ContainsKey(chunkPos)) return 0;

            int localX = Mathf.FloorToInt(worldPos.x) - chunkPos.x * ChunkData.Width;
            int worldY = Mathf.FloorToInt(worldPos.y);
            int localZ = Mathf.FloorToInt(worldPos.z) - chunkPos.y * ChunkData.Width;

            // 월드 y 범위 체크
            if (localX < 0 || localX >= ChunkData.Width ||
                worldY < WorldYMin || worldY > WorldYMax  ||
                localZ < 0 || localZ >= ChunkData.Width)
                return 0;

            return _chunkDataMap[chunkPos].GetBlock(localX, worldY, localZ);
        }
        
        public void SaveChunks()
        {
            foreach (var kvp in _chunkDataMap)
                ChunkSerializer.Save(kvp.Key, kvp.Value);
            Debug.Log($"청크 저장 완료: {_chunkDataMap.Count}개");
        }
    }
}