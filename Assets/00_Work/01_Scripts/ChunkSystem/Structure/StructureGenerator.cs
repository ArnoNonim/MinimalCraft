using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Biome;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.ChunkSystem.Terrain;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    public static class StructureGenerator
    {
        public static TerrainSettingsSO Settings { get; set; }
        public static List<StructurePlacementSO> StructurePlacements { get; set; }

        // 월드 y 범위
        private static int WorldYMin => -ChunkData.YOffset;
        private static int WorldYMax =>  ChunkData.Height - ChunkData.YOffset - 1;

        public static void PlaceStructures(
            ChunkData chunk,
            Vector2Int chunkPos,
            Dictionary<Vector2Int, ChunkData> allChunks)
        {
            if (StructurePlacements == null) return;
            
            for (int x = 2; x < ChunkData.Width - 2; x++)
            for (int z = 2; z < ChunkData.Width - 2; z++)
            {
                float worldX = chunkPos.x * ChunkData.Width + x;
                float worldZ = chunkPos.y * ChunkData.Width + z;

                BiomeType biome  = BiomeGenerator.GetBiome(worldX, worldZ);
                int       surfaceY = GetSurfaceY(chunk, x, z);
                if (surfaceY == int.MinValue) continue;

                foreach (var placement in StructurePlacements)
                {
                    if (placement.structure == null) continue;
                    if (!placement.biomes.Contains(biome)) continue;

                    float chance = placement.GetChance(biome);
                    if (chance <= 0f) continue;

                    if (!ShouldPlace(worldX, worldZ, chance, placement.noiseSeed))
                        continue;

                    byte surfaceBlock = chunk.GetBlock(x, surfaceY, z);
                    if (!placement.validSurfaceBlocks.Contains(
                            (BlockType)surfaceBlock)) continue;

                    PlaceStructure(
                        placement.structure,
                        chunk, chunkPos, allChunks,
                        x, surfaceY + 1, z);

                    break;
                }
            }
        }

        static void PlaceStructure(
            StructureSO structure,
            ChunkData chunk, Vector2Int chunkPos,
            Dictionary<Vector2Int, ChunkData> allChunks,
            int rootX, int rootY, int rootZ)
        {
            foreach (var block in structure.blocks)
            {
                int wx = rootX + block.offset.x;
                int wy = rootY + block.offset.y;
                int wz = rootZ + block.offset.z;

                if (block.block == BlockType.Leaves)
                {
                    byte existing = GetBlockAt(
                        chunk, chunkPos, allChunks, wx, wy, wz);
                    if (existing == (byte)BlockType.Log) continue;
                }

                SetBlock(chunk, chunkPos, allChunks, wx, wy, wz, block.block);
            }
        }

        static byte GetBlockAt(
            ChunkData chunk, Vector2Int chunkPos,
            Dictionary<Vector2Int, ChunkData> allChunks,
            int x, int y, int z)
        {
            // 월드 y 범위 체크
            if (y < WorldYMin || y > WorldYMax) return 0;

            if (x >= 0 && x < ChunkData.Width &&
                z >= 0 && z < ChunkData.Width)
                return chunk.GetBlock(x, y, z);

            int neighborCX = chunkPos.x + FloorDiv(x, ChunkData.Width);
            int neighborCZ = chunkPos.y + FloorDiv(z, ChunkData.Width);
            int localX     = Mod(x, ChunkData.Width);
            int localZ     = Mod(z, ChunkData.Width);

            var neighborPos = new Vector2Int(neighborCX, neighborCZ);
            if (allChunks.TryGetValue(neighborPos, out var neighborChunk))
                return neighborChunk.GetBlock(localX, y, localZ);

            return 0;
        }

        static bool ShouldPlace(
            float worldX, float worldZ,
            float chance, int seed)
        {
            float noise = Mathf.PerlinNoise(
                worldX * 0.15f + seed,
                worldZ * 0.15f + seed);

            return noise > (1f - chance);
        }

        /// <summary>
        /// 월드 y 기준 위→아래로 가장 높은 비공기 블록 위치 반환
        /// 없으면 int.MinValue 반환
        /// </summary>
        static int GetSurfaceY(ChunkData chunk, int x, int z)
        {
            for (int y = WorldYMax; y >= WorldYMin; y--)
            {
                if (chunk.GetBlock(x, y, z) != (byte)BlockType.Air)
                    return y;
            }
            return int.MinValue;
        }

        static void SetBlock(
            ChunkData chunk, Vector2Int chunkPos,
            Dictionary<Vector2Int, ChunkData> allChunks,
            int x, int y, int z, BlockType type)
        {
            // 월드 y 범위 체크
            if (y < WorldYMin || y > WorldYMax) return;

            if (x >= 0 && x < ChunkData.Width &&
                z >= 0 && z < ChunkData.Width)
            {
                if (chunk.GetBlock(x, y, z) == (byte)BlockType.Air)
                    chunk.SetBlock(x, y, z, (byte)type);
                return;
            }

            int neighborCX = chunkPos.x + FloorDiv(x, ChunkData.Width);
            int neighborCZ = chunkPos.y + FloorDiv(z, ChunkData.Width);
            int localX     = Mod(x, ChunkData.Width);
            int localZ     = Mod(z, ChunkData.Width);

            var neighborPos = new Vector2Int(neighborCX, neighborCZ);
            if (allChunks.TryGetValue(neighborPos, out var neighborChunk))
            {
                if (neighborChunk.GetBlock(localX, y, localZ) == (byte)BlockType.Air)
                    neighborChunk.SetBlock(localX, y, localZ, (byte)type);
            }
        }

        static int FloorDiv(int a, int b)
            => a >= 0 ? a / b : (a - b + 1) / b;

        static int Mod(int a, int b)
            => ((a % b) + b) % b;
    }
}