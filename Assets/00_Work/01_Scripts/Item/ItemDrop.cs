using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    /// <summary>
    /// Q키로 현재 핫바 아이템을 바라보는 방향으로 던지기
    /// </summary>
    public class ItemDrop : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private UIHotbar      hotbar;
        [SerializeField] private ItemDropper   itemDropper;
        [SerializeField] private Transform     headTransform;
        [SerializeField] private Animator animator;

        [Header("던지기 설정")]
        [Tooltip("던지는 힘")]
        [SerializeField] private float throwForce    = 5f;
        [Tooltip("위쪽으로 살짝 띄우는 힘")]
        [SerializeField] private float throwUpForce  = 2f;
        [Tooltip("스폰 위치 — 카메라 앞 거리")]
        [SerializeField] private float spawnDistance = 0.8f;
        
        private static readonly int DropHash =
            Animator.StringToHash("Punch");

        // ──────────────────────────────────────────────
        
        void OnEnable()  => playerInput.OnDrop += OnDrop;
        void OnDisable() => playerInput.OnDrop -= OnDrop;

        void OnDrop()
        {
            var stack = hotbar.GetSelectedItem();
            if (stack == null || stack.IsEmpty) return;

            Vector3 spawnPos = headTransform.position
                             + headTransform.forward * spawnDistance;

            // 드롭 아이템 생성
            var obj = Object.Instantiate(
                itemDropper.worldItemPrefab, spawnPos, Quaternion.identity);

            var worldItem = obj.GetComponent<WorldItem>();
            worldItem.Initialize(stack.item, itemDropper.blockData);

            // 던지는 방향 — 카메라 forward + 약간 위
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 throwDir = headTransform.forward + Vector3.up * (throwUpForce / throwForce);
                rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
    
                // 랜덤 회전 토크 추가
                rb.AddTorque(Random.insideUnitSphere * 0.25f, ForceMode.Impulse);
            }

            // 인벤토리에서 1개 차감
            var inv   = hotbar.inventory;
            int idx   = Inventory.InventorySize + hotbar.SelectedIndex;
            var slot  = inv.slots[idx];

            slot.count--;
            if (slot.count <= 0) slot.Clear();
            inv.NotifyChanged();

            animator.SetTrigger(DropHash);
        }
    }
}