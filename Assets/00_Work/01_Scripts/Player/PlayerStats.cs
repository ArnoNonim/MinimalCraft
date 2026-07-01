using System;
using _00_Work._01_Scripts.ChunkSystem.Biome;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Player.SO;
using _00_Work._01_Scripts.Sound;
using _00_Work._01_Scripts.UI;
using _00_Work._01_Scripts.UI.UIs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlayerStatSO   stat;
        [SerializeField] private UIBars          uiBars;
        [SerializeField] private PlayerMovement  movement;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private DamageVignette  damageVignette;
        [SerializeField] private UIDeath deathUI;
        
        [SerializeField] private ItemDropper itemDropper;
        [SerializeField] private Inventory   inventory;

        [Header("설정")]
        [SerializeField] private string hurtSound;

        [SerializeField] private PlayerRagdoll ragdoll;
        
        [Header("허기 / 목마름 — 자연 소모")]
        [Tooltip("정지 상태에서 허기·목마름이 줄어드는 주기 (초) — 아주 느리게")]
        [SerializeField] private float depletionInterval = 30f;

        [Tooltip("허기·목마름이 0일 때 체력 감소 주기 (초)")]
        [SerializeField] private float starvationInterval = 2f;

        [Tooltip("허기·목마름이 0일 때 1회 감소량")]
        [SerializeField] private int starvationDamage = 1;

        [Header("허기 / 목마름 — 이동 소모")]
        [Tooltip("이동 소모가 누적되는 단위 (초) — 이 시간마다 이동 소모 1회 처리")]
        [SerializeField] private float moveDepletionInterval = 1f;

        [Tooltip("걷기 1회당 소모량")]
        [SerializeField] private float walkCost    = 0.15f;

        [Tooltip("뛰기 1회당 소모량")]
        [SerializeField] private float sprintCost  = 0.35f;

        [Tooltip("점프 1회당 즉시 소모량 (타이머 무관)")]
        [SerializeField] private float jumpCost    = 0.5f;

        [Tooltip("수영 1회당 소모량")]
        [SerializeField] private float swimCost    = 0.25f;

        [Header("체온")]
        [Tooltip("체온이 환경 온도 쪽으로 변화하는 속도 (도/초)")]
        [SerializeField] private float temperatureChangeRate = 0.5f;

        [Tooltip("이 온도를 초과하면 고온 틱데미지 (기본 38.5°C)")]
        [SerializeField] private float dangerHeatThreshold = 38.5f;

        [Tooltip("이 온도 미만이면 저체온 틱데미지 (기본 35.0°C)")]
        [SerializeField] private float dangerColdThreshold = 35.0f;

        [Tooltip("체온 위험 시 데미지 주기 (초)")]
        [SerializeField] private float temperatureDamageInterval = 3f;

        [Tooltip("체온 위험 시 1회 데미지")]
        [SerializeField] private int temperatureDamage = 1;

        [Header("바이옴 자동 감지")]
        [Tooltip("바이옴을 다시 확인하는 주기 (초). 너무 짧으면 퍼린노이즈 연산 부담 증가")]
        [SerializeField] private float biomeCheckInterval = 1f;

        // 디버그용 — Inspector에서 현재 바이옴 확인
        [SerializeField, HideInInspector] private BiomeType currentBiome = BiomeType.Plains;


        [Header("자연 회복")]
        [Tooltip("자연 회복 주기 (초)")]
        [SerializeField] private float regenInterval = 4f;

        [Tooltip("1회 회복량 (동일한 양만큼 허기도 소모됨)")]
        [SerializeField] private int regenAmount = 1;

        [Tooltip("자연 회복이 활성화되는 최소 허기 수치 (슬롯 기준, 8칸 = 16포인트)")]
        [SerializeField] private int regenHungerThreshold = 16;

        [Tooltip("자연 회복이 활성화되는 최소 목마름 수치 (슬롯 기준, 8칸 = 16포인트)")]
        [SerializeField] private int regenThirstyThreshold = 16;

        public Action OnDamaged;


        private float _depletionTimer;
        private float _starvationTimer;
        private float _temperatureDamageTimer;
        private float _biomeCheckTimer;
        private float _regenTimer;
        private float _moveDepletionTimer;
        private float _moveDepletionAccum;  // 이동 소모 누적값 (float → int 변환용)
        private bool  _wasJumping;           // 점프 즉시 소모 중복 방지


        private void Start()
        {
            stat.curHealth     = stat.maxHealth;
            stat.curEfficiency = stat.maxEfficiency;
            stat.curHunger     = stat.maxHunger;
            stat.curThirsty    = stat.maxThirsty;
            stat.temperature   = 36.5f;

            RefreshBiome();
            uiBars.UpdateBars();
        }

        private void Update()
        {
            TickBiome();
            TickDepletion();
            TickMoveDepletion();
            TickStarvation();
            TickRegen();
            TickTemperature();
        }


        public void TakeDamage(int amount)
        {
            ModifyHealth(-amount);
            impulseSource.GenerateImpulse();
            damageVignette.PlayDamageEffect(amount);
            SoundManager.Instance.PlaySFXAt(hurtSound, gameObject.transform.parent, 1.5f);
            OnDamaged?.Invoke();
        }
        public void Heal(int amount)            => ModifyHealth(amount);
        public void EatFood(int amount)         { ModifyStat(ref stat.curHunger,     stat.maxHunger,     amount); RefreshUI(); }
        public void Drink(int amount)           { ModifyStat(ref stat.curThirsty,    stat.maxThirsty,    amount); RefreshUI(); }
        public void ModifyEfficiency(int delta) { ModifyStat(ref stat.curEfficiency, stat.maxEfficiency, delta);  RefreshUI(); }

        public void SetBiome(BiomeType biome) => currentBiome = biome;

        public bool IsAlive         => stat.curHealth > 0;
        public bool IsStarving      => stat.curHunger == 0;
        public bool IsDehydrated    => stat.curThirsty == 0;
        public bool IsOverheating   => stat.temperature > dangerHeatThreshold;
        public bool IsHypothermic   => stat.temperature < dangerColdThreshold;
        public bool IsTempDangerous => IsOverheating || IsHypothermic;

        private void TickBiome()
        {
            _biomeCheckTimer += Time.deltaTime;
            if (_biomeCheckTimer < biomeCheckInterval) return;
            _biomeCheckTimer = 0f;

            RefreshBiome();
        }

        private void TickDepletion()
        {
            _depletionTimer += Time.deltaTime;
            if (_depletionTimer < depletionInterval) return;
            _depletionTimer = 0f;

            bool changed = false;

            if (stat.curHunger > 0)
            {
                ModifyStat(ref stat.curHunger, stat.maxHunger, -1);
                changed = true;
            }

            if (stat.curThirsty > 0)
            {
                ModifyStat(ref stat.curThirsty, stat.maxThirsty, -1);
                changed = true;
            }

            if (changed) RefreshUI();
        }

        private void TickMoveDepletion()
        {
            if (!IsAlive) return;

            // 점프 즉시 소모 (착지 후 재점프 시에만)
            bool isJumping = movement.IsJumping;
            if (isJumping && !_wasJumping)
            {
                _moveDepletionAccum += jumpCost;
            }
            _wasJumping = isJumping;

            // 이동 소모 타이머
            _moveDepletionTimer += Time.deltaTime;
            if (_moveDepletionTimer < moveDepletionInterval) return;
            _moveDepletionTimer = 0f;

            // 현재 이동 상태에 맞는 소모량 누적
            if (movement.IsInWater && movement.MoveDir.magnitude > 0.1f)
                _moveDepletionAccum += swimCost;
            else if (movement.IsSprinting && movement.MoveDir.magnitude > 0.1f)
                _moveDepletionAccum += sprintCost;
            else if (movement.MoveDir.magnitude > 0.1f)
                _moveDepletionAccum += walkCost;

            // 누적값 1 이상이면 실제 차감
            if (_moveDepletionAccum < 1f) return;

            int consume         = Mathf.FloorToInt(_moveDepletionAccum);
            _moveDepletionAccum -= consume;

            bool changed = false;

            if (stat.curHunger > 0)
            {
                ModifyStat(ref stat.curHunger, stat.maxHunger, -consume);
                changed = true;
            }
            if (stat.curThirsty > 0)
            {
                ModifyStat(ref stat.curThirsty, stat.maxThirsty, -consume);
                changed = true;
            }

            if (changed) RefreshUI();
        }

        private void TickStarvation()
        {
            if (!IsStarving && !IsDehydrated) return;
            if (!IsAlive) return;

            _starvationTimer += Time.deltaTime;
            if (_starvationTimer < starvationInterval) return;
            _starvationTimer = 0f;

            TakeDamage(starvationDamage);
        }

        private void TickRegen()
        {
            if (!IsAlive) return;
            if (stat.curHealth >= stat.maxHealth) return;
            if (stat.curHunger  < regenHungerThreshold)  return;
            if (stat.curThirsty < regenThirstyThreshold) return;

            _regenTimer += Time.deltaTime;
            if (_regenTimer < regenInterval) return;
            _regenTimer = 0f;

            ModifyStat(ref stat.curHealth, stat.maxHealth, regenAmount);
            ModifyStat(ref stat.curHunger, stat.maxHunger, -regenAmount);
            RefreshUI();
        }

        private void TickTemperature()
        {
            if (!IsAlive) return;

            float ambientTemp     = BiomeGenerator.GetBiomeAmbientTemperature(currentBiome);
            float prevTemperature = stat.temperature;
            stat.temperature      = Mathf.MoveTowards(
                stat.temperature,
                ambientTemp,
                temperatureChangeRate * Time.deltaTime);

            if (!Mathf.Approximately(prevTemperature, stat.temperature))
                uiBars.UpdateTemperatureUI(stat.temperature);

            if (!IsTempDangerous) return;

            _temperatureDamageTimer += Time.deltaTime;
            if (_temperatureDamageTimer < temperatureDamageInterval) return;
            _temperatureDamageTimer = 0f;

            TakeDamage(temperatureDamage);
            Debug.Log(IsOverheating
                ? $"[PlayerStats] 고온 데미지! 체온: {stat.temperature:F1}°C"
                : $"[PlayerStats] 저체온 데미지! 체온: {stat.temperature:F1}°C");
        }

        private void RefreshBiome()
        {
            if (BiomeGenerator.Settings == null) return;

            BiomeType detected = BiomeGenerator.GetBiome(
                transform.position.x,
                transform.position.z);

            if (detected == currentBiome) return;

            currentBiome = detected;
        }

        private void ModifyHealth(int delta)
        {
            ModifyStat(ref stat.curHealth, stat.maxHealth, delta);
            RefreshUI();
            if (!IsAlive) OnDeath();
        }

        private static void ModifyStat(ref int current, int max, int delta)
        {
            current = Mathf.Clamp(current + delta, 0, max);
        }

        private void RefreshUI() => uiBars.UpdateBars();

        private void OnDeath()
        {
            DropAllItems();
            ragdoll?.OnDeath();
            deathUI?.Show();
        }
        
        void DropAllItems()
        {
            if (itemDropper == null || inventory == null) return;

            Vector3 dropPos = transform.position + Vector3.up * 0.5f;

            for (int i = 0; i < inventory.slots.Length; i++)
            {
                var slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty) continue;

                itemDropper.DropItem(slot.item, slot.count, dropPos);
                slot.Clear();
            }

            inventory.NotifyChanged();
        }

        public void Respawn()
        {
            stat.curHealth  = stat.maxHealth;
            stat.curHunger  = stat.maxHunger;
            stat.curThirsty = stat.maxThirsty;
            uiBars.UpdateBars();
        }
    }
}