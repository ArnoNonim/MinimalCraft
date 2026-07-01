using UnityEngine;

namespace _00_Work._01_Scripts.Player.SO
{
    [CreateAssetMenu(fileName = "PlayerStat", menuName = "SO/PlayerStat")]
    public class PlayerStatSO : ScriptableObject
    {
        [Header("이동 속도")]
        public float walkSpeed   = 5f;
        public float sprintSpeed = 9f;
        public float crouchSpeed = 2.5f;
        
        [Header("점프")]
        public float jumpForce = 5f;

        [Header("상태")]
        public int maxHealth = 20;
        public int curHealth;
        public int maxEfficiency = 20;
        public int curEfficiency;
        public int maxHunger = 20;
        public int curHunger;
        public int maxThirsty = 20;
        public int curThirsty;
        [Range(20f, 50f)]
        public float temperature = 36.5f;
    }
}