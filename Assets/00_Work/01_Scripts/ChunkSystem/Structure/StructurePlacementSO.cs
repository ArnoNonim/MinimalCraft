using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Biome;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Structure;
using UnityEngine;

[CreateAssetMenu(menuName = "Minecraft/StructurePlacement")]
public class StructurePlacementSO : ScriptableObject
{
    public StructureSO structure;

    [Header("바이옴")]
    public List<BiomeType> biomes;

    [Header("배치 설정")]
    public List<BlockType> validSurfaceBlocks;
    public int             noiseSeed   = 0;

    // 기존 단일 확률 대신 바이옴별 확률
    [System.Serializable]
    public class BiomeSpawnChance
    {
        public BiomeType biome;
        public float     chance = 0.1f;
    }

    public List<BiomeSpawnChance> biomeChances;

    // 바이옴별 확률 반환
    public float GetChance(BiomeType biome)
    {
        if (biomeChances == null) return 0f;
        foreach (var b in biomeChances)
            if (b.biome == biome) return b.chance;
        return 0f;
    }
}