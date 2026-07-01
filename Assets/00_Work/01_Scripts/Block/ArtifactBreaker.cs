using _00_Work._01_Scripts.ChunkSystem.Structure;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Player.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    /// <summary>
    /// 유물 오브젝트 채굴 처리.
    /// BlockBreaker와 같은 공격 이벤트 사용 — IsTargetingArtifact로 우선순위 조정.
    /// </summary>
    public class ArtifactBreaker : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Camera        playerCamera;
        [SerializeField] private ItemDropper   itemDropper;

        [Header("설정")]
        public float     rayDistance  = 5f;

        // ── 프로퍼티 ────────────────────────────────────────────────
        public bool           IsBreaking         => _isBreaking;
        public bool           IsTargetingArtifact { get; private set; }
        public ArtifactObject CurrentTarget      => _target;
        public float          Progress           => _target != null ? _progress / _target.mineTime : 0f;

        // ── 상태 ────────────────────────────────────────────────────
        private ArtifactObject _target;
        private float          _progress;
        private bool           _isBreaking;

        // ──────────────────────────────────────────────

        
        private void OnEnable()
        {
            playerInput.OnAttackKeyDown += OnAttackDown;
            playerInput.OnAttackKeyUp   += OnAttackUp;
        }

        private void OnDisable()
        {
            playerInput.OnAttackKeyDown -= OnAttackDown;
            playerInput.OnAttackKeyUp   -= OnAttackUp;
            StopBreaking();
        }

        private void Update()
        {
            var ray = playerCamera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

            bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, rayDistance, LayerMask.GetMask("Artifact"));
    
            // ← 먼저 세팅
            IsTargetingArtifact = hit && hitInfo.collider.GetComponentInParent<ArtifactObject>() != null;

            if (!_isBreaking || !IsTargetingArtifact)
            {
                if (!IsTargetingArtifact) StopBreaking();
                return;
            }

            var artifact = hitInfo.collider.GetComponentInParent<ArtifactObject>();
            if (artifact != _target) { _target = artifact; _progress = 0f; }

            _progress += Time.unscaledDeltaTime; // ← timeScale 무관하게
            if (_progress >= _target.mineTime) CompleteBreaking();
        }

        // ──────────────────────────────────────────────

        private void OnAttackDown() => _isBreaking = true;

        private void OnAttackUp() => StopBreaking();

        private void StopBreaking()
        {
            _isBreaking = false;
            _progress   = 0f;
            _target     = null;
        }

        private void CompleteBreaking()
        {
            _target?.OnMined(itemDropper);
            StopBreaking();
        }
    }
}
