using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    /// <summary>
    /// WorldItem 합체 시스템
    /// 주변 동일 아이템을 자석처럼 끌어당겨 합체
    /// </summary>
    public class WorldItemMerger : MonoBehaviour
    {
        [Header("합체 설정")]
        [SerializeField] private float mergeRadius      = 1.5f;
        [SerializeField] private float mergeDelay       = 1.0f;  // 스폰 후 합체 시작 딜레이
        [SerializeField] private float attractSpeed     = 4f;    // 끌어당기는 속도
        [SerializeField] private float mergeDistance    = 0.15f; // 이 거리 이하면 합체 확정
        [SerializeField] private LayerMask itemLayer;

        [Header("파티클")]
        [SerializeField] private ParticleSystem mergeParticlePrefab;

        private WorldItem  _worldItem;
        private WorldItem  _mergeTarget;   // 흡수될 대상
        private bool       _isAttracting;  // 끌려가는 중
        private bool       _isMerged;      // 이미 합체됨
        private Rigidbody  _rb;
        private float      _spawnTime;

        // ──────────────────────────────────────────────

        void Awake()
        {
            _worldItem = GetComponent<WorldItem>();
            _rb        = GetComponent<Rigidbody>();
            _spawnTime = Time.time;
        }

        void Update()
        {
            if (_isMerged) return;

            // 끌려가는 중
            if (_isAttracting && _mergeTarget != null)
            {
                AttrractToTarget();
                return;
            }

            // 합체 딜레이
            if (Time.time - _spawnTime < mergeDelay) return;

            TryFindMergeTarget();
        }

        // ──────────────────────────────────────────────
        // 대상 탐색
        // ──────────────────────────────────────────────

        void TryFindMergeTarget()
        {
            var cols = Physics.OverlapSphere(
                transform.position, mergeRadius, itemLayer);

            WorldItem closest     = null;
            float     closestDist = float.MaxValue;

            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;

                var other        = col.GetComponent<WorldItem>();
                var otherMerger  = col.GetComponent<WorldItemMerger>();

                if (other == null || otherMerger == null) continue;
                if (otherMerger._isMerged || otherMerger._isAttracting) continue;
                if (other.item != _worldItem.item) continue;
                if (Time.time - otherMerger._spawnTime < mergeDelay) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest     = other;
                }
            }

            if (closest == null) return;

            // 이 아이템이 대상 쪽으로 끌려감 (작은 쪽이 큰 쪽으로)
            var closestMerger = closest.GetComponent<WorldItemMerger>();

            // instanceId 낮은 쪽이 흡수되러 감
            if (gameObject.GetInstanceID() < closest.gameObject.GetInstanceID())
            {
                _isAttracting             = true;
                _mergeTarget              = closest;
                closestMerger._isMerged  = false; // 대상은 유지

                // 물리 끄고 키네마틱으로 이동
                if (_rb != null) _rb.isKinematic = true;
            }
        }

        // ──────────────────────────────────────────────
        // 끌려가기
        // ──────────────────────────────────────────────

        void AttrractToTarget()
        {
            if (_mergeTarget == null || _mergeTarget.gameObject == null)
            {
                // 대상이 사라짐 — 물리 복구
                CancelAttract();
                return;
            }

            float dist = Vector3.Distance(
                transform.position, _mergeTarget.transform.position);

            // 속도 — 가까울수록 빠르게
            float speed = attractSpeed * Mathf.Lerp(3f, 1f, dist / mergeRadius);

            transform.position = Vector3.MoveTowards(
                transform.position,
                _mergeTarget.transform.position,
                speed * Time.deltaTime);

            // 합체 거리 도달
            if (dist <= mergeDistance)
                CompleteMerge();
        }

        void CompleteMerge()
        {
            if (_mergeTarget == null) return;

            // 파티클
            if (mergeParticlePrefab != null)
            {
                var ps = Instantiate(
                    mergeParticlePrefab,
                    _mergeTarget.transform.position,
                    Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + 0.5f);
            }

            // 수량 흡수
            _mergeTarget.count += _worldItem.count;

            _isMerged = true;
            Destroy(gameObject);
        }

        void CancelAttract()
        {
            _isAttracting = false;
            _mergeTarget  = null;
            if (_rb != null) _rb.isKinematic = false;
        }

        // ──────────────────────────────────────────────

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, mergeRadius);
        }
    }
}