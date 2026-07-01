using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "MaterialItem", menuName = "Item/MaterialItem", order = 0)]
    public class MaterialItemSO : ItemSO
    {
        [Header("비주얼")]
        [Tooltip("아이콘 및 3D 메시 생성에 사용할 텍스처")]
        public Texture2D itemTexture;
 
        [Tooltip("월드 드롭 메시 두께 (얇을수록 동전형, 두꺼울수록 입체형)")]
        [Range(0.01f, 0.5f)]
        public float meshThickness = 0.05f;
    }
}