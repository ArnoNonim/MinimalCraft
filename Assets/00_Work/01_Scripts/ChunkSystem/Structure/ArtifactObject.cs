using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    /// <summary>
    /// 유물 Prefab에 붙는 컴포넌트.
    /// 내구도, 드롭 아이템 정보 보유.
    /// </summary>
    public class ArtifactObject : MonoBehaviour
    {
        [Header("채굴 설정")]
        [Tooltip("채굴에 필요한 총 시간 (초)")]
        public float mineTime = 10f;

        [Header("드롭")]
        public ItemSO  dropItem;
        public int     dropCount = 1;

        public bool IsMined { get; private set; }

        // ──────────────────────────────────────────────

        /// <summary>채굴 완료 시 호출 — 아이템 드롭 후 오브젝트 제거</summary>
        public void OnMined(ItemDropper dropper)
        {
            if (IsMined) return;
            IsMined = true;

            if (dropper != null && dropItem != null)
                dropper.DropItem(dropItem, dropCount, transform.position + Vector3.up * 0.5f);

            Destroy(gameObject);
        }
    }
}