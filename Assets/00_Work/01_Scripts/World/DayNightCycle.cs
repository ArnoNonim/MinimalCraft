using UnityEngine;

namespace _00_Work._01_Scripts.World
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("참조")]
        public Light    sunLight;
        public Material skyboxMaterial;

        [Header("설정")]
        public float dayDuration = 600f;

        [Header("밝기")]
        public Gradient skyTintGradient;    // 시간대별 Tint 색상
        public Gradient ambientGradient;    // 환경광
        public Gradient sunColorGradient;   // 태양 색상

        [Range(0f, 1f)]
        public float timeOfDay = 0f;

        private static readonly int TintID     = Shader.PropertyToID("_Tint");
        private static readonly int ExposureID = Shader.PropertyToID("_Exposure");
        private static readonly int RotationID = Shader.PropertyToID("_Rotation");

        void Start()
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        void Update()
        {
            timeOfDay += Time.deltaTime / dayDuration;
            if (timeOfDay >= 1f) timeOfDay = 0f;

            UpdateSkybox();
            UpdateSunLight();
            UpdateAmbient();
        }

        void UpdateSkybox()
        {
            // Tint로 밝기 조절
            skyboxMaterial.SetColor(TintID, skyTintGradient.Evaluate(timeOfDay));

            // 시간에 따라 회전 (동→서)
            skyboxMaterial.SetFloat(RotationID, timeOfDay * 360f);

            // 낮엔 밝고 밤엔 어둡게
            float dayT     = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI * 2f));
            float exposure = Mathf.Lerp(0.15f, 2f, dayT);
            skyboxMaterial.SetFloat(ExposureID, exposure);
        }

        void UpdateSunLight()
        {
            if (sunLight == null) return;

            sunLight.transform.rotation = Quaternion.Euler(
                timeOfDay * 360f - 90f, 170f, 0f);

            float intensity = Mathf.Clamp01(
                Mathf.Sin(timeOfDay * Mathf.PI * 2f));
            sunLight.intensity = intensity * intensity * 1.5f;
            sunLight.color     = sunColorGradient.Evaluate(timeOfDay);
        }

        void UpdateAmbient()
        {
            RenderSettings.ambientLight = ambientGradient.Evaluate(timeOfDay);
        }
    }
}