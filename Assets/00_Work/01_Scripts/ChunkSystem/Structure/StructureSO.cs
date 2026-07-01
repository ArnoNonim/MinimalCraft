using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    [CreateAssetMenu(menuName = "Minecraft/Structure")]
    public class StructureSO : ScriptableObject
    {
        public string structureName;

        [System.Serializable]
        public class StructureBlock
        {
            public Vector3Int offset; // 루트 기준 상대 위치
            public BlockType  block;
        }

        public List<StructureBlock> blocks = new List<StructureBlock>();
    }
}