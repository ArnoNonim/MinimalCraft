using UnityEngine;

namespace _00_Work._01_Scripts.Tool
{
    [CreateAssetMenu(fileName = "ToolData", menuName = "SO/ToolData", order = 0)]
    public class ToolDataSO : ScriptableObject
    {
        public ToolType toolType;
        public int    miningSpeed = 1;  // 채굴 속도 배수
        public int        maxDurability = 100;

        // 도구 등급 (높을수록 더 단단한 블록 채굴 가능)
        public int toolLevel = 0;
    }
}