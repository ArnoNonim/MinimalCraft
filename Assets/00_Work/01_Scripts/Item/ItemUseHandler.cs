using System.Collections;
using _00_Work._01_Scripts.Item.SO;
using _00_Work._01_Scripts.Player;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Sound;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    /// <summary>
    /// 핫바에서 들고 있는 UsableItemSO를 상호작용 키로 사용.
    /// 채널링 중 이동 또는 피격 시 취소.
    /// </summary>
    public class ItemUseHandler : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private PlayerStats   playerStats;
        [SerializeField] private UIHotbar      hotbar;
        [SerializeField] private Inventory     inventory;

        [Header("채널링 파티클")]
        [SerializeField] private ParticleSystem channelingParticle;

        [Header("취소 판정")]
        [Tooltip("이 거리 이상 이동하면 채널링 취소")]
        [SerializeField] private float moveCancelThreshold = 0.5f;

        // ── 상태 ────────────────────────────────────────────────────
        private bool      _isChanneling;
        private Coroutine _channelCoroutine;
        private Coroutine _particleFadeCoroutine;
        private Vector3   _channelStartPos;

        // ──────────────────────────────────────────────

        private void OnEnable()
        {
            playerInput.OnInteractKeyDown += OnInteractKeyDown;
            if (playerStats != null)
                playerStats.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            playerInput.OnInteractKeyDown -= OnInteractKeyDown;
            if (playerStats != null)
                playerStats.OnDamaged -= OnDamaged;

            CancelChannel();
        }

        private void Update()
        {
            if (!_isChanneling) return;

            float moved = Vector3.Distance(transform.position, _channelStartPos);
            if (moved >= moveCancelThreshold)
                CancelChannel();
        }

        // ──────────────────────────────────────────────

        private void OnInteractKeyDown()
        {
            if (_isChanneling)
            {
                CancelChannel();
                return;
            }

            ItemStack stack = hotbar.GetSelectedItem();
            if (stack == null || stack.IsEmpty) return;
            if (stack.item is not UsableItemSO usable) return;

            _channelCoroutine = StartCoroutine(ChannelRoutine(stack, usable));
        }

        private void OnDamaged()
        {
            if (_isChanneling) CancelChannel();
        }

        // ──────────────────────────────────────────────

        private IEnumerator ChannelRoutine(ItemStack stack, UsableItemSO usable)
        {
            _isChanneling    = true;
            _channelStartPos = transform.position;

            UIChannelBar.Instance?.StartChannel(usable.itemName, usable.channelDuration);
            if (usable.itemName == "귀환석" || usable.itemName == "제네시스 스피어")
            {
                PlayParticle();
                SoundManager.Instance.PlaySFXAt("Teleport", transform);
            }
            else if (usable.itemName == "스마나")
            {
                SoundManager.Instance.PlaySFXAt("Eat", transform);
            }

            float elapsed = 0f;
            while (elapsed < usable.channelDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 완료 — 파티클 서서히 소멸
            UIChannelBar.Instance?.CancelChannel();
            StopParticleGradually();

            usable.OnUse(gameObject);

            if (usable.consumeOnUse)
            {
                int hotbarSlotIndex = Inventory.InventorySize + hotbar.SelectedIndex;
                var slot = inventory.slots[hotbarSlotIndex];
                slot.count--;
                if (slot.count <= 0) slot.Clear();
                inventory.NotifyChanged();
            }

            _isChanneling     = false;
            _channelCoroutine = null;
        }

        private void CancelChannel()
        {
            if (!_isChanneling) return;

            if (_channelCoroutine != null)
            {
                StopCoroutine(_channelCoroutine);
                _channelCoroutine = null;
            }

            _isChanneling = false;
            UIChannelBar.Instance?.CancelChannel();
            StopParticleGradually();
        }

        // ── 파티클 제어 ───────────────────────────────────────────────

        private void PlayParticle()
        {
            if (channelingParticle == null) return;

            // 진행 중인 페이드아웃 코루틴 있으면 취소
            if (_particleFadeCoroutine != null)
            {
                StopCoroutine(_particleFadeCoroutine);
                _particleFadeCoroutine = null;
            }

            channelingParticle.Play();
        }

        private void StopParticleGradually()
        {
            if (channelingParticle == null) return;

            // StopEmitting: 새 파티클 생성 중단, 기존 파티클은 수명대로 서서히 사라짐
            channelingParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}