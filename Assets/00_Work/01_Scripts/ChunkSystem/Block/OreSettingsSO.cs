using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Block
{
    [CreateAssetMenu(fileName = "OreSettings", menuName = "Ore/OreSettings", order = 0)]
    public class OreSettingsSO : ScriptableObject
    {
        [System.Serializable]
        public class OreData
        {
            public string    oreName;
            public BlockType blockType;

            [Header("생성 범위")]
            public int minY          = 0;
            public int maxY          = 64;

            [Header("노이즈 설정")]
            public float noiseScale  = 0.1f;
            public float threshold   = 0.75f;  // 높을수록 희귀
            public float noiseOffset = 0f;     // 시드 오프셋

            [Header("바이옴 제한")]
            public bool  anyBiome    = true;
        }

        public OreData[] ores;
    }
}