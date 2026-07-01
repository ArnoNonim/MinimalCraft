using _00_Work._01_Scripts.Block;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private BlockHighlighter highlighter;

        [Header("UI")]
        [SerializeField] private UIWorkbench workbenchUI;
        [SerializeField] private UIFurnace furnaceUI;

        void OnEnable()
        {
            playerInput.OnInteractKeyDown += OnInteract;
        }

        void OnDisable()
        {
            playerInput.OnInteractKeyDown -= OnInteract;
        }

        void OnInteract()
        {
            if (!highlighter.TryGetTargetBlock(
                    out _,
                    out BlockType blockType))
                return;

            switch (blockType)
            {
                case BlockType.Workbench:
                    OpenWorkbench();
                    break;

                case BlockType.Furnace:
                    OpenFurnace();
                    break;
            }
        }

        void OpenWorkbench()
        {
            if (workbenchUI == null)
                return;

            if (workbenchUI.IsOpen)
                return;

            UIManager.Instance?.CloseAllPopups();

            workbenchUI.Open();
        }
        
        void OpenFurnace()
        {
            if (furnaceUI == null) return;
            if (furnaceUI.IsOpen) return;

            if (!highlighter.TryGetTargetBlock(out Vector3 blockPos, out _)) return;

            UIManager.Instance?.CloseAllPopups();

            // 좌표 기반으로 화로 열기
            var pos = Vector3Int.FloorToInt(blockPos);
            furnaceUI.OpenAt(pos);
        }
    }
}