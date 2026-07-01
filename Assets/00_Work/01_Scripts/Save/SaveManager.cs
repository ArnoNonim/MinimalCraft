using System.IO;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI;
using UnityEngine;

namespace _00_Work._01_Scripts.Save
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private PlayerStatSO stat;
        [SerializeField] private Transform    playerTransform;
        [SerializeField] private Inventory    inventory;
        [SerializeField] private UIBars       uiBars;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "PlayerSave.json");

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ──────────────────────────────────────────────
        // 저장
        // ──────────────────────────────────────────────

        public void Save()
        {
            var data = new PlayerSaveData
            {
                posX = playerTransform.position.x,
                posY = playerTransform.position.y,
                posZ = playerTransform.position.z,

                curHealth     = stat.curHealth,
                curEfficiency = stat.curEfficiency,
                curHunger     = stat.curHunger,
                curThirsty    = stat.curThirsty,
                temperature   = stat.temperature,

                slots = SerializeInventory(),
            };

            if (FurnaceManager.Instance != null)
            {
                var furnaceData = FurnaceManager.Instance.GetSaveData();
                data.furnaceData = JsonUtility.ToJson(furnaceData);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        // ──────────────────────────────────────────────
        // 불러오기
        // ──────────────────────────────────────────────

        /// <summary>저장 파일의 위치만 읽어 반환 — Transform 건드리지 않음</summary>
        public Vector3 PeekPosition()
        {
            var data = ReadSaveData();
            return data == null ? Vector3.zero : new Vector3(data.posX, data.posY, data.posZ);
        }

        /// <summary>
        /// 저장된 위치를 즉시 적용.
        /// isKinematic 여부에 관계없이 velocity 리셋 보장.
        /// </summary>
        public bool LoadPositionOnly()
        {
            var data = ReadSaveData();
            if (data == null) return false;

            playerTransform.position = new Vector3(data.posX, data.posY, data.posZ);

            var rb = playerTransform.GetComponent<Rigidbody>()
                     ?? playerTransform.GetComponentInChildren<Rigidbody>();

            // kinematic 상태에서 velocity 세팅 불가 — 비kinematic일 때만 클리어
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            return true;
        }

        public bool LoadStatsAndInventory()
        {
            var data = ReadSaveData();
            if (data == null) return false;

            stat.curHealth     = Mathf.Clamp(data.curHealth,     0, stat.maxHealth);
            stat.curEfficiency = Mathf.Clamp(data.curEfficiency, 0, stat.maxEfficiency);
            stat.curHunger     = Mathf.Clamp(data.curHunger,     0, stat.maxHunger);
            stat.curThirsty    = Mathf.Clamp(data.curThirsty,    0, stat.maxThirsty);
            stat.temperature   = Mathf.Clamp(data.temperature,   20f, 50f);

            if (!string.IsNullOrEmpty(data.furnaceData) && FurnaceManager.Instance != null)
            {
                var furnaceData = JsonUtility.FromJson<FurnaceManager.SaveData>(data.furnaceData);
                FurnaceManager.Instance.LoadSaveData(furnaceData);
            }

            DeserializeInventory(data.slots);
            uiBars?.UpdateBars();
            return true;
        }

        public bool Load()
        {
            if (!LoadPositionOnly())      return false;
            if (!LoadStatsAndInventory()) return false;
            return true;
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        public bool HasSave() => File.Exists(SavePath);

        // ──────────────────────────────────────────────
        // 직렬화
        // ──────────────────────────────────────────────

        private PlayerSaveData ReadSaveData()
        {
            if (!File.Exists(SavePath)) return null;
            return JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(SavePath));
        }

        private SlotSaveData[] SerializeInventory()
        {
            var result = new SlotSaveData[inventory.slots.Length];

            for (int i = 0; i < inventory.slots.Length; i++)
            {
                var slot = inventory.slots[i];
                var save = new SlotSaveData { durability = -1 };

                if (!slot.IsEmpty && slot.item != null)
                {
                    // ── 핵심 수정 ──────────────────────────────────────────────
                    // AssetDatabase 대신 ItemSO 자체에 구워진 ResourcePath 사용
                    // 빌드/에디터 모두 동일하게 동작
                    save.itemPath   = slot.item.ResourcePath;
                    save.count      = slot.count;
                    save.instanceId = slot.instanceId;

                    if (slot.HasDurability)
                        save.durability = slot.durability;

                    // ResourcePath가 비어있으면 에디터 경고
                    if (string.IsNullOrEmpty(save.itemPath))
                        Debug.LogError(
                            $"[SaveManager] {slot.item.name}의 ResourcePath가 비어있음!\n" +
                            "ItemSO Inspector에서 우클릭 → '경로 자동 세팅' 실행 필요");
                }

                result[i] = save;
            }

            return result;
        }

        private void DeserializeInventory(SlotSaveData[] savedSlots)
        {
            if (savedSlots == null) return;

            for (int i = 0; i < inventory.slots.Length; i++)
                inventory.slots[i].Clear();

            int len = Mathf.Min(savedSlots.Length, inventory.slots.Length);

            for (int i = 0; i < len; i++)
            {
                var save = savedSlots[i];
                if (string.IsNullOrEmpty(save.itemPath)) continue;

                var item = Resources.Load<ItemSO>(save.itemPath);
                if (item == null)
                {
                    Debug.LogError($"[SaveManager] 아이템 로드 실패: '{save.itemPath}'\n" +
                                   "경로가 Resources 폴더 기준 상대경로인지 확인");
                    continue;
                }

                var stack = new ItemStack(item, save.count);

                if (save.instanceId != 0)
                    stack.instanceId = save.instanceId;

                if (save.durability >= 0)
                    stack.durability = save.durability;

                inventory.slots[i] = stack;
            }

            inventory.NotifyChanged();
        }
    }
}