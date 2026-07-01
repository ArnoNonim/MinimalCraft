using _00_Work._01_Scripts.ChunkSystem.Terrain;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 게임 씬 진입 직후 WorldCreationData를 소비해서
    /// TerrainSettings SO에 Seed / NoiseScale을 덮어씀.
    /// ChunkManager보다 Script Execution Order가 앞서야 함.
    /// </summary>
    public class WorldCreationApplier : MonoBehaviour
    {
        [SerializeField] private TerrainSettingsSO terrainSettings;

        private void Awake()
        {
            if (!WorldCreationData.HasPendingCreation) return;

            terrainSettings.seed       = WorldCreationData.Seed;
            terrainSettings.noiseScale = WorldCreationData.NoiseScale;
            terrainSettings.randomSeed = false; // 지정 시드 사용

            WorldCreationData.Consume();
        }
    }
}