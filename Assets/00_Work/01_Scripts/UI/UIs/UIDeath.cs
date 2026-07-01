using _00_Work._01_Scripts.Player;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    /// <summary>
    /// 사망 UI — 페이드 인/아웃만 담당
    /// 리스폰 로직은 RespawnManager에서 처리
    /// </summary>
    public class UIDeath : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("설정")]
        [SerializeField] private float fadeInDuration  = 0.8f;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeOutDuration = 0.8f;

        void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void Show()
        {
            if (canvasGroup == null) return;

            canvasGroup.blocksRaycasts = true;

            // 페이드 인
            LMotion.Create(0f, 1f, fadeInDuration)
                .WithEase(Ease.OutCubic)
                .BindToAlpha(canvasGroup)
                .AddTo(gameObject);

            // displayDuration 후 리스폰 트리거
            Invoke(nameof(TriggerRespawn), fadeInDuration + displayDuration);
        }

        void TriggerRespawn()
        {
            // 리스폰 로직은 RespawnManager에 위임
            RespawnManager.Instance?.Respawn();

            // 페이드 아웃
            LMotion.Create(1f, 0f, fadeOutDuration)
                .WithEase(Ease.InCubic)
                .BindToAlpha(canvasGroup)
                .AddTo(gameObject);

            LMotion.Create(0f, 0f, fadeOutDuration)
                .WithOnComplete(() => canvasGroup.blocksRaycasts = false)
                .Bind(_ => { })
                .AddTo(gameObject);
        }
    }
}