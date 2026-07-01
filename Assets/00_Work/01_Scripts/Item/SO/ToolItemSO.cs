using _00_Work._01_Scripts.Tool;
using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "ToolItem", menuName = "Item/ToolItem", order = 0)]
    public class ToolItemSO : ItemSO
    {
        public ToolDataSO toolData;
        public Texture2D  toolTexture;
        public float      meshThickness = 0.05f;
    }
}