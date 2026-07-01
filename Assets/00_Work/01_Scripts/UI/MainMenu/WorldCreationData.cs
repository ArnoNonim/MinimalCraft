namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 메인메뉴 → 게임 씬 간 월드 생성 설정값 전달용 정적 컨테이너.
    /// SceneManager.LoadScene() 이후에도 값이 유지된다.
    /// </summary>
    public static class WorldCreationData
    {
        public static bool  HasPendingCreation { get; private set; }
        public static int   Seed               { get; private set; }
        public static float NoiseScale         { get; private set; }

        public static void Set(int seed, float noiseScale)
        {
            Seed               = seed;
            NoiseScale         = noiseScale;
            HasPendingCreation = true;
        }

        public static void Consume()
        {
            HasPendingCreation = false;
        }
    }
}