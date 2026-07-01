using TMPro;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    /// <summary>
    /// 로딩 화면 열릴 때 랜덤 팁 1개 표시
    /// </summary>
    public class LoadingTips : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private TMP_Text tipText;

        [Header("팁 목록")]
        [TextArea(2, 4)]
        [SerializeField] private string[] tips =
        {
            "상승 부하는 밑으로 내려갈 수록 그 부작용이 강해진다.",
            "웅크린 상태로 물가에 가면 물을 마실 수 있다.",
            "심층부에는 오래된 문명의 흔적이 있다고 전해진다.",
            "나뭇잎을 부수면 가끔 사과가 나온다.",
            "귀환석을 사용하면 스폰 지점으로 돌아갈 수 있다.",
            "화로에 연료를 넣으면 광석을 제련할 수 있다.",
            "허기와 목마름이 일정량 이상 차 있으면 체력이 자연 회복된다.",
            "도구 레벨이 부족하면 상위 블록은 채굴할 수 없다.",
            "깊은 곳일수록 귀한 자원이 있지만 돌아오기 힘들다.",
        };

        private void Awake()
        {
            if (tipText != null) tipText.alpha = 0f;
        }

        public void StartTips()
        {
            if (tipText == null || tips == null || tips.Length == 0) return;
            tipText.text  = tips[Random.Range(0, tips.Length)];
            tipText.alpha = 1f;
        }

        public void StopTips()
        {
            if (tipText != null) tipText.alpha = 0f;
        }
    }
}