using System.IO;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    /// <summary>
    /// 심층 도시당 유물을 딱 1개만 스폰.
    /// DeepCityGenerator의 신전 중앙 좌표에 배치.
    /// ChunkManager.OnChunksLoaded 이벤트에 연결해서 청크 로딩 후 스폰 시도.
    /// </summary>
    public class ArtifactSpawner : MonoBehaviour
    {
        public static ArtifactSpawner Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private GameObject artifactPrefab;
        [SerializeField] private Transform  player;

        [Header("스폰 설정")]
        [Tooltip("신전 바닥 위 오프셋 (y)")]
        [SerializeField] private float spawnHeightOffset = 2f;

        private ArtifactSaveData _saveData;
        private bool _restored;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "ArtifactSave.json");

        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        private void Start()
        {
            if (Chunk.ChunkManager.Instance != null)
                Chunk.ChunkManager.Instance.OnChunksLoaded += TrySpawn;
        }

        private void OnDestroy()
        {
            if (Chunk.ChunkManager.Instance != null)
                Chunk.ChunkManager.Instance.OnChunksLoaded -= TrySpawn;
        }

        // ──────────────────────────────────────────────
        // 스폰 시도
        // ──────────────────────────────────────────────

        private void TrySpawn()
        {
            if (artifactPrefab == null || player == null) return;

            // 저장된 유물 복원 — 아직 스폰 안 된 것만
            foreach (var pos in _saveData.spawnedPositions)
            {
                // 이미 씬에 있는지 체크 없이 그냥 스폰하면 중복될 수 있으니까
                // 첫 번째 OnChunksLoaded에서만 복원하도록 플래그 사용
                if (!_restored)
                {
                    Instantiate(artifactPrefab, pos, Quaternion.identity);
                }
            }
            _restored = true;

            // 플레이어 위치 기준 도시 원점 계산
            Vector2Int? cityOrigin = GetCityOriginAt(player.position);
            if (!cityOrigin.HasValue)
            {
                Debug.Log($"[Artifact] 플레이어 위치 {player.position}에서 도시 못 찾음");
                return;
            }

            int ox = cityOrigin.Value.x;
            int oz = cityOrigin.Value.y;

            // 이미 스폰된 도시면 스킵
            if (_saveData.HasSpawned(ox, oz)) return;

            // 신전 중앙 = cityOrigin + HalfCity
            int worldX = ox * Chunk.ChunkData.Width + DeepCityGenerator.HalfCity;
            int worldZ = oz * Chunk.ChunkData.Width + DeepCityGenerator.HalfCity;

            // DeepCity 바닥 Y + 오프셋
            float spawnY = -38f + spawnHeightOffset;

            var spawnPos = new Vector3(worldX + 0.5f, spawnY, worldZ + 0.5f);
            Instantiate(artifactPrefab, spawnPos, Quaternion.identity);

            _saveData.MarkSpawned(ox, oz, spawnPos);
            Save();

            Debug.Log($"[ArtifactSpawner] 유물 스폰 완료: {spawnPos}");
        }

        // ──────────────────────────────────────────────
        // 도시 원점 계산 (DeepCityGenerator 로직 재활용)
        // ──────────────────────────────────────────────

        private Vector2Int? GetCityOriginAt(Vector3 worldPos)
        {
            var chunkManager = Chunk.ChunkManager.Instance;
            if (chunkManager == null) return null;

            int seed     = chunkManager.terrainSettings.seed;
            int gridSize = DeepCityGenerator.CitySize / Chunk.ChunkData.Width;

            int chunkX = Mathf.FloorToInt(worldPos.x / Chunk.ChunkData.Width);
            int chunkZ = Mathf.FloorToInt(worldPos.z / Chunk.ChunkData.Width);

            int cellX = Mathf.FloorToInt((float)chunkX / gridSize);
            int cellZ = Mathf.FloorToInt((float)chunkZ / gridSize);

            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                int cx = cellX + dx;
                int cz = cellZ + dz;

                float noise = Mathf.PerlinNoise(
                    cx * 1.3f + seed * 0.00371f + 17.3f,
                    cz * 1.3f + seed * 0.00371f + 31.7f);

                if (noise < 0.40f) continue;

                var origin = new Vector2Int(cx * gridSize, cz * gridSize);

                if (chunkX >= origin.x && chunkX < origin.x + gridSize &&
                    chunkZ >= origin.y && chunkZ < origin.y + gridSize)
                    return origin;
            }
            return null;
        }

        // ──────────────────────────────────────────────
        // 저장 / 로드
        // ──────────────────────────────────────────────

        private void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(_saveData, prettyPrint: true));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ArtifactSpawner] 저장 실패: {e.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    _saveData = JsonUtility.FromJson<ArtifactSaveData>(File.ReadAllText(SavePath));
                else
                    _saveData = new ArtifactSaveData();
            }
            catch
            {
                _saveData = new ArtifactSaveData();
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            _saveData = new ArtifactSaveData();
        }
    }
}