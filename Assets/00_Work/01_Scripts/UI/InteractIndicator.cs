using _00_Work._01_Scripts.Block;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.Player;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    /// <summary>
    /// 매 프레임 상호작용 가능 여부를 통합 판단해 UIInteractPopup 하나를 제어.
    /// 팝업 제어 책임은 이 컴포넌트에만 있음.
    /// </summary>
    public class InteractIndicator : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private BlockHighlighter blockHighlighter;
        [SerializeField] private PlayerMovement   playerMovement;
        [SerializeField] private UIInteractPopup  interactPopup;

        private void Update()
        {
            // ── 물 마시기 (웅크림 + 물 블록) — 최우선 ────────────────────
            if (playerMovement.IsCrouching && IsWaterInSight())
            {
                interactPopup.Show("물 마시기");
                return;
            }

            // ── 상호작용 가능 블록 ────────────────────────────────────────
            if (blockHighlighter.TryGetTargetBlock(out _, out BlockType blockType)
                && BlockInteractionUtility.IsInteractable(blockType))
            {
                interactPopup.Show(BlockInteractionUtility.GetPromptText(blockType));
                return;
            }

            // ── 아무것도 없으면 숨김 ──────────────────────────────────────
            interactPopup.Hide();
        }

        private bool IsWaterInSight()
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