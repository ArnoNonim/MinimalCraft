using LitMotion;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    /// <summary>
    /// UI 텍스트 위아래 귀엽게 둥둥 이펙트
    /// RectTransform anchoredPosition.y를 LitMotion으로 루핑
    /// </summary>
    public class FloatTextEffect : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float amplitude = 6f;    // 상하 진폭 (px)
        [SerializeField] private float duration  = 1.2f;  // 1사이클 시간
        [SerializeField] private Ease  ease      = Ease.InOutSine;
        [SerializeField] private float randomOffset = 0f; // 여러 개일 때 위상 차이

        private RectTransform _rect;
        private Vector2       _originPos;
        private MotionHandle  _handle;

        private void Awake()
        {
            _rect      = GetComponent<RectTransform>();
            _originPos = _rect.anchoredPosition;
        }

        private void OnEnable()
        {
            StartFloat();
        }

        private void OnDisable()
        {
            if (_handle.IsActive()) _handle.Cancel();
            _rect.anchoredPosition = _originPos;
        }

        private void OnDestroy()
        {
            if (_handle.IsActive()) _handle.Cancel();
        }

        private void StartFloat()
        {
            if (_handle.IsActive()) _handle.Cancel();

            // 위상 오프셋 적용
            float offset = randomOffset > 0f
                ? Random.Range(-randomOffset, randomOffset)
                : 0f;

            _handle = LMotion
                .Create(_originPos.y - amplitude, _originPos.y + amplitude, duration)
                .WithEase(ease)
                .WithLoops(-1, LoopType.Yoyo)
                .WithDelay(offset)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(y =>
                {
                    var pos = _rect.anchoredPosition;
                    pos.y   = y;
                    _rect.anchoredPosition = pos;
                });
        }
    }
}