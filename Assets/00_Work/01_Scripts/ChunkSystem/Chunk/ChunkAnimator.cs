using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public class ChunkAnimator : MonoBehaviour
    {
        [Header("애니메이션 설정")]
        public float startOffsetY  = -20f;
        public float animDuration  =  0.4f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3       _targetPos;
        private Vector3       _startPos;
        private float         _timer;
        private bool          _isAnimating;
        private System.Action _onComplete;

        public void PlaySpawn()
        {
            _targetPos         = transform.position;
            _startPos          = _targetPos + Vector3.up * startOffsetY;
            transform.position = _startPos;
            _timer             = 0f;
            _isAnimating       = true;
            _onComplete        = null;
        }

        public void PlayDespawn(System.Action onComplete)
        {
            _startPos    = transform.position;
            _targetPos   = _startPos + Vector3.up * startOffsetY;
            _timer       = 0f;
            _isAnimating = true;
            _onComplete  = onComplete;
        }

        private void Update()
        {
            if (!_isAnimating) return;

            // Time.timeScale 영향을 받지 않도록 unscaledDeltaTime 사용
            _timer += Time.unscaledDeltaTime;
            float t      = Mathf.Clamp01(_timer / animDuration);
            float curved = curve.Evaluate(t);

            transform.position = Vector3.Lerp(_startPos, _targetPos, curved);

            if (t >= 1f)
            {
                _isAnimating       = false;
                transform.position = _targetPos;
                _onComplete?.Invoke();
                _onComplete = null;
            }
        }
    }
}