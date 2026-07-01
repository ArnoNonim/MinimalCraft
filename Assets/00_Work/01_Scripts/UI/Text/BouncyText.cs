using System.Collections;
using LitMotion;
using TMPro;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.Text
{
    [RequireComponent(typeof(TMP_Text))]
    public class BouncyText : MonoBehaviour
    {
        [Header("바운스 설정")]
        [SerializeField] private float bounceHeight   = 12f;
        [SerializeField] private float bounceDuration = 0.35f;
        [SerializeField] private float stagger        = 0.06f;
        [SerializeField] private bool  loop           = true;
        [SerializeField] private float loopDelay      = 1.2f;

        private TMP_Text       _text;
        private Coroutine      _coroutine;
        private MotionHandle[] _handles = new MotionHandle[0];
        private float[]        _offsets = new float[0];

        // 원본 버텍스 캐시 — 메시별로 저장
        private Vector3[][] _baseVertices;

        void Awake()     => _text = GetComponent<TMP_Text>();
        void OnEnable()  => Play();
        void OnDisable() => Stop();

        public void Play()
        {
            Stop();
            _coroutine = StartCoroutine(RunBounce());
        }

        public void Stop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            CancelHandles();
            ResetToBase();
        }

        IEnumerator RunBounce()
        {
            while (true)
            {
                _text.ForceMeshUpdate();

                int charCount = _text.textInfo.characterCount;
                if (charCount == 0) { yield return null; continue; }

                // ── 원본 버텍스 캐시 저장 ──────────────────────────────
                CacheBaseVertices();

                CancelHandles();
                _handles = new MotionHandle[charCount];
                _offsets = new float[charCount];

                int visibleOrder = 0;

                for (int i = 0; i < charCount; i++)
                {
                    if (!_text.textInfo.characterInfo[i].isVisible) continue;

                    int   ci    = i;
                    float delay = visibleOrder * stagger;
                    visibleOrder++;

                    _handles[ci] = LMotion
                        .Create(0f, bounceHeight, bounceDuration * 0.45f)
                        .WithDelay(delay)
                        .WithEase(Ease.OutQuad)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .WithOnComplete(() => BounceDown(ci))
                        .Bind(y => { _offsets[ci] = y; ApplyOffsets(); });
                }

                float totalDuration = (visibleOrder - 1) * stagger
                                    + bounceDuration + loopDelay;

                yield return new WaitForSecondsRealtime(
                    loop ? totalDuration
                         : bounceDuration + (visibleOrder - 1) * stagger);

                if (!loop) yield break;
            }
        }

        void BounceDown(int ci)
        {
            if (ci >= _handles.Length) return;

            _handles[ci] = LMotion
                .Create(bounceHeight, 0f, bounceDuration * 0.55f)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(y => { _offsets[ci] = y; ApplyOffsets(); });
        }

        // ──────────────────────────────────────────────
        // 버텍스 처리
        // ──────────────────────────────────────────────

        /// <summary>ForceMeshUpdate 직후 원본 버텍스를 캐시에 복사</summary>
        void CacheBaseVertices()
        {
            var meshInfo = _text.textInfo.meshInfo;
            _baseVertices = new Vector3[meshInfo.Length][];

            for (int m = 0; m < meshInfo.Length; m++)
            {
                var src = meshInfo[m].vertices;
                _baseVertices[m] = new Vector3[src.Length];
                src.CopyTo(_baseVertices[m], 0);
            }
        }

        /// <summary>캐시된 원본 버텍스 기준으로 오프셋 적용</summary>
        void ApplyOffsets()
        {
            if (_baseVertices == null) return;

            var textInfo = _text.textInfo;

            // 캐시 기준으로 버텍스 초기화
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                if (m >= _baseVertices.Length) break;
                var src = _baseVertices[m];
                var dst = textInfo.meshInfo[m].vertices;
                for (int v = 0; v < src.Length && v < dst.Length; v++)
                    dst[v] = src[v];
            }

            // 오프셋 적용
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (i >= _offsets.Length) break;
                if (_offsets[i] == 0f) continue;

                var ci = textInfo.characterInfo[i];
                if (!ci.isVisible) continue;

                var verts = textInfo.meshInfo[ci.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                    verts[ci.vertexIndex + v].y += _offsets[i];
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        void ResetToBase()
        {
            if (_text == null || _baseVertices == null) return;

            var textInfo = _text.textInfo;
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                if (m >= _baseVertices.Length) break;
                var src = _baseVertices[m];
                var dst = textInfo.meshInfo[m].vertices;
                for (int v = 0; v < src.Length && v < dst.Length; v++)
                    dst[v] = src[v];
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            _baseVertices = null;
        }

        void CancelHandles()
        {
            foreach (var h in _handles)
                if (h.IsActive()) h.Cancel();
            _handles = new MotionHandle[0];
        }
    }
}