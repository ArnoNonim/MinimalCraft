using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    /// <summary>
    /// 유물 둥둥 떠다니기 + Y축 회전 + Light intensity 연동
    /// </summary>
    public class ArtifactFloatEffect : MonoBehaviour
    {
        [Header("부유 설정")]
        [SerializeField] private float floatAmplitude = 0.3f;   // 상하 진폭
        [SerializeField] private float floatSpeed     = 1.2f;   // 부유 속도

        [Header("회전 설정")]
        [SerializeField] private float rotateSpeed    = 45f;     // 도/초

        [Header("라이트 설정")]
        [SerializeField] private Light artifactLight;
        [SerializeField] private float lightMin       = 0.8f;    // 최소 intensity
        [SerializeField] private float lightMax       = 1.8f;    // 최대 intensity

        private Vector3 _originPos;
        private float   _timeOffset;

        private void Start()
        {
            _originPos  = transform.position;
            _timeOffset = Random.Range(0f, Mathf.PI * 2f); // 스폰마다 위상 랜덤
        }

        private void Update()
        {
            float t = Mathf.Sin(Time.time * floatSpeed + _timeOffset);

            // 부유
            transform.position = _originPos + Vector3.up * (t * floatAmplitude);

            // 회전
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // 라이트 intensity — 부유와 동기화
            if (artifactLight != null)
            {
                float normalized     = (t + 1f) * 0.5f; // -1~1 → 0~1
                artifactLight.intensity = Mathf.Lerp(lightMin, lightMax, normalized);
            }
        }
    }
}