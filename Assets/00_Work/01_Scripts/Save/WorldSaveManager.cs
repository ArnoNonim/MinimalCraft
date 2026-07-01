using System.IO;
using UnityEngine;

namespace _00_Work._01_Scripts.Save
{
    /// <summary>
    /// 월드 전역 정보 저장/불러오기
    /// 스폰 좌표는 최초 1회만 생성 — 이후 리스폰 시 재사용
    /// </summary>
    public class WorldSaveManager : MonoBehaviour
    {
        public static WorldSaveManager Instance { get; private set; }

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "WorldSave.json");

        private WorldSaveData _data;

        public bool HasSpawnPoint => _data != null && _data.hasSpawnPoint;

        public Vector3 SpawnPoint => _data != null
            ? new Vector3(_data.spawnX, _data.spawnY, _data.spawnZ)
            : Vector3.zero;

        // ──────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Load();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// 월드 스폰 좌표 설정 — 최초 1회만 호출
        /// 이미 설정된 경우 무시
        /// </summary>
        public void SetSpawnPointOnce(Vector3 pos)
        {
            if (_data.hasSpawnPoint) return;

            _data.hasSpawnPoint = true;
            _data.spawnX        = pos.x;
            _data.spawnY        = pos.y;
            _data.spawnZ        = pos.z;

            Save();
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            _data = new WorldSaveData();
            Debug.Log("[WorldSave] 월드 저장 파일 삭제");
        }

        public bool HasSave() => File.Exists(SavePath);

        // ──────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────

        void Save()
        {
            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }

        void Load()
        {
            if (!File.Exists(SavePath))
            {
                _data = new WorldSaveData();
                return;
            }

            string json = File.ReadAllText(SavePath);
            _data = JsonUtility.FromJson<WorldSaveData>(json) ?? new WorldSaveData();
        }
    }
}