using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.UI;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    [CreateAssetMenu(fileName = "ArtifactItem", menuName = "Item/ArtifactItem")]
    public class ArtifactItemSO : UsableItemSO
    {
        [Header("렌더링")]
        public GameObject handPrefab;  // 손에 들 때 쓸 작은 프리팹
        
        public override void OnUse(GameObject user)
        {
            UIEnding.Instance?.Show();
        }
    }
}