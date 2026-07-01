using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    public class BlockPlacer : MonoBehaviour
    {
        [Header("참조")]
        public PlayerInputSO playerInput;
        public ChunkManager  chunkManager;
        public BlockHighlighter highlighter;
        public UIHotbar      hotbar;
        public Animator      animator;

        [Header("설정")]
        public LayerMask playerLayer; // 플레이어 레이어 (블록 설치 방지)

        private static readonly int PlaceHash =
            Animator.StringToHash("Punch");

        void OnEnable()
        {
            playerInput.OnInteractKeyDown += OnPlace;
        }

        void OnDisable()
        {
            playerInput.OnInteractKeyDown -= OnPlace;
        }

        void OnPlace()
        {
            var stack = hotbar.GetSelectedItem();

            if (stack == null || stack.IsEmpty)
                return;

            if (stack.item is not BlockItemSO blockItem)
                return;

            // 상호작용 블록 위에는 설치 금지
            if (highlighter.TryGetTargetBlock(
                    out _,
                    out BlockType targetBlock))
            {
                if (BlockInteractionUtility.IsInteractable(targetBlock))
                    return;
            }

            if (!highlighter.TryGetPlaceBlock(out Vector3 placePos))
                return;

            byte existing = chunkManager.GetBlockAt(placePos);

            if (existing != (byte)BlockType.Air)
                return;

            if (IsOverlappingPlayer(placePos))
                return;

            chunkManager.SetBlock(
                placePos,
                blockItem.blockType);

            stack.count--;

            if (stack.count <= 0)
                stack.Clear();

            hotbar.inventory.NotifyChanged();

            animator.SetTrigger(PlaceHash);
        }

        bool IsOverlappingPlayer(Vector3 blockPos)
        {
            Vector3 center = new Vector3(
                Mathf.Floor(blockPos.x) + 0.5f,
                Mathf.Floor(blockPos.y) + 0.5f,
                Mathf.Floor(blockPos.z) + 0.5f);

            return Physics.CheckBox(
                center,
                Vector3.one * 0.4f,
                Quaternion.identity,
                playerLayer);
        }
    }
}