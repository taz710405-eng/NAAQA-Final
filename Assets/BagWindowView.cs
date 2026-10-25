using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using System.Collections.Generic;

public class BagWindowView : MonoBehaviour
{
    // وضع التركيب بيسمع لهذا في الخطوة الجاية
    public static System.Action<string> OnPlaceRequested;

    [Header("القالب")]
    public GameObject slotTemplate;   // BagSlot
    public GameObject emptyText;      // EmptyText

    [Header("يتسكر مع النافذة")]
    public GameObject dimBackground;

    private readonly List<GameObject> spawned = new List<GameObject>();

    void OnEnable()
    {
        InventoryManager.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        InventoryManager.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (slotTemplate == null || InventoryManager.Instance == null) return;

        slotTemplate.SetActive(false);
        foreach (GameObject g in spawned) if (g != null) Destroy(g);
        spawned.Clear();

        List<string> ids = InventoryManager.Instance.GetOwnedIds();

        foreach (string id in ids)
        {
            BagItemData data = InventoryManager.Instance.GetData(id);
            if (data == null) continue;

            GameObject slot = Instantiate(slotTemplate, slotTemplate.transform.parent);
            slot.SetActive(true);
            spawned.Add(slot);

            Image icon = FindChild<Image>(slot.transform, "icon");
            if (icon != null && data.icon != null) icon.sprite = data.icon;

            // الرقم يطلع بس إذا عندك أكثر من وحدة
            int count = InventoryManager.Instance.GetCount(id);
            RTLTextMeshPro countText = FindChild<RTLTextMeshPro>(slot.transform, "CountText");
            if (countText != null)
            {
                countText.gameObject.SetActive(count > 1);
                SetText(countText, "×" + ToArabicNumbers(count));
            }

            // المربع كله زر
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                string capturedId = id;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Place(capturedId));
            }
        }

        if (emptyText != null) emptyText.SetActive(ids.Count == 0);
    }

    void Place(string id)
    {
        gameObject.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);

        if (OnPlaceRequested != null)
            OnPlaceRequested(id);
        else if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show("وضع التركيب قريب!");
    }

    T FindChild<T>(Transform root, string childName) where T : Component
    {
        foreach (T c in root.GetComponentsInChildren<T>(true))
            if (c.gameObject.name.ToLower() == childName.ToLower()) return c;
        return null;
    }

    void SetText(RTLTextMeshPro t, string value)
    {
        if (t == null) return;
        t.text = value;
        t.ForceMeshUpdate();
    }

    string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        return new string(chars);
    }
}