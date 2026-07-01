using System;
using UnityEngine;

namespace _00_Work._01_Scripts.Save
{
    /// <summary>
    /// 플레이어 저장 데이터 컨테이너
    /// JSON 직렬화 가능한 순수 C# 클래스
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        // ── 위치 ────────────────────────────────────────────────────────
        public float posX;
        public float posY;
        public float posZ;

        // ── 스탯 ────────────────────────────────────────────────────────
        public int   curHealth;
        public int   curEfficiency;
        public int   curHunger;
        public int   curThirsty;
        public float temperature;

        // ── 인벤토리 ─────────────────────────────────────────────────────
        public SlotSaveData[] slots;
        
        public string furnaceData;
    }

    [Serializable]
    public class SlotSaveData
    {
        /// <summary>ScriptableObject 에셋 경로 (Resources 폴더 기준)</summary>
        public string itemPath;
        public int    count;
        public int    instanceId;
        public int    durability; // -1 = 내구도 없음
    }
}