using _00_Work._01_Scripts.ChunkSystem.Biome;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.ChunkSystem.Terrain;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public static class ChunkGenerator
    {
        public static TerrainSettingsSO Settings { get; set; }

        public static void Generate(ChunkData chunk, Vector2Int chunkPos)
        {
            for (int x = 0; x < ChunkData.Width; x++)
            for (int z = 0; z < ChunkData.Width; z++)
            {
                float worldX = chunkPos.x * ChunkData.Width + x;
                float worldZ = chunkPos.y * ChunkData.Width + z;

                BiomeType biome     = BiomeGenerator.GetBiome(worldX, worldZ);
                int       height    = BiomeGenerator.GetHeight(worldX, worldZ, biome);
                int       dirtDepth = GetDirtDepth(worldX, worldZ);

                // 바이옴별 해수면
                int seaLevel = GetSeaLevel(biome);

                BiomeGenerator.GetSurfaceBlocks(biome,
                    out byte topBlock, out byte subBlock);

                // 해안선 모래
                bool isNearWater = false;
                int[] dx = { 1, -1, 0,  0 };
                int[] dz = { 0,  0, 1, -1 };

                for (int i = 0; i < 4; i++)
                {
                    float nx     = worldX + dx[i];
                    float nz     = worldZ + dz[i];
                    BiomeType nb = BiomeGenerator.GetBiome(nx, nz);
                    int nh       = BiomeGenerator.GetHeight(nx, nz, nb);
                    int nSea     = GetSeaLevel(nb);

                    if (nh < nSea)
                    {
                        isNearWater = true;
                        break;
                    }
                }

                bool isShore = isNearWater      &&
                               biome != BiomeType.Desert &&
                               height >= seaLevel - 1   &&
                               height <= seaLevel + 1;

                if (isShore)
                {
                    topBlock = (byte)BlockType.Sand;
                    subBlock = (byte)BlockType.Sand;
                }

                if (biome == BiomeType.Mountain)
                {
                    int snowLine = Settings.baseHeight
                                   + Mathf.RoundToInt(Settings.mountainHeightRange * 0.55f);
                    if (height >= snowLine)
                        topBlock = (byte)BlockType.Snow;
                }
                
                for (int y = -ChunkData.YOffset; y < ChunkData.Height - ChunkData.YOffset; y++)
                {
                    byte block;

                    if (y == -ChunkData.YOffset)   // 배열 최하단 = 월드 y -40
                        block = (byte)BlockType.Stone;
                    else if (y > height && y <= seaLevel)
                        block = (byte)BlockType.Water;
                    else if (y > height)
                        block = (byte)BlockType.Air;
                    else if (y == height)
                        block = y <= seaLevel
                            ? (byte)BlockType.Sand : topBlock;
                    else if (y > height - dirtDepth)
                        block = y < seaLevel
                            ? (byte)BlockType.Sand : subBlock;
                    else
                        block = GetStoneType(worldX, y, worldZ);

                    chunk.SetBlock(x, y, z, block);
                }
            }

            CarveCaves(chunk, chunkPos);
            PlaceOres(chunk, chunkPos);
            DeepCityGenerator.Generate(chunk, chunkPos, Settings.seed, null);
        }

        static int GetSeaLevel(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean: return Settings.oceanSeaLevel;
                case BiomeType.River: return Settings.riverSeaLevel;
                case BiomeType.Pond:  return Settings.pondSeaLevel;
                default:              return Settings.seaLevel;
            }
        }

        static void PlaceOres(ChunkData chunk, Vector2Int chunkPos)
        {
            if (Settings.oreSettings == null)
            {
                Debug.LogWarning("OreSettings가 null");
                return;
            }

            // 월드 y 기준 순회 (-YOffset+1 ~ Height-YOffset-1)
            int worldYMin = -ChunkData.YOffset + 1;
            int worldYMax =  ChunkData.Height - ChunkData.YOffset - 1;

            for (int x = 0; x < ChunkData.Width; x++)
            for (int z = 0; z < ChunkData.Width; z++)
            for (int y = worldYMin; y <= worldYMax; y++)
            {
                // Stone / MiddleSite / Deepslate 모두 광물 배치 대상
                if (!IsStoneType(chunk.GetBlock(x, y, z))) continue;

                float worldX = chunkPos.x * ChunkData.Width + x;
                float worldZ = chunkPos.y * ChunkData.Width + z;

                foreach (var ore in Settings.oreSettings.ores)
                {
                    if (y < ore.minY || y > ore.maxY) continue;

                    float noise = Perlin3D(
                        worldX * ore.noiseScale + Settings.seed + ore.noiseOffset,
                        y      * ore.noiseScale + ore.noiseOffset,
                        worldZ * ore.noiseScale + Settings.seed + ore.noiseOffset);

                    if (noise > ore.threshold)
                    {
                        chunk.SetBlock(x, y, z, (byte)ore.blockType);
                        break;
                    }
                }
            }
        }

        /// <summary>광물이 생성될 수 있는 돌 계열 블록인지 확인</summary>
        static bool IsStoneType(byte block)
            => block == (byte)BlockType.Stone     ||
               block == (byte)BlockType.MiddleSite ||
               block == (byte)BlockType.Deepslate;

        static int GetDirtDepth(float x, float z)
        {
            float noise = Mathf.PerlinNoise(
                x * Settings.dirtNoiseScale + Settings.seed + 500f,
                z * Settings.dirtNoiseScale + Settings.seed + 500f);
            return Mathf.RoundToInt(noise * Settings.dirtMaxDepth)
                   + Settings.dirtMinDepth;
        }

        static void CarveCaves(ChunkData chunk, Vector2Int chunkPos)
        {
            for (int x = 0; x < ChunkData.Width; x++)
            for (int z = 0; z < ChunkData.Width; z++)
            {
                float worldX = chunkPos.x * ChunkData.Width + x;
                float worldZ = chunkPos.y * ChunkData.Width + z;

                BiomeType biome         = BiomeGenerator.GetBiome(worldX, worldZ);
                int       surfaceHeight = BiomeGenerator.GetHeight(worldX, worldZ, biome);

                for (int y = -ChunkData.YOffset + 1; y < ChunkData.Height - ChunkData.YOffset; y++)
                {
                    if (y >= surfaceHeight) continue;

                    bool carved = false;

                    // ── 1. 기존 동굴 ──
                    int caveMaxY = Mathf.Max(surfaceHeight - Settings.caveSurfaceOffset, -ChunkData.YOffset + 1);

                    if (y < caveMaxY)
                    {
                        float noise1 = Perlin3D(
                            worldX * Settings.caveScale + Settings.seed,
                            y      * Settings.caveScale,
                            worldZ * Settings.caveScale + Settings.seed);

                        float noise2 = Perlin3D(
                            worldX * Settings.caveScale + Settings.seed + 100f,
                            y      * Settings.caveScale + 100f,
                            worldZ * Settings.caveScale + Settings.seed + 100f);

                        float surfaceFade = Mathf.Clamp01(
                            (surfaceHeight - y) / (float)Settings.caveSurfaceFade);

                        if (noise1 * noise2 * surfaceFade > Settings.caveThreshold)
                            carved = true;
                    }

                    // ── 2. 치즈 동굴 ──
                    if (!carved &&
                        y >= Settings.cheeseCaveMinY &&
                        y <= Settings.cheeseCaveMaxY)
                    {
                        float cheese = Perlin3D(
                            worldX * Settings.cheeseCaveScale + Settings.seed + 200f,
                            y      * Settings.cheeseCaveScale + 200f,
                            worldZ * Settings.cheeseCaveScale + Settings.seed + 200f);

                        float heightFactor = Mathf.Clamp01(
                            1f - (float)y / Settings.cheeseCaveMaxY);

                        // 최대 Y 근처에서 부드럽게 페이드
                        float topFade = Mathf.Clamp01(
                            (float)(Settings.cheeseCaveMaxY - y) / 10f);
                        // 최소 Y 근처에서 부드럽게 페이드
                        float bottomFade = Mathf.Clamp01(
                            (float)(y - Settings.cheeseCaveMinY) / 10f);

                        float fade = topFade * bottomFade;

                        float threshold = Mathf.Lerp(
                            Settings.cheeseCaveThreshold,
                            Settings.cheeseCaveThreshold + 0.1f,
                            1f - heightFactor);

                        // fade가 낮을수록 threshold 높아짐 → 동굴 생성 안 됨
                        float adjustedThreshold = Mathf.Lerp(
                            1f, threshold, fade);

                        if (cheese > adjustedThreshold)
                            carved = true;
                    }

                    // ── 3. 스파게티 동굴 ──
                    if (!carved &&
                        y >= Settings.spaghettiMinY &&
                        y <= Settings.spaghettiMaxY)
                    {
                        float yScale1 = Settings.spaghettiScale1 * 0.06f;
                        float yScale2 = Settings.spaghettiScale2 * 0.06f;

                        float s1 = Perlin3D(
                            worldX * Settings.spaghettiScale1 + Settings.seed + 300f,
                            y      * yScale1                  + 300f,
                            worldZ * Settings.spaghettiScale1 + Settings.seed + 300f);

                        float s2 = Perlin3D(
                            worldX * Settings.spaghettiScale2 + Settings.seed + 400f,
                            y      * yScale2                  + 400f,
                            worldZ * Settings.spaghettiScale2 + Settings.seed + 400f);

                        // 최대/최소 Y 근처 페이드
                        float topFade    = Mathf.Clamp01(
                            (float)(Settings.spaghettiMaxY - y) / 10f);
                        float bottomFade = Mathf.Clamp01(
                            (float)(y - Settings.spaghettiMinY) / 10f);
                        float fade = topFade * bottomFade;

                        float d1 = Mathf.Abs(s1 - 0.5f);
                        float d2 = Mathf.Abs(s2 - 0.5f);

                        // fade 낮을수록 threshold 낮아짐 → 터널 생성 안 됨
                        float adjustedThreshold = Mathf.Lerp(
                            0f, Settings.spaghettiThreshold, fade);

                        if (d1 < adjustedThreshold || d2 < adjustedThreshold)
                            carved = true;
                    }

                    // ── 4. 지상 연결 동굴 ──
                    if (!carved &&
                        y >= surfaceHeight - Settings.entranceMinY &&
                        y < surfaceHeight)
                    {
                        float entrance = Perlin3D(
                            worldX * Settings.entranceScale + Settings.seed + 500f,
                            y      * Settings.entranceScale + 500f,
                            worldZ * Settings.entranceScale + Settings.seed + 500f);

                        float fade = Mathf.Clamp01(
                            (float)(surfaceHeight - y) / Settings.entranceMinY);

                        if (entrance * fade > Settings.entranceThreshold)
                            carved = true;
                    }

                    if (carved)
                        chunk.SetBlock(x, y, z, (byte)BlockType.Air);
                }
            }
        }
        
        static byte GetStoneType(float worldX, int y, float worldZ)
        {
            float noise = (Mathf.PerlinNoise(
                worldX * Settings.stoneTransitionScale + Settings.seed + 999f,
                worldZ * Settings.stoneTransitionScale + Settings.seed + 999f
            ) - 0.5f) * 2f;
 
            float effectiveY = y + noise * Settings.stoneTransitionRange;
 
            if (effectiveY >= Settings.andesiteStartY)  return (byte)BlockType.Stone;
            if (effectiveY >= Settings.deepslateStartY) return (byte)BlockType.MiddleSite;
            return (byte)BlockType.Deepslate;
        }

        static float Perlin3D(float x, float y, float z)
        {
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y, z);
            float xz = Mathf.PerlinNoise(x, z);
            float yx = Mathf.PerlinNoise(y, x);
            float zy = Mathf.PerlinNoise(z, y);
            float zx = Mathf.PerlinNoise(z, x);
            return (xy + yz + xz + yx + zy + zx) / 6f;
        }
    }
}