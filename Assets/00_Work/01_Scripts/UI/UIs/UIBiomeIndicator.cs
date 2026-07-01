using _00_Work._01_Scripts.ChunkSystem.Biome;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIBiomeIndicator : MonoBehaviour
    {
        [Header("참조")]
        public TMP_Text  biomeText;
        public Transform playerTransform;

        [Header("설정")]
        public float checkInterval = 1f;   // 바이옴 체크 주기
        public float fadeInDuration  = 0.5f;
        public float fadeOutDuration = 1f;
        public float displayDuration = 2f; // 페이드아웃 전 대기

        private BiomeType    _currentBiome = (BiomeType)(-1);
        private float        _checkTimer;
        private MotionHandle _fadeHandle;
        private CanvasGroup  _canvasGroup;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>()
                        ?? gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
        }

        void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < checkInterval) return;
            _checkTimer = 0f;

            CheckBiome();
        }

        void CheckBiome()
        {
            float worldX = playerTransform.position.x;
            float worldZ = playerTransform.position.z;

            BiomeType biome = BiomeGenerator.GetBiome(worldX, worldZ);
            if (biome == _currentBiome) return;

            _currentBiome  = biome;
            biomeText.text = GetBiomeName(biome);

            ShowBiome();
        }

        void ShowBiome()
        {
            if (_fadeHandle.IsActive())
                _fadeHandle.Cancel();

            _fadeHandle = LMotion
                .Create(0f, 1f, fadeInDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => StartFadeOut())
                .Bind(alpha => _canvasGroup.alpha = alpha);
        }

        void StartFadeOut()
        {
            if (_fadeHandle.IsActive())
                _fadeHandle.Cancel();

            _fadeHandle = LMotion
                .Create(1f, 1f, displayDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    _fadeHandle = LMotion
                        .Create(1f, 0f, fadeOutDuration)
                        .WithEase(Ease.InCubic)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(alpha => _canvasGroup.alpha = alpha);
                })
                .Bind(_ => { });
        }

        string GetBiomeName(BiomeType biome) => biome switch
        {
            BiomeType.Plains  => "ㅡ 평원 ㅡ",
            BiomeType.Desert  => "ㅡ 사막 ㅡ",
            BiomeType.Snow    => "ㅡ 설원 ㅡ",
            BiomeType.Forest  => "ㅡ 숲 ㅡ",
            _                 => biome.ToString()
        };
    }
}