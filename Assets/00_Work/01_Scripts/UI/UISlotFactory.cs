using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI
{
    public static class UISlotFactory
    {
        public static UISlot Create(
            Transform  parent,
            GameObject slotPrefab = null,
            int        size       = 50)
        {
            GameObject obj;

            if (slotPrefab != null)
            {
                obj = Object.Instantiate(slotPrefab, parent);
                return obj.GetComponent<UISlot>()
                    ?? obj.AddComponent<UISlot>();
            }

            obj = new GameObject("UISlot");
            obj.transform.SetParent(parent, false);

            var bg   = obj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var rect       = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            // 아이콘
            var iconObj  = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            var icon     = iconObj.AddComponent<RawImage>();
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -4);

            // 카운트 텍스트
            var countObj  = new GameObject("Count");
            countObj.transform.SetParent(obj.transform, false);
            var count     = countObj.AddComponent<TMPro.TextMeshProUGUI>();
            count.fontSize  = 12;
            count.alignment = TMPro.TextAlignmentOptions.BottomRight;
            count.color     = Color.white;
            var countRect   = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(2, 2);
            countRect.offsetMax = new Vector2(-2, -2);

            // 내구도 바
            var durObj  = new GameObject("DurabilityBar");
            durObj.transform.SetParent(obj.transform, false);
            var dur     = durObj.AddComponent<Image>();
            dur.color   = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 배경색
            var durRect = durObj.GetComponent<RectTransform>();
            durRect.anchorMin = new Vector2(0, 0);
            durRect.anchorMax = new Vector2(1, 0);
            durRect.offsetMin = new Vector2(2, 2);
            durRect.offsetMax = new Vector2(-2, 5);
            durObj.SetActive(false);

            // Fill 자식
            var fillObj  = new GameObject("Fill");
            fillObj.transform.SetParent(durObj.transform, false);
            var fill     = fillObj.AddComponent<Image>();
            fill.type        = Image.Type.Filled;
            fill.fillMethod  = Image.FillMethod.Horizontal;
            fill.fillAmount  = 1f;
            fill.color       = Color.green;
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // UISlot 컴포넌트에 참조 연결
            var slot             = obj.AddComponent<UISlot>();
            slot.iconImage       = icon;
            slot.countText       = count;
            slot.slotBackground  = bg;
            slot.durabilityBar   = dur;
            slot.durabilityFill  = fill; // 연결

            return slot;
        }

        public static UISlot[] CreateSlots(
            Transform  parent,
            int        count,
            GameObject slotPrefab = null,
            int        size       = 50)
        {
            var slots = new UISlot[count];
            for (int i = 0; i < count; i++)
                slots[i] = Create(parent, slotPrefab, size);
            return slots;
        }
    }
}