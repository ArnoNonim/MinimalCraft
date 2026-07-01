using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    [Serializable]
    public class ArtifactSaveData
    {
        public List<string>  spawnedCityKeys = new();
        public List<Vector3> spawnedPositions = new();

        public string MakeKey(int ox, int oz) => $"{ox}_{oz}";

        public bool HasSpawned(int ox, int oz)
            => spawnedCityKeys.Contains(MakeKey(ox, oz));

        public void MarkSpawned(int ox, int oz, Vector3 pos)
        {
            var key = MakeKey(ox, oz);
            if (!spawnedCityKeys.Contains(key))
            {
                spawnedCityKeys.Add(key);
                spawnedPositions.Add(pos); // ← 위치도 저장
            }
        }
    }
}