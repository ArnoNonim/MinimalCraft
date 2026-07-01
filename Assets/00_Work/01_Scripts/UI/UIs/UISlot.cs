using System;
using System.Collections.Generic;
using _00_Work._01_Scripts.Item;
using _00_Work._01_Scripts.Item.SO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UISlot : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Header("참조")]
        public RawImage iconImage;
        public TMP_Text countText;
        public Image    slotBackground;

        [Header("설정")]
        public float doubleClickInterval = 0.3f;

        [Header("내구도")]
        public Image durabilityBar;
        public Image durabilityFill;

        [Header("홀드 분배 설정")]
        [SerializeField] private float holdDelay = 0.25f;

        public event Action<int, ItemSO> OnItemChanged;
        public event Action<int, bool> OnResultTaken;

        [field: SerializeField] public int SlotIndex { get; set; }
        public Inventory Inventory { get; set; }

        public enum SlotType
        {
            Inventory,
            Crafting,
            FurnaceInput,
            FurnaceFuel,
            Result
        }

        [SerializeField] public SlotType slotType = SlotType.Inventory;

        bool IsResult      => slotType == SlotType.Result;
        private bool IsCraftingLike =>
            slotType == SlotType.Crafting    ||
            slotType == SlotType.FurnaceInput ||
            slotType == SlotType.FurnaceFuel;
        
        private ItemStack _craftingStack;
        private ItemStack _resultStack;

        public ItemSO CraftingItem  => _craftingStack?.item;
        public int    CraftingCount => _craftingStack?.count ?? 0;

        // ── 전역 픽업 상태 ─────────────────────────────────────────────
        private static PickupState     _pickup;
        private static RawImage        _cursorIcon;
        private static TextMeshProUGUI _cursorCountText;
        private static Canvas          _canvas;

        // ── 홀드 분배 상태 ─────────────────────────────────────────────
        private static bool         _isHolding;
        private static List<UISlot> _holdSlots;
        private static int          _holdTotal;
        private static bool         _holdDidWork;
        private static bool         _holdStartedByDrag;
        private static bool         _clickConsumedByHold;

        // ── 홀드 딜레이 ───────────────────────────────────────────────
        private float _pointerDownTime = -1f;
        private bool  _waitingForHold;

        // ── 더블클릭 ─────────────────────────────────────────────────
        private float         _lastClickTime;
        private static UISlot _lastClickedSlot;

        private class PickupState
        {
            public UISlot SourceSlot;
            public ItemSO Item;
            public int    Count;
            public int    InstanceId;
            public bool   IsCrafting;
        }

        // ──────────────────────────────────────────────
        // Unity 이벤트
        // ──────────────────────────────────────────────

        void Awake()
        {
            if (iconImage == null)
                iconImage = GetComponentInChildren<RawImage>();
            if (countText == null)
                countText = GetComponentInChildren<TMP_Text>();
            if (slotBackground == null)
            {
                var imgs = GetComponentsInChildren<Image>();
                if (imgs.Length > 0) slotBackground = imgs[0];
            }
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (durabilityBar != null)
            {
                durabilityBar.fillAmount = 1f;
                durabilityBar.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            // 커서 아이콘 따라다니기
            if (_pickup != null && _cursorIcon != null &&
                _cursorIcon.gameObject.activeSelf && _canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform,
                    Mouse.current.position.ReadValue(),
                    _canvas.worldCamera, out Vector2 p);
                _cursorIcon.rectTransform.anchoredPosition = p;
            }

            // 홀드 딜레이 체크 — 일정 시간 누르면 홀드 시작
            if (_waitingForHold && _pickup != null)
            {
                if (Time.unscaledTime - _pointerDownTime >= holdDelay)
                {
                    _waitingForHold    = false;
                    _isHolding         = true;
                    _holdDidWork       = false;
                    _holdTotal         = _pickup.Count;
                    _holdSlots         = new List<UISlot> { this };
                    _holdStartedByDrag = true;
                }
            }
        }

        void OnDisable()
        {
            if (_isHolding) CancelHold();
            if (_pickup != null && _pickup.SourceSlot != null)
                ReturnPickup();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        public void SetCraftingItem(ItemSO item, int count)
        {
            _craftingStack = item != null ? new ItemStack(item, count) : null;
            UpdateSlot(_craftingStack);
            OnItemChanged?.Invoke(SlotIndex, item);
        }

        public void SetCraftingItemSilent(ItemStack stack)
        {
            _craftingStack = stack;
            UpdateSlot(stack);
        }

        public void SetResultItem(ItemStack stack)
        {
            _resultStack = stack;
            UpdateSlot(stack);
        }

        public void UpdateSlot(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty || stack.item == null)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (countText != null) countText.enabled = false;
                HideDurabilityBar();
                return;
            }

            if (ItemIconRenderer.Instance != null)
            {
                var rt = ItemIconRenderer.Instance.GetIconAuto(stack.item);
                if (rt != null) { iconImage.texture = rt; iconImage.enabled = true; }
                else
                {
                    if (iconImage != null) iconImage.enabled = false;
                    CancelInvoke(nameof(RefreshFromData));
                    Invoke(nameof(RefreshFromData), 0.1f);
                }
            }
            else
            {
                if (iconImage != null) iconImage.enabled = false;
                CancelInvoke(nameof(RefreshFromData));
                Invoke(nameof(RefreshFromData), 0.1f);
            }

            if (countText != null)
            {
                countText.enabled = stack.count > 1;
                countText.text    = stack.count.ToString();
            }

            if (stack.HasDurability)
            {
                var   tool = stack.item as ToolItemSO;
                int   max  = tool.toolData.maxDurability;
                int   cur  = stack.GetDurability();
                float r    = max > 0 ? (float)cur / max : 1f;
                if (r >= 1f) HideDurabilityBar();
                else
                {
                    if (durabilityBar != null) durabilityBar.gameObject.SetActive(true);
                    if (durabilityFill != null)
                    {
                        durabilityFill.fillAmount = r;
                        durabilityFill.color = r > 0.5f ? Color.green
                                             : r > 0.25f ? Color.yellow : Color.red;
                    }
                }
            }
            else HideDurabilityBar();
        }

        public void RefreshFromData()
        {
            if (IsResult)       { UpdateSlot(_resultStack);   return; }
            if (IsCraftingLike) { UpdateSlot(_craftingStack); return; }
            if (Inventory != null && SlotIndex >= 0 && SlotIndex < Inventory.slots.Length)
                UpdateSlot(Inventory.slots[SlotIndex]);
        }

        // ──────────────────────────────────────────────
        // 포인터 이벤트
        // ──────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData e)
        {
            if (_pickup != null && Mouse.current.leftButton.isPressed)
            {
                if (!_isHolding)
                {
                    _isHolding         = true;
                    _holdDidWork       = false;
                    _holdTotal         = _pickup.Count;
                    _holdSlots         = new List<UISlot>();
                    _holdStartedByDrag = true;
                }
 
                if (_holdSlots != null && !_holdSlots.Contains(this) && CanAcceptHold(this))
                {
                    _holdSlots.Add(this);
                    _holdDidWork = true;
                    RefreshHoldPreview(); // 전체 슬롯 미리보기 갱신
                }
                return;
            }
 
            var stack = GetCurrentStack();
            if (stack == null || stack.IsEmpty) return;
            ItemTooltip.Instance?.Show(stack.item, e.position);
        }

        public void OnPointerExit(PointerEventData e)
            => ItemTooltip.Instance?.Hide();

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;

            _holdStartedByDrag = false;
            _waitingForHold    = false;

            if (_pickup != null && CanAcceptHold(this))
            {
                _pointerDownTime = Time.unscaledTime;
                _waitingForHold  = true;
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;

            _waitingForHold  = false;
            _pointerDownTime = -1f;

            // 홀드로 처리됐으면 클릭 소비 플래그 세팅
            _clickConsumedByHold = _holdStartedByDrag && _holdDidWork;

            if (_holdStartedByDrag && _isHolding)
            {
                if (_holdDidWork && _holdSlots != null && _holdSlots.Count > 0)
                    CommitHold();
                else
                    CancelHold();
            }

            // 홀드 상태 일괄 초기화
            _isHolding         = false;
            _holdSlots         = null;
            _holdDidWork       = false;
            _holdStartedByDrag = false;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;

            _waitingForHold  = false;
            _pointerDownTime = -1f;

            // 홀드로 소비된 클릭 무시
            if (_clickConsumedByHold) { _clickConsumedByHold = false; return; }

            ItemTooltip.Instance?.Hide();
            ProcessClick();
        }

        // ──────────────────────────────────────────────
        // 홀드 분배
        // ──────────────────────────────────────────────

        static bool CanAcceptHold(UISlot slot)
        {
            if (_pickup == null || slot.IsResult) return false;

            if (slot.IsCraftingLike)
            {
                var ex = slot._craftingStack?.item;
                if (ex == null) return true;
                if (ex != _pickup.Item) return false;
                // ← maxStack 체크 추가
                return slot._craftingStack.count < _pickup.Item.maxStack;
            }

            if (slot.Inventory == null) return false;
            var t = slot.Inventory.slots[slot.SlotIndex];
            return t.IsEmpty ||
                   (t.item == _pickup.Item && t.count < _pickup.Item.maxStack);
        }

        static void RefreshHoldPreview()
        {
            if (_pickup == null || _holdSlots == null || _holdSlots.Count == 0) return;

            int n       = _holdSlots.Count;
            int perSlot = _holdTotal / n;
            int extra   = _holdTotal % n;

            for (int i = 0; i < n; i++)
            {
                _holdSlots[i].CancelInvoke(nameof(RefreshFromData));
                _holdSlots[i].ShowHoldPreview(perSlot + (i < extra ? 1 : 0));
            }

            UpdateCursorCount(0);
        }

        void ShowHoldPreview(int addAmount)
        {
            CancelInvoke(nameof(RefreshFromData));

            int cur = 0;
            if (IsCraftingLike)
                cur = _craftingStack?.count ?? 0;
            else if (Inventory != null)
            {
                var t = Inventory.slots[SlotIndex];
                cur = (t.item == _pickup.Item) ? t.count : 0;
            }
            else return;

            int previewCount = cur + addAmount;

            if (previewCount <= 0)
            {
                RefreshFromData(); // ← 아이콘 표시할 게 없으면 원본 복원
                return;
            }

            if (iconImage != null)
            {
                var rt = ItemIconRenderer.Instance?.GetIconAuto(_pickup.Item);
                if (rt != null) { iconImage.texture = rt; iconImage.enabled = true; }
            }
            if (countText != null)
            {
                countText.enabled = previewCount > 1;
                countText.text    = previewCount.ToString();
            }
        }

        static void CommitHold()
        {
            if (_pickup == null || _holdSlots == null) return;
 
            int n          = _holdSlots.Count;
            int perSlot    = _holdTotal / n;
            int extra      = _holdTotal % n;
            int totalGiven = 0;
 
            var dirtyCraftingSlots = new List<UISlot>();
 
            for (int i = 0; i < n; i++)
            {
                var slot   = _holdSlots[i];
                int amount = perSlot + (i < extra ? 1 : 0);
                if (amount <= 0)
                {
                    slot.CancelInvoke(nameof(RefreshFromData));
                    slot.RefreshFromData(); // ← 유령 제거
                    continue;
                }
 
                // 커밋 전 예약된 RefreshFromData 취소  
                slot.CancelInvoke(nameof(RefreshFromData));
 
                if (slot.IsCraftingLike)
                {
                    int cur         = slot._craftingStack?.count ?? 0;
                    int max         = _pickup.Item.maxStack;
                    int newCnt      = Mathf.Min(cur + amount, max);
                    int actualGiven = newCnt - cur;
                    if (actualGiven <= 0) continue;
 
                    // OnItemChanged 없이 데이터+UI만 직접 갱신
                    slot._craftingStack = new ItemStack(_pickup.Item, newCnt);
                    slot.UpdateSlot(slot._craftingStack);
                    totalGiven += actualGiven;
                    dirtyCraftingSlots.Add(slot);
                }
                else if (slot.Inventory != null)
                {
                    var t = slot.Inventory.slots[slot.SlotIndex];
                    if (t.IsEmpty)
                    {
                        t.item  = _pickup.Item;
                        t.count = amount;
                        totalGiven += amount;
                    }
                    else if (t.item == _pickup.Item)
                    {
                        int give = Mathf.Min(amount, t.item.maxStack - t.count);
                        t.count   += give;
                        totalGiven += give;
                    }
                    slot.Inventory.NotifyChanged();
                }
            }
 
            // 조합 슬롯 OnItemChanged 일괄 발동 — 레시피 갱신
            foreach (var slot in dirtyCraftingSlots)
                slot.OnItemChanged?.Invoke(slot.SlotIndex, slot._craftingStack?.item);
 
            _pickup.Count -= totalGiven;
            if (_pickup.Count <= 0) ClearPickup();
            else UpdateCursorCount(_pickup.Count);
        }

        static void CancelHold()
        {
            if (_holdSlots != null)
            {
                foreach (var slot in _holdSlots)
                {
                    slot.CancelInvoke(nameof(RefreshFromData)); // 예약 취소
                    slot.RefreshFromData();                     // 원본 데이터로 복원
                }
            }
            UpdateCursorCount(_pickup?.Count ?? 0);
        }

        // ──────────────────────────────────────────────
        // 클릭 처리
        // ──────────────────────────────────────────────

        void ProcessClick()
        {
            bool isShift = Keyboard.current?.shiftKey.isPressed ?? false;
         
            if (IsResult) { HandleResultSlotClick(isShift); return; }
            if (isShift && _pickup == null && !IsCraftingLike) { HandleShiftClick(); return; }
            if (_pickup != null) { HandleDrop(); return; }

            {
                float now  = Time.unscaledTime;
                bool  isDbl = (this == _lastClickedSlot) &&
                              (now - _lastClickTime) <= doubleClickInterval &&
                              _lastClickTime > 0f;
                _lastClickTime   = now;
                _lastClickedSlot = this;

                if (isDbl)
                {
                    if (IsCraftingLike) MergeCrafting();
                    else if (Inventory != null) MergeAll();
                    _lastClickTime = 0f;
                    return;
                }
            }

            HandlePickup();
        }
        
        void MergeCrafting()
        {
            if (_craftingStack == null || _craftingStack.IsEmpty) return;
            var item = _craftingStack.item;
            int max  = item.maxStack;
            if (_craftingStack.count >= max) return;

            // 같은 Canvas 내 모든 UISlot의 조합 슬롯에서 긁어오기
            var allSlots = _canvas.GetComponentsInChildren<UISlot>();
            foreach (var other in allSlots)
            {
                if (other == this) continue;
                if (!other.IsCraftingLike) continue;
                if (other.CraftingItem != item) continue;

                int add = Mathf.Min(max - _craftingStack.count, other.CraftingCount);
                if (add <= 0) continue;

                other.SetCraftingItem(
                    other.CraftingCount - add > 0 ? item : null,
                    other.CraftingCount - add);

                _craftingStack.count += add;
                UpdateSlot(_craftingStack);
                OnItemChanged?.Invoke(SlotIndex, item);

                if (_craftingStack.count >= max) break;
            }
        }

        // ──────────────────────────────────────────────
        // 결과 슬롯
        // ──────────────────────────────────────────────

        void HandleResultSlotClick(bool isShift)
        {
            if (_resultStack == null || _resultStack.IsEmpty) return;

            if (isShift)
            {
                while (true)
                {
                    if (_resultStack == null || _resultStack.IsEmpty) break;
                    if (Inventory == null || Inventory.IsFull()) break;

                    int cnt = _resultStack.count;
                    SetResultItem(null);
                    OnResultTaken?.Invoke(cnt, true);
                }
                return;
            }

            if (_pickup != null) return;

            int c  = _resultStack.count;
            int id = _resultStack.instanceId;
            var it = _resultStack.item;

            SetResultItem(null);
            OnResultTaken?.Invoke(c, false);

            _pickup = new PickupState
            {
                SourceSlot = this,
                Item       = it,
                Count      = c,
                InstanceId = id,
                IsCrafting = false
            };
            ShowPickupVisual(it, c);
        }

        // ──────────────────────────────────────────────
        // Shift+클릭 인벤↔핫바
        // ──────────────────────────────────────────────

        void HandleShiftClick()
        {
            if (Inventory == null) return;
            var slot = Inventory.slots[SlotIndex];
            if (slot == null || slot.IsEmpty) return;

            bool isHotbar   = SlotIndex >= Inventory.InventorySize;
            int  rem        = slot.count;
            var  item       = slot.item;
            int  origInstId = slot.instanceId;
            slot.Clear();

            int from = isHotbar ? 0                      : Inventory.InventorySize;
            int to   = isHotbar ? Inventory.InventorySize : Inventory.TotalSize;

            for (int i = from; i < to && rem > 0; i++)
            {
                var s = Inventory.slots[i];
                if (s.item != item || s.count >= item.maxStack) continue;
                int add = Mathf.Min(item.maxStack - s.count, rem);
                s.count += add; rem -= add;
            }
            for (int i = from; i < to && rem > 0; i++)
            {
                if (!Inventory.slots[i].IsEmpty) continue;
                var s        = Inventory.slots[i];
                s.SetItem(item, rem);
                s.instanceId = origInstId;
                rem = 0;
            }
            if (rem > 0)
            {
                slot.item       = item;
                slot.count      = rem;
                slot.instanceId = origInstId;
            }
            Inventory.NotifyChanged();
        }

        // ──────────────────────────────────────────────
        // 픽업 / 드롭
        // ──────────────────────────────────────────────

        void HandlePickup()
        {
            bool isCtrl = Keyboard.current?.ctrlKey.isPressed ?? false;

            if (IsCraftingLike)
            {
                if (_craftingStack == null || _craftingStack.IsEmpty) return;
                int pick = isCtrl ? Mathf.Max(1, _craftingStack.count / 2) : _craftingStack.count;
                StartPickup(this, _craftingStack.item, pick, true);
                int rem = _craftingStack.count - pick;
                SetCraftingItem(rem > 0 ? _craftingStack.item : null, rem);
            }
            else
            {
                if (Inventory == null) return;
                var slot = Inventory.slots[SlotIndex];
                if (slot == null || slot.IsEmpty) return;
                int pick = isCtrl ? Mathf.Max(1, slot.count / 2) : slot.count;
                StartPickup(this, slot.item, pick, false);
                slot.count -= pick;
                if (slot.count <= 0) slot.Clear();
                Inventory.NotifyChanged();
            }
        }

        void HandleDrop()
        {
            if (_pickup == null) return;

            if (IsCraftingLike)
            {
                ItemSO existing = _craftingStack?.item;
                int    existCnt = _craftingStack?.count ?? 0;

                if (existing == _pickup.Item)
                {
                    SetCraftingItem(_pickup.Item, existCnt + _pickup.Count);
                    ClearPickup();
                }
                else if (existing != null)
                {
                    var prev = new PickupState
                    {
                        SourceSlot = _pickup.SourceSlot,
                        Item       = existing,
                        Count      = existCnt,
                        IsCrafting = true
                    };
                    SetCraftingItem(_pickup.Item, _pickup.Count);
                    ClearPickupVisual();
                    _pickup = prev;
                    ShowPickupVisual(prev.Item, prev.Count);
                }
                else
                {
                    SetCraftingItem(_pickup.Item, _pickup.Count);
                    ClearPickup();
                }
                return;
            }

            if (Inventory == null) return;
            var target = Inventory.slots[SlotIndex];

            if (target.IsEmpty)
            {
                target.item       = _pickup.Item;
                target.count      = _pickup.Count;
                target.instanceId = _pickup.InstanceId;
                Inventory.NotifyChanged();
                ClearPickup();
            }
            else if (target.item == _pickup.Item)
            {
                int canAdd = target.item.maxStack - target.count;
                int adding = Mathf.Min(canAdd, _pickup.Count);
                target.count  += adding;
                _pickup.Count -= adding;
                Inventory.NotifyChanged();
                if (_pickup.Count <= 0) ClearPickup();
                else UpdateCursorCount(_pickup.Count);
            }
            else
            {
                var swapItem       = target.item;
                var swapCount      = target.count;
                var swapInstanceId = target.instanceId;

                target.item       = _pickup.Item;
                target.count      = _pickup.Count;
                target.instanceId = _pickup.InstanceId;
                Inventory.NotifyChanged();

                ClearPickupVisual();
                _pickup = new PickupState
                {
                    SourceSlot = this,
                    Item       = swapItem,
                    Count      = swapCount,
                    InstanceId = swapInstanceId,
                    IsCrafting = false
                };
                ShowPickupVisual(swapItem, swapCount);
            }
        }

        // ──────────────────────────────────────────────
        // 픽업 시각화
        // ──────────────────────────────────────────────

        static void StartPickup(UISlot source, ItemSO item, int count, bool isCrafting)
        {
            int instanceId = 0;
            if (source.IsCraftingLike)
                instanceId = source._craftingStack?.instanceId ?? 0;
            else if (source.Inventory != null)
                instanceId = source.Inventory.slots[source.SlotIndex]?.instanceId ?? 0;
            else if (source.IsResult)
                instanceId = source._resultStack?.instanceId ?? 0;

            _pickup = new PickupState
            {
                SourceSlot = source,
                Item       = item,
                Count      = count,
                InstanceId = instanceId,
                IsCrafting = isCrafting
            };
            ShowPickupVisual(item, count);
        }

        static void ShowPickupVisual(ItemSO item, int count)
        {
            if (_cursorIcon == null)
            {
                var obj = new GameObject("CursorIcon");
                obj.transform.SetParent(_canvas.transform, false);
                _cursorIcon               = obj.AddComponent<RawImage>();
                _cursorIcon.raycastTarget = false;
                _cursorIcon.rectTransform.sizeDelta = new Vector2(40, 40);

                var srcText = _pickup?.SourceSlot?.countText;
                if (srcText != null)
                {
                    var cObj = Instantiate(srcText.gameObject, obj.transform, false);
                    _cursorCountText = cObj.GetComponent<TextMeshProUGUI>();
                    _cursorCountText.raycastTarget = false;
                    var sr = srcText.GetComponent<RectTransform>();
                    var dr = cObj.GetComponent<RectTransform>();
                    dr.anchorMin = sr.anchorMin; dr.anchorMax = sr.anchorMax;
                    dr.offsetMin = sr.offsetMin; dr.offsetMax = sr.offsetMax;
                    dr.anchoredPosition = sr.anchoredPosition;
                }
            }

            var rt = ItemIconRenderer.Instance?.GetIconAuto(item);
            _cursorIcon.texture = rt;
            _cursorIcon.gameObject.SetActive(true);

            if (_cursorCountText != null)
            {
                _cursorCountText.enabled = count > 1;
                _cursorCountText.text    = count.ToString();
            }
        }

        static void UpdateCursorCount(int count)
        {
            if (_cursorCountText == null) return;
            _cursorCountText.enabled = count > 1;
            _cursorCountText.text    = count.ToString();
        }

        static void ClearPickupVisual()
        {
            if (_cursorIcon != null)
                _cursorIcon.gameObject.SetActive(false);
        }

        static void ClearPickup()
        {
            _pickup              = null;
            _isHolding           = false;
            _holdSlots           = null;
            _holdDidWork         = false;
            _holdStartedByDrag   = false;
            _clickConsumedByHold = false;
            ClearPickupVisual();
        }

        static void ReturnPickup()
        {
            if (_pickup == null) return;
            var src = _pickup.SourceSlot;

            if (_pickup.IsCrafting)
            {
                src.SetCraftingItem(_pickup.Item, src.CraftingCount + _pickup.Count);
            }
            else if (src.Inventory != null)
            {
                var slot = src.Inventory.slots[src.SlotIndex];
                if (slot.IsEmpty)
                {
                    slot.item       = _pickup.Item;
                    slot.count      = _pickup.Count;
                    slot.instanceId = _pickup.InstanceId;
                }
                else if (slot.item == _pickup.Item)
                    slot.count += _pickup.Count;
                else
                    src.Inventory.AddItem(_pickup.Item, _pickup.Count);
                src.Inventory.NotifyChanged();
            }

            ClearPickup();
        }

        // ──────────────────────────────────────────────
        // 헬퍼
        // ──────────────────────────────────────────────

        ItemStack GetCurrentStack()
        {
            if (IsResult)       return _resultStack;
            if (IsCraftingLike) return _craftingStack;
            if (Inventory == null) return null;
            if (SlotIndex < 0 || SlotIndex >= Inventory.slots.Length) return null;
            return Inventory.slots[SlotIndex];
        }

        void MergeAll()
        {
            if (Inventory == null) return;
            var stack = Inventory.slots[SlotIndex];
            if (stack == null || stack.IsEmpty) return;
            var item = stack.item;
            int max  = item.maxStack;
            if (stack.count >= max) return;

            for (int i = 0; i < Inventory.slots.Length; i++)
            {
                if (i == SlotIndex) continue;
                var o = Inventory.slots[i];
                if (o.item != item) continue;
                int add = Mathf.Min(max - stack.count, o.count);
                stack.count += add; o.count -= add;
                if (o.count <= 0) o.Clear();
                if (stack.count >= max) break;
            }
            Inventory.NotifyChanged();
        }

        void HideDurabilityBar()
        {
            if (durabilityBar == null) return;
            durabilityBar.fillAmount = 1f;
            durabilityBar.gameObject.SetActive(false);
        }
    }
}
