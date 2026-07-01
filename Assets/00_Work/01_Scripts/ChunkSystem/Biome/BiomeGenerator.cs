using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Terrain;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Biome
{
    public static class BiomeGenerator
    {
        public static TerrainSettingsSO Settings { get; set; }
        
        public static float GetBiomeAmbientTemperature(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 42f;   // 사막 — 고온
                case BiomeType.Forest: return 37f;   // 숲   — 약간 따뜻
                case BiomeType.Plains: return 36.5f; // 평원 — 정상 체온
                case BiomeType.River:  return 35f;   // 강   — 약간 저체온
                case BiomeType.Pond:   return 35f;   // 연못 — 약간 저체온
                case BiomeType.Ocean:  return 33f;   // 바다 — 저체온
                case BiomeType.Mountain: return 32f; // 고산 — 저체온 위험
                case BiomeType.Snow:   return 30f;   // 설원 — 심각한 저체온
                default:               return 36.5f;
            }
        }

        public static BiomeType GetBiome(float worldX, float worldZ)
        {
            if (Settings == null)
                return BiomeType.Plains;

            float temp, humidity;
            GetClimate(worldX, worldZ, out temp, out humidity);
            return ClassifyBiome(worldX, worldZ, temp, humidity);
        }

        public static void GetClimate(float worldX, float worldZ,
            out float temperature, out float humidity)
        {
            float s  = Settings.biomeScale;
            float sd = Settings.seed * 0.00371f;

            temperature =
                Mathf.PerlinNoise(worldX * s        + sd + 300f, worldZ * s        + sd + 300f) * 0.6f +
                Mathf.PerlinNoise(worldX * s * 2.5f + sd + 150f, worldZ * s * 2.5f + sd + 150f) * 0.3f +
                Mathf.PerlinNoise(worldX * s * 6f   + sd + 450f, worldZ * s * 6f   + sd + 450f) * 0.1f;

            humidity =
                Mathf.PerlinNoise(worldX * s        + sd + 600f, worldZ * s        + sd + 600f) * 0.6f +
                Mathf.PerlinNoise(worldX * s * 3f   + sd + 750f, worldZ * s * 3f   + sd + 750f) * 0.3f +
                Mathf.PerlinNoise(worldX * s * 7f   + sd + 900f, worldZ * s * 7f   + sd + 900f) * 0.1f;
        }

        static BiomeType ClassifyBiome(
            float worldX, float worldZ,
            float temp, float humidity)
        {
            if (humidity > Settings.oceanMinHumid)
                return BiomeType.Ocean;

            if (humidity > Settings.riverMinHumid)
                return BiomeType.River;

            float pondNoise = Mathf.PerlinNoise(
                worldX * 0.02f + Settings.seed + 777f,
                worldZ * 0.02f + Settings.seed + 777f);
            if (pondNoise > (1f - Settings.pondChance))
                return BiomeType.Pond;

            
            if (temp >= Settings.mountainMinTemp   &&
                temp <= Settings.mountainMaxTemp   &&
                humidity >= Settings.mountainMinHumid &&
                humidity <= Settings.mountainMaxHumid)
                return BiomeType.Mountain;
            if (temp < Settings.snowMaxTemp)
                return BiomeType.Snow;
            if (temp > Settings.desertMinTemp &&
                humidity < Settings.desertMaxHumid)
                return BiomeType.Desert;
            if (humidity > Settings.forestMinHumid)
                return BiomeType.Forest;

            return BiomeType.Plains;
        }

        static float GetRawHeight(float x, float z, BiomeType biome)
        {
            float largeNoise = Mathf.PerlinNoise(
                x * 0.003f + Settings.seed,
                z * 0.003f + Settings.seed);

            float medNoise = Mathf.PerlinNoise(
                x * 0.008f + Settings.seed + 100f,
                z * 0.008f + Settings.seed + 100f);

            float smallNoise = Mathf.PerlinNoise(
                x * 0.02f + Settings.seed + 200f,
                z * 0.02f + Settings.seed + 200f);

            float combined = (largeNoise * 0.6f
                              + medNoise   * 0.3f
                              + smallNoise * 0.1f) - 0.5f;

            switch (biome)
            {
                case BiomeType.Ocean:
                {
                    // combined -0.5~0.5 → 중앙(0)이 가장 깊고 가장자리(±0.5)는 얕음
                    // pow로 오목한 곡선 강조
                    float depth = Mathf.Pow(1f - Mathf.Abs(combined) * 2f, 1.5f);
                    return Settings.oceanSeaLevel - Mathf.RoundToInt(Settings.oceanDepth * depth);
                }

                case BiomeType.River:
                {
                    float depth = Mathf.Pow(1f - Mathf.Abs(combined) * 2f, 2f);
                    return Settings.riverSeaLevel - Mathf.RoundToInt(Settings.riverDepth * depth);
                }

                case BiomeType.Pond:
                {
                    float depth = Mathf.Pow(1f - Mathf.Abs(combined) * 2f, 2.5f);
                    return Settings.pondSeaLevel - Mathf.RoundToInt(Settings.pondDepth * depth);
                }
                
                case BiomeType.Mountain:
                { 
                    float mountainHeightRange = Settings.mountainHeightRange;
                    
                    // 산봉우리: combined을 제곱해서 양수 방향으로 과장
                    // combined 범위 -0.5~0.5 → 제곱 후 0~0.25, 부호 복원
                    float ridgeNoise = Mathf.PerlinNoise(
                        x * 0.005f + Settings.seed * 0.00371f + 1111f,
                        z * 0.005f + Settings.seed * 0.00371f + 1111f);
                    
                    // 능선 효과: 1 - |noise - 0.5| * 2 → 0.5 근처에서 높아짐
                    float ridge = 1f - Mathf.Abs(ridgeNoise - 0.5f) * 2f;
                    ridge = Mathf.Pow(ridge, 2.5f); // 뾰족한 산봉우리
                    
                    // 기본 노이즈와 능선 노이즈 합산
                    float baseContrib  = combined * 0.4f + 0.2f; // 기본 지형 기여
                    float ridgeContrib = ridge * 0.6f;            // 능선 기여
                    
                    return Settings.baseHeight + (baseContrib + ridgeContrib) * mountainHeightRange;
                }
            }

            float heightRange = GetBiomeHeightRange(biome);
            return Settings.baseHeight + combined * heightRange;
        }

        static float GetBiomeHeightRange(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Plains:  return Settings.plainsHeightRange;
                case BiomeType.Desert:  return Settings.desertHeightRange;
                case BiomeType.Snow:    return Settings.snowHeightRange;
                case BiomeType.Forest:  return Settings.forestHeightRange;
                case BiomeType.Mountain: return Settings.mountainHeightRange;
                default:                return Settings.plainsHeightRange;
            }
        }

        public static int GetHeight(float worldX, float worldZ, BiomeType biome)
        {
            float range = Settings.blendRange;
            float step  = range / 2f;

            float totalHeight = 0f;
            float totalWeight = 0f;

            for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            {
                float sx = worldX + dx * step;
                float sz = worldZ + dz * step;

                float st, sh;
                GetClimate(sx, sz, out st, out sh);
                BiomeType sampleBiome = ClassifyBiome(sx, sz, st, sh);
                float sampleHeight    = GetRawHeight(sx, sz, sampleBiome);

                float dist   = Mathf.Sqrt(dx * dx + dz * dz);
                float weight = Mathf.Exp(-dist * 0.8f);

                totalHeight += sampleHeight * weight;
                totalWeight += weight;
            }

            return Mathf.RoundToInt(totalHeight / totalWeight);
        }

        public static void GetSurfaceBlocks(
            BiomeType biome, out byte topBlock, out byte subBlock)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    topBlock = (byte)BlockType.Sand;
                    subBlock = (byte)BlockType.Sand;
                    break;
                case BiomeType.Snow:
                    topBlock = (byte)BlockType.Snow;
                    subBlock = (byte)BlockType.Dirt;
                    break;
                case BiomeType.Ocean:
                case BiomeType.River:
                case BiomeType.Pond:
                    topBlock = (byte)BlockType.Sand;
                    subBlock = (byte)BlockType.Sand;
                    break;
                case BiomeType.Mountain:
                    // 높이에 따라 눈 덮개 or 돌 표면
                    // (높이 정보 없이 바이옴만으로 결정하므로 기본은 Stone)
                    topBlock = (byte)BlockType.Stone;
                    subBlock = (byte)BlockType.Stone;
                    break;
                case BiomeType.Forest:
                case BiomeType.Plains:
                default:
                    topBlock = (byte)BlockType.Grass;
                    subBlock = (byte)BlockType.Dirt;
                    break;
            }
        }
    }
}
