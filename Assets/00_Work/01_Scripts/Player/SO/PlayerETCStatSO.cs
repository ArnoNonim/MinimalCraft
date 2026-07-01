using UnityEngine;

namespace _00_Work._01_Scripts.Player.SO
{
    [CreateAssetMenu(fileName = "PlayerETCStat", menuName = "SO/PlayerETCStat", order = 0)]
    public class PlayerETCStatSO : ScriptableObject
    {
        [Header("물리")]
        public float groundDrag = 6f;
        public float airDrag    = 1f;
        
        [Header("카메라 감도")]
        public float sensitivity = 0.2f;
        public float smoothSpeed = 15f;
        public bool useSmoothSpeed = true;

        [Header("Bobbing - 걷기")]
        public float walkBobbingSpeed   = 8f;
        public float walkBobbingAmountX = 0.03f;
        public float walkBobbingAmountY = 0.05f;

        [Header("Bobbing - 달리기")]
        public float sprintBobbingSpeed   = 14f;
        public float sprintBobbingAmountX = 0.06f;
        public float sprintBobbingAmountY = 0.09f;

        [Header("Bobbing - 기타")]
        public float bobbingReturnSpeed = 8f;

    }
}