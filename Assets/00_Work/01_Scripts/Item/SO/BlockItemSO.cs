using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "BlockItem", menuName = "Item/Block", order = 0)]
    public class BlockItemSO : ItemSO
    {
        public BlockType blockType; // 설치 시 어떤 블록인지
    }
}