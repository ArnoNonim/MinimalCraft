using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using UnityEngine;
using UnityEngine.Audio;

namespace _00_Work._01_Scripts.Sound
{
    /// <summary>
    /// 공간 크기 기반 리버브 제어
    ///
    /// [감지 방식]
    /// 플레이어 주변 6방향(상하좌우앞뒤)으로 블록까지 거리 측정
    /// 평균 거리 → 공간 크기 추정 → AudioMixer 리버브 파라미터 조절
    ///
    /// [AudioMixer 세팅]
    /// Master → Reverb Zone Mix, Room, Decay Time 파라미터 Expose 필요
    /// </summary>
    public class ReverbController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Transform    playerTransform;
        [SerializeField] private ChunkManager chunkManager;
        [SerializeField] private AudioMixer   audioMixer;

        [Header("감지 설정")]
        [Tooltip("공간 크기 감지 주기 (초)")]
        [SerializeField] private float checkInterval = 0.4f;

        [Tooltip("감지할 최대 거리 (블록)")]
        [SerializeField] private int maxScanDistance = 24;

        [Header("전환 속도")]
        [SerializeField] private float transitionSpeed = 2f;

        [Header("─── AudioMixer 파라미터 이름 ───")]
        [Tooltip("Mixer에서 Expose한 Room 파라미터 이름")]
        [SerializeField] private string roomParam      = "ReverbRoom";

        [Tooltip("Mixer에서 Expose한 Decay Time 파라미터 이름")]
        [SerializeField] private string decayParam     = "ReverbDecay";

        [Tooltip("Mixer에서 Expose한 Reverb Mix 파라미터 이름")]
        [SerializeField] private string mixParam       = "ReverbMix";

        [Header("─── 공간별 리버브 설정 ───")]

        [Header("야외 / 개방 공간 (spaceSize > openThreshold)")]
        [SerializeField] private float openRoom  = -10000f; // 리버브 없음
        [SerializeField] private float openDecay = 0.3f;
        [SerializeField] private float openMix   = 0f;

        [Header("소형 밀폐 공간 (spaceSize 1~5)")]
        [SerializeField] private float smallRoom  = -2000f;
        [SerializeField] private float smallDecay = 0.8f;
        [SerializeField] private float smallMix   = 0.4f;

        [Header("중형 공간 (spaceSize 5~12)")]
        [SerializeField] private float medRoom  = -1000f;
        [SerializeField] private float medDecay = 1.5f;
        [SerializeField] private float medMix   = 0.65f;

        [Header("대형 공간 / 동굴 (spaceSize 12~20)")]
        [SerializeField] private float largeRoom  = -500f;
        [SerializeField] private float largeDecay = 2.8f;
        [SerializeField] private float largeMix   = 0.8f;

        [Header("거대 공간 / 광장 (spaceSize > 20)")]
        [SerializeField] private float hugeRoom  = -200f;
        [SerializeField] private float hugeDecay = 4.5f;
        [SerializeField] private float hugeMix   = 0.9f;

        [Tooltip("이 값 이상이면 야외로 판정")]
        [SerializeField] private float openThreshold = 15f;

        // ── 상태 ────────────────────────────────────────────────────
        private float _checkTimer;
        private float _targetRoom;
        private float _targetDecay;
        private float _targetMix;
        private float _currentRoom;
        private float _currentDecay;
        private float _currentMix;

        // ──────────────────────────────────────────────
        // Unity 이벤트
        // ──────────────────────────────────────────────

        void Awake()
        {
            // 초기값 — 야외 설정
            _currentRoom  = _targetRoom  = openRoom;
            _currentDecay = _targetDecay = openDecay;
            _currentMix   = _targetMix   = openMix;
            ApplyToMixer(_currentRoom, _currentDecay, _currentMix);
        }

        void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= checkInterval)
            {
                _checkTimer = 0f;
                MeasureSpace();
            }

            // 부드러운 전환
            _currentRoom  = Mathf.MoveTowards(_currentRoom,  _targetRoom,  transitionSpeed * 200f * Time.deltaTime);
            _currentDecay = Mathf.MoveTowards(_currentDecay, _targetDecay, transitionSpeed * Time.deltaTime);
            _currentMix = Mathf.MoveTowards(_currentMix, _targetMix, transitionSpeed * 200f * Time.deltaTime);

            ApplyToMixer(_currentRoom, _currentDecay, _currentMix);
        }

        // ──────────────────────────────────────────────
        // 공간 크기 측정
        // ──────────────────────────────────────────────

        void MeasureSpace()
        {
            if (chunkManager == null || playerTransform == null) return;

            Vector3 pos = playerTransform.position;

            // 6방향 스캔
            int[] dirs = { 1, -1 };
            float totalDist = 0f;
            int   count     = 0;
            bool  hasOpen   = false; // 위가 뚫려있는지 (야외 판정)

            // 위 방향 스캔 (야외 판정용)
            int upDist = ScanDirection(pos, Vector3Int.up);
            if (upDist >= maxScanDistance) hasOpen = true;

            // 6방향 평균 거리
            totalDist += upDist;
            totalDist += ScanDirection(pos, Vector3Int.down);
            totalDist += ScanDirection(pos, Vector3Int.left);
            totalDist += ScanDirection(pos, Vector3Int.right);
            totalDist += ScanDirection(pos, Vector3Int.forward);
            totalDist += ScanDirection(pos, Vector3Int.back);
            count      = 6;

            float avgSpace = totalDist / count;

            // 야외 — 위가 뚫리고 평균 거리도 넓으면
            if (hasOpen && avgSpace >= openThreshold)
            {
                SetTarget(openRoom, openDecay, openMix);
                return;
            }

            // 공간 크기별 리버브 결정
            SetTargetBySize(avgSpace);
        }

        /// <summary>특정 방향으로 블록까지 거리 반환 (블록 단위)</summary>
        int ScanDirection(Vector3 origin, Vector3Int dir)
        {
            for (int d = 1; d <= maxScanDistance; d++)
            {
                var pos   = new Vector3(
                    origin.x + dir.x * d,
                    origin.y + dir.y * d,
                    origin.z + dir.z * d);
                byte block = chunkManager.GetBlockAt(pos);

                if (block != (byte)BlockType.Air &&
                    block != (byte)BlockType.Water &&
                    block != 0)
                    return d;
            }
            return maxScanDistance; // 벽 없음 → 최대값
        }

        void SetTargetBySize(float size)
        {
            if      (size < 3f)  SetTarget(smallRoom,  smallDecay,  smallMix);
            else if (size < 8f)  SetTarget(medRoom,    medDecay,    medMix);
            else if (size < 16f) SetTarget(largeRoom,  largeDecay,  largeMix);
            else                 SetTarget(hugeRoom,   hugeDecay,   hugeMix);
        }

        void SetTarget(float room, float decay, float mix)
        {
            _targetRoom  = room;
            _targetDecay = decay;
            _targetMix   = mix;
        }

        void ApplyToMixer(float room, float decay, float mix)
        {
            if (audioMixer == null) return;
            audioMixer.SetFloat(roomParam,  room);
            audioMixer.SetFloat(decayParam, decay);
            audioMixer.SetFloat(mixParam,   mix);
        }

        // ──────────────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────────────

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, maxScanDistance * 0.3f);
        }
#endif
    }
}