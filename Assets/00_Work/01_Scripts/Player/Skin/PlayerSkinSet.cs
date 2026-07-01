using UnityEngine;

namespace _00_Work._01_Scripts.Player.Skin
{
    public class PlayerSkinSet : MonoBehaviour
    {
        public bool isSlim = false;
        
        public MeshRenderer lArm;
        public MeshRenderer rArm;
        public MeshRenderer lSleeve;
        public MeshRenderer rSleeve;
        public MeshRenderer lArmSlim;
        public MeshRenderer rArmSlim;
        public MeshRenderer lSleeveSlim;
        public MeshRenderer rSleeveSlim;
        
        private void OnValidate()
        {
            // Unity 에디터가 현재 프레임의 처리를 마친 후, 안전하게 실행하도록 예약합니다.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += UpdateSkinVisibility;
#endif
        }

        private void UpdateSkinVisibility()
        {
            // 에디터 플레이를 종료하거나 오브젝트가 파괴되었을 때 발생할 수 있는 Null 예외 방지
            if (this == null) return; 

            lArm.enabled = !isSlim;
            rArm.enabled = !isSlim;
            lSleeve.enabled = !isSlim;
            rSleeve.enabled = !isSlim;
            
            lArmSlim.enabled = isSlim;
            rArmSlim.enabled = isSlim;
            lSleeveSlim.enabled = isSlim;
            rSleeveSlim.enabled = isSlim;
        }
    }
}