using System;
using UnityEngine;

namespace _00_Work._01_Scripts.Save
{
    /// <summary>
    /// 월드 전역 정보 저장 데이터
    /// 스폰 좌표 등 플레이어와 무관한 월드 정보
    /// </summary>
    [Serializable]
    public class WorldSaveData
    {
        // ── 월드 스폰 좌표 ────────────────────────────────────────────
        public bool  hasSpawnPoint;
        public float spawnX;
        public float spawnY;
        public float spawnZ;
    }
}