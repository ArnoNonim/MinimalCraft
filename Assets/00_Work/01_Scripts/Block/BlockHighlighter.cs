using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    /// <summary>
    /// 레이캐스트로 바라보는 블록 위치/타입을 제공.
    /// 팝업 제어 없음 — InteractIndicator가 담당.
    /// </summary>
    public class BlockHighlighter : MonoBehaviour
    {
        [Header("참조")]
        public Camera        playerCamera;
        public ChunkManager  chunkManager;

        [Header("설정")]
        public float     rayDistance = 5f;
        public LayerMask blockLayer;

        private Vector3 _highlightedBlockPos;
        private Vector3 _lastHitNormal;
        private bool    _hasTarget;

        void Update()
        {
            Ray ray = playerCamera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, blockLayer))
            {
                Vector3    blockPos = hit.point - hit.normal * 0.4f;
                Vector3Int cell     = Vector3Int.FloorToInt(blockPos);

                _highlightedBlockPos = new Vector3(
                    cell.x + 0.5f, cell.y + 0.5f, cell.z + 0.5f);

                _lastHitNormal = new Vector3(
                    Mathf.Round(hit.normal.x),
                    Mathf.Round(hit.normal.y),
                    Mathf.Round(hit.normal.z));

                _hasTarget = true;
            }
            else
            {
                _hasTarget     = false;
                _lastHitNormal = Vector3.zero;
            }
        }

        public bool TryGetTargetBlock(out Vector3 blockPos)
        {
            blockPos = _highlightedBlockPos;
            return _hasTarget;
        }

        public bool TryGetTargetBlock(out Vector3 blockPos, out BlockType blockType)
        {
            blockPos  = _highlightedBlockPos;
            blockType = BlockType.Air;

            if (!_hasTarget) return false;

            blockType = (BlockType)chunkManager.GetBlockAt(_highlightedBlockPos);
            return true;
        }

        public bool TryGetPlaceBlock(out Vector3 placePos)
        {
            placePos = Vector3.zero;
            if (!_hasTarget) return false;

            Vector3    placePosRaw = _highlightedBlockPos + _lastHitNormal;
            Vector3Int cell        = Vector3Int.FloorToInt(placePosRaw);

            placePos = new Vector3(cell.x + 0.5f, cell.y + 0.5f, cell.z + 0.5f);
            return true;
        }
    }
}