using System.Collections;
using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Block
{
    public class GrassSpreadSystem : MonoBehaviour
    {
        [Header("참조")]
        public ChunkManager chunkManager;

        [Header("설정")]
        public float checkInterval  = 3f;
        public int   spreadRangeMin = 1;
        public int   spreadRangeMax = 5;

        // 월드 y 범위
        private static readonly int WorldYMin = -ChunkData.YOffset;
        private static readonly int WorldYMax =  ChunkData.Height - ChunkData.YOffset - 1;

        void Start()
        {
            StartCoroutine(SpreadGrass());
        }

        IEnumerator SpreadGrass()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);
                ProcessGrassSpread();
            }
        }

        void ProcessGrassSpread()
        {
            var candidates = new List<(Vector2Int chunkPos, int x, int y, int z)>();

            var chunkKeys = new List<Vector2Int>(chunkManager.LoadedChunks.Keys);

            foreach (var chunkPos in chunkKeys)
            {
                if (!chunkManager.LoadedChunks.TryGetValue(chunkPos, out ChunkData chunk)) continue;

                for (int x = 0; x < ChunkData.Width; x++)
                for (int z = 0; z < ChunkData.Width; z++)
                // 월드 y 기준 위→아래 탐색, 최상단 -1 (above 체크를 위해)
                for (int y = WorldYMax - 1; y >= WorldYMin; y--)
                {
                    byte block = chunk.GetBlock(x, y, z);
                    if (block != (byte)BlockType.Dirt) continue;

                    byte above = chunk.GetBlock(x, y + 1, z);
                    if (above != (byte)BlockType.Air) continue;

                    candidates.Add((chunkPos, x, y, z));
                    break; // 같은 x,z에서 가장 위 흙만
                }
            }

            if (candidates.Count == 0) return;

            int spreadCount = Random.Range(
                spreadRangeMin,
                Mathf.Min(spreadRangeMax, candidates.Count) + 1);

            // 셔플
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            var dirtyChunks = new HashSet<Vector2Int>();

            for (int i = 0; i < spreadCount; i++)
            {
                var (chunkPos, x, y, z) = candidates[i];

                if (!chunkManager.LoadedChunks.TryGetValue(chunkPos, out var chunk)) continue;

                chunk.SetBlock(x, y, z, (byte)BlockType.Grass);
                dirtyChunks.Add(chunkPos);
            }

            foreach (var chunkPos in dirtyChunks)
                chunkManager.MarkChunkDirty(chunkPos);
        }
    }
}