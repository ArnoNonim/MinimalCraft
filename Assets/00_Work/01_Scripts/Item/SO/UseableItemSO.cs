using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    /// <summary>
    /// 사용 가능한 아이템 베이스 SO.
    /// 채널링 시간이 0이면 즉시 발동.
    /// </summary>
    public abstract class UsableItemSO : ItemSO
    {
        [Header("손 메시 설정")]
        public Texture2D itemTexture;
        public float     meshThickness = 0.05f;
        
        [Header("사용 설정")]
        [Tooltip("채널링 시간 (초). 0이면 즉시 발동")]
        public float channelDuration = 3f;
        [Tooltip("사용 후 아이템 소모 여부")]
        public bool  consumeOnUse    = true;

        /// <summary>채널링 완료 시 호출. context는 사용 주체 GameObject.</summary>
        public abstract void OnUse(GameObject user);
    }
}