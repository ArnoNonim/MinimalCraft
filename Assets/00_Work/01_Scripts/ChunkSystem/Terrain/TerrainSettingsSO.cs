using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Terrain
{
    [CreateAssetMenu(fileName = "TerrainSettings", menuName = "Minecraft/TerrainSettings")]
    public class TerrainSettingsSO : ScriptableObject
    {
        [Header("시드")]
        public int  seed       = 0;
        public bool randomSeed = false;

        [Header("지형 부드러움")]
        [Range(1, 8)]
        public int octaves = 2;
        [Range(0f, 1f)]
        public float persistence = 0.4f;
        [Range(1f, 4f)]
        public float lacunarity = 2f;

        [Header("노이즈 기본 설정")]
        public float noiseScale  = 0.003f;

        [Header("바이옴 분류 임계값")]
        [Range(0f, 1f)] public float snowMaxTemp    = 0.35f;
        [Range(0f, 1f)] public float desertMinTemp  = 0.6f;
        [Range(0f, 1f)] public float desertMaxHumid = 0.45f;
        [Range(0f, 1f)] public float forestMinHumid = 0.55f;
        [Range(0f, 1f)] public float mountainMinTemp   = 0.25f;
        [Range(0f, 1f)] public float mountainMaxTemp   = 0.55f;
        [Range(0f, 1f)] public float mountainMinHumid  = 0.30f;
        [Range(0f, 1f)] public float mountainMaxHumid  = 0.60f;

        [Header("바이옴 노이즈")]
        public float biomeScale = 0.002f;

        [Header("바이옴 블렌딩")]
        [Range(8f, 64f)]
        public float blendRange = 24f;

        [Header("바이옴별 높이")]
        public int baseHeight = 10;

        [Space]
        public int plainsHeightRange = 10;
        public int desertHeightRange = 6;
        public int snowHeightRange   = 25;
        public int forestHeightRange = 15;
        public int mountainHeightRange = 90;

        [Header("흙 두께")]
        public float dirtNoiseScale = 0.1f;
        public int   dirtMinDepth   = 2;
        public int   dirtMaxDepth   = 6;

        [Header("해수면")]
        public int seaLevel        = 55;
    
        [Header("물 바이옴")]
        public int   oceanDepth    = 30;  // 바다 깊이
        public int   riverDepth    = 8;   // 강 깊이
        public int   pondDepth     = 5;   // 연못 깊이
        public int   oceanSeaLevel = 55;  // 바다 해수면
        public int   riverSeaLevel = 55;  // 강 해수면
        public int   pondSeaLevel  = 55;  // 연못 해수면

        [Header("물 바이옴 분류")]
        public float oceanMinHumid  = 0.75f; // 습도 높으면 바다
        public float riverMinHumid  = 0.65f; // 중간 습도 강
        public float pondChance     = 0.3f;  // 연못 생성 확률

        [Header("동굴 설정")]
        public float caveScale         = 0.04f;
        public float caveThreshold     = 0.18f;
        public int   caveSurfaceOffset = 6;
        public int   caveSurfaceFade   = 12;

        [Header("치즈 동굴")]
        public float cheeseCaveScale     = 0.02f;
        public float cheeseCaveThreshold = 0.55f;
        public int   cheeseCaveMinY      = 5;
        public int   cheeseCaveMaxY      = 50;

        [Header("스파게티 동굴")]
        public float spaghettiScale1    = 0.04f;
        public float spaghettiScale2    = 0.04f;
        public float spaghettiThreshold = 0.02f;
        public int   spaghettiMinY      = 5;
        public int   spaghettiMaxY      = 60;

        [Header("지상 연결 동굴")]
        public float entranceScale     = 0.015f;
        public float entranceThreshold = 0.6f;
        public int   entranceMinY      = 10;

        [Header("돌 종류 경계")]
        [Tooltip("중층석(Andesite) 시작 Y — 이 아래부터 중층석 섞임 시작")]
        public int andesiteStartY  = 30;

        [Tooltip("심층석(Deepslate) 시작 Y — 이 아래부터 심층석 섞임 시작")]
        public int deepslateStartY = 0;

        [Tooltip("경계 노이즈 스케일 — 작을수록 경계가 넓고 자연스러움")]
        public float stoneTransitionScale = 0.04f;

        [Tooltip("경계 블렌딩 두께 (블록) — 클수록 전환 구간이 넓어짐")]
        public int stoneTransitionRange = 8;
    
        [Header("광물")]
        public OreSettingsSO oreSettings;
    }
}