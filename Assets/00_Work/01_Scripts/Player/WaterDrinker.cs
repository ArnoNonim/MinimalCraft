using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Sound;
using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    /// <summary>
    /// 물 블록을 바라보고 웅크린 상태에서 상호작용 키 누르면 목마름 회복.
    /// 팝업 제어 없음 — InteractIndicator가 담당.
    /// </summary>
    public class WaterDrinker : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerInputSO  playerInput;
        [SerializeField] private PlayerStats    playerStats;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("설정")]
        [SerializeField] private int   drinkAmount   = 4;
        [SerializeField] private float drinkCooldown = 1f;

        private float _lastDrinkTime = -999f;

        private void OnEnable()  => playerInput.OnInteractKeyDown += OnInteract;
        private void OnDisable() => playerInput.OnInteractKeyDown -= OnInteract;

        private void OnInteract()
        {
            if (Time.time - _lastDrinkTime < drinkCooldown) return;
            if (!playerMovement.IsCrouching)                return;
            if (!IsWaterInSight())                           return;

            playerStats.Drink(drinkAmount);
            _lastDrinkTime = Time.time;
            SoundManager.Instance.PlaySFXAt("Drink", transform.parent);
        }

        public bool IsWaterInSight()
        {
            Ray ray = Camera.main.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

            for (float dist = 0.5f; dist <= 5f; dist += 0.25f)
            {
                Vector3 checkPos = ray.origin + ray.direction * dist;
                byte    block    = ChunkSystem.Chunk.ChunkManager.Instance.GetBlockAt(checkPos);

                if (block == (byte)BlockType.Water) return true;
                if (block != (byte)BlockType.Air)   return false;
            }
            return false;
        }
    }
}