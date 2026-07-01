using System;
using System.Collections.Generic;
using _00_Work._01_Scripts.Crafting;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Save
{
    /// <summary>
    /// 월드의 모든 화로를 관리
    /// UI와 무관하게 항상 제련 틱 처리
    /// 좌표(Vector3Int)를 키로 각 화로 데이터 관리
    /// </summary>
    public class FurnaceManager : MonoBehaviour
    {
        public static FurnaceManager Instance { get; private set; }

        // 좌표별 화로 데이터
        private readonly Dictionary<Vector3Int, FurnaceData> _furnaces = new();

        // 변경 알림 — UIFurnace가 구독해서 UI 갱신
        public event Action<Vector3Int> OnFurnaceChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            foreach (var kv in _furnaces)
                TickFurnace(kv.Key, kv.Value);
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>화로 데이터 가져오기 (없으면 새로 생성)</summary>
        public FurnaceData GetOrCreate(Vector3Int pos)
        {
            if (!_furnaces.TryGetValue(pos, out var data))
            {
                data = new FurnaceData();
                _furnaces[pos] = data;
            }
            return data;
        }

        /// <summary>화로 블록 파괴 시 데이터 제거 + 아이템 드롭</summary>
        public void Remove(Vector3Int pos, ItemDropper dropper = null, Vector3 dropPos = default)
        {
            if (!_furnaces.TryGetValue(pos, out var data)) return;

            if (dropper != null)
            {
                if (data.fuel   != null && !data.fuel.IsEmpty)
                    dropper.DropItem(data.fuel.item,   data.fuel.count,   dropPos);
                if (data.input  != null && !data.input.IsEmpty)
                    dropper.DropItem(data.input.item,  data.input.count,  dropPos);
                if (data.result != null && !data.result.IsEmpty)
                    dropper.DropItem(data.result.item, data.result.count, dropPos);
            }

            _furnaces.Remove(pos);
        }

        // ──────────────────────────────────────────────
        // 저장/불러오기
        // ──────────────────────────────────────────────

        [Serializable]
        public class SaveData
        {
            public List<FurnaceSaveEntry> entries = new();
        }

        [Serializable]
        public class FurnaceSaveEntry
        {
            public int   x, y, z;
            public float remainingFuel;
            public float maxFuel;
            public float smeltTimer;

            // 아이템은 이름으로 저장 (SaveManager 방식과 동일)
            public string fuelItemPath;
            public int    fuelCount;
            public string inputItemPath;
            public int    inputCount;
            public string resultItemPath;
            public int    resultCount;
        }

        public SaveData GetSaveData()
        {
            var save = new SaveData();
            foreach (var kv in _furnaces)
            {
                var d = kv.Value;
                var entry = new FurnaceSaveEntry
                {
                    x             = kv.Key.x,
                    y             = kv.Key.y,
                    z             = kv.Key.z,
                    remainingFuel = d.remainingFuel,
                    maxFuel       = d.maxFuel,
                    smeltTimer    = d.smeltTimer,
                    fuelItemPath  = GetPath(d.fuel?.item),
                    fuelCount     = d.fuel?.count ?? 0,
                    inputItemPath = GetPath(d.input?.item),
                    inputCount    = d.input?.count ?? 0,
                    resultItemPath = GetPath(d.result?.item),
                    resultCount   = d.result?.count ?? 0,
                };
                save.entries.Add(entry);
            }
            return save;
        }

        public void LoadSaveData(SaveData save)
        {
            _furnaces.Clear();
            if (save?.entries == null) return;

            foreach (var entry in save.entries)
            {
                var data = new FurnaceData
                {
                    remainingFuel = entry.remainingFuel,
                    maxFuel       = entry.maxFuel,
                    smeltTimer    = entry.smeltTimer,
                    fuel          = LoadStack(entry.fuelItemPath,   entry.fuelCount),
                    input         = LoadStack(entry.inputItemPath,  entry.inputCount),
                    result        = LoadStack(entry.resultItemPath, entry.resultCount),
                };
                _furnaces[new Vector3Int(entry.x, entry.y, entry.z)] = data;
            }
        }

        // ──────────────────────────────────────────────
        // 틱 처리
        // ──────────────────────────────────────────────

        void TickFurnace(Vector3Int pos, FurnaceData d)
        {
            bool changed = false;

            // 연료 틱
            if (d.remainingFuel > 0)
            {
                d.remainingFuel -= Time.deltaTime;
                changed = true;
            }
            else if (d.fuel != null && !d.fuel.IsEmpty &&
                     d.fuel.item.isFuel && d.fuel.item.burnTime > 0f)
            {
                d.maxFuel       = d.fuel.item.burnTime;
                d.remainingFuel = d.fuel.item.burnTime;
                d.fuel.count--;
                if (d.fuel.count <= 0) d.fuel = null;
                changed = true;
            }

            // 제련 틱
            if (d.input != null && !d.input.IsEmpty && d.remainingFuel > 0)
            {
                var recipe = FurnaceSystem.Instance?.GetRecipe(d.input.item);
                if (recipe != null)
                {
                    bool resultMismatch = d.result != null &&
                                          !d.result.IsEmpty &&
                                          d.result.item != recipe.resultItem;

                    bool resultFull = d.result != null &&
                                      !d.result.IsEmpty &&
                                      d.result.count >= recipe.resultItem.maxStack;

                    if (!resultMismatch && !resultFull)
                    {
                        d.smeltTimer += Time.deltaTime;
                        changed = true;

                        if (d.smeltTimer >= recipe.smeltTime)
                        {
                            d.smeltTimer = 0;
                            d.input.count--;
                            if (d.input.count <= 0) d.input = null;

                            if (d.result == null || d.result.IsEmpty)
                                d.result = new ItemStack(recipe.resultItem);
                            else
                                d.result.count++;
                        }
                    }
                }
                else
                {
                    d.smeltTimer = 0;
                }
            }
            else
            {
                if (d.smeltTimer != 0) { d.smeltTimer = 0; changed = true; }
            }

            if (changed)
                OnFurnaceChanged?.Invoke(pos);
        }

        // ──────────────────────────────────────────────
        // 유틸
        // ──────────────────────────────────────────────

        static string GetPath(ItemSO item)
        {
            if (item == null) return null;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(item);
#else
            return item.name;
#endif
        }

        static ItemStack LoadStack(string path, int count)
        {
            if (string.IsNullOrEmpty(path) || count <= 0) return null;
            ItemSO item;
#if UNITY_EDITOR
            item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
#else
            item = Resources.Load<ItemSO>(path);
#endif
            return item != null ? new ItemStack(item, count) : null;
        }
    }
}