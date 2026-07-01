using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.Item.SO;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class ItemDropper : MonoBehaviour
    {
        public GameObject worldItemPrefab;
        public BlockDataSO blockData;

        private Dictionary<string, ItemSO> _itemCache;
        
        void Awake()
        {
            // 시작 시 한 번만 로드해서 캐싱
            _itemCache = new Dictionary<string, ItemSO>();
            var allItems = Resources.LoadAll<ItemSO>("Item");
            foreach (var item in allItems)
                _itemCache[item.name] = item;
        }
        
        public void DropBlock(BlockType blockType, Vector3 position)
        {
            if (!_itemCache.TryGetValue(blockType.ToString(), out var item)) return;

            Vector3 spawnPos = new Vector3(
                Mathf.Floor(position.x) + 0.5f,
                Mathf.Floor(position.y) + 1.0f,
                Mathf.Floor(position.z) + 0.5f);

            var obj = Instantiate(worldItemPrefab, spawnPos, Quaternion.identity);
            obj.GetComponent<WorldItem>().Initialize(item, blockData, 1);

            // 랜덤 방향으로 튀어나오기
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDir = new Vector3(
                    Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
                rb.AddForce(randomDir * 0.8f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
            }
        }
        
        public void DropItem(ItemSO item, int count, Vector3 position)
        {
            if (item == null) return;

            var obj = Instantiate(worldItemPrefab, position, Quaternion.identity);
            obj.GetComponent<WorldItem>().Initialize(item, blockData, count);
        }
    }
}