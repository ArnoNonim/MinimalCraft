using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.Player
{
    public class DamageVignette : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Image vignetteImage;

        [Header("이펙트 설정")]
        [Tooltip("페이드인 시간 (초)")]
        [SerializeField] private float fadeInDuration  = 0.08f;

        [Tooltip("페이드아웃 시간 (초)")]
        [SerializeField] private float fadeOutDuration = 0.6f;

        [Tooltip("데미지 1일 때 최대 알파")]
        [SerializeField] private float minAlpha = 0.25f;

        [Tooltip("데미지 maxHealth 이상일 때 최대 알파")]
        [SerializeField] private float maxAlpha = 0.75f;

        [Tooltip("최대 알파 기준 데미지 (이 이상이면 maxAlpha 고정)")]
        [SerializeField] private int maxDamageRef = 10;

        private MotionHandle _fadeInHandle;
        private MotionHandle _fadeOutHandle;

        private void Awake()
        {
            // 시작 시 완전 투명
            SetAlpha(0f);
        }
        
        public void PlayDamageEffect(int amount)
        {
            // 진행 중인 모션 취소
            if (_fadeInHandle.IsActive())  _fadeInHandle.Cancel();
            if (_fadeOutHandle.IsActive()) _fadeOutHandle.Cancel();

            float targetAlpha = Mathf.Lerp(
                minAlpha, maxAlpha,
                Mathf.Clamp01((float)amount / maxDamageRef));

            // 페이드인 완료 후 페이드아웃 체이닝
            _fadeInHandle = LMotion
                .Create(vignetteImage.color.a, targetAlpha, fadeInDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(StartFadeOut)
                .BindToColorA(vignetteImage);
        }

        private void StartFadeOut()
        {
            _fadeOutHandle = LMotion
                .Create(vignetteImage.color.a, 0f, fadeOutDuration)
                .WithEase(Ease.InQuad)
                .BindToColorA(vignetteImage);
        }

        private void SetAlpha(float a)
        {
            var c = vignetteImage.color;
            c.a = a;
            vignetteImage.color = c;
        }
    }
}