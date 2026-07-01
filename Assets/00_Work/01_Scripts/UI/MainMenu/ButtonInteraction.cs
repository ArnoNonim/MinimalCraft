using _00_Work._01_Scripts.Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    public class ButtonInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scaleUp = 1.1f;
        public float duration = 0.2f;

        private Button _button;
        
        private Vector3 _originalScale;
        
        // 현재 실행 중인 모션을 추적하고 취소하기 위한 핸들
        private MotionHandle _scaleMotion;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }
        
        void Start()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 1. 기존에 돌고 있던 트윈 취소 (DoTween의 DOKill 역할)
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();

            // 2. LitMotion 생성 및 실행
            _scaleMotion = LMotion.Create(transform.localScale, _originalScale * scaleUp, duration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(transform) // transform.localScale에 바인딩
                .AddTo(gameObject);          // 오브젝트 파괴 시 자동 취소 안전장치
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();

            _scaleMotion = LMotion.Create(transform.localScale, _originalScale, duration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(transform)
                .AddTo(gameObject);
        }

        public void OnClick()
        {
            SoundManager.Instance.PlaySFX("BtnClick");
            
            // DoTween.KillAll() 대신, 현재 이 버튼에 연결된 모션만 취소합니다.
            // (전역 취소를 원하신다면 MotionTracker를 쓰거나 모든 핸들을 관리해야 합니다)
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
        
            // 클릭 연출 시작 (조금 더 커짐)
            _scaleMotion = LMotion.Create(transform.localScale, _originalScale * (scaleUp + 0.1f), duration)
                .WithEase(Ease.OutBack)
                .WithOnComplete(() =>
                {
                    // 클릭 후 Hover 크기로 돌아가는 연출 (OnComplete 콜백 내부에서 체이닝)
                    _scaleMotion = LMotion.Create(transform.localScale, _originalScale * scaleUp, duration * 4)
                        .WithEase(Ease.OutBack)
                        .BindToLocalScale(transform)
                        .AddTo(gameObject);
                })
                .BindToLocalScale(transform)
                .AddTo(gameObject);
        }

        // 오브젝트가 비활성화되거나 파괴될 때 모션 정리
        private void OnDestroy()
        {
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
        }
    }
}
