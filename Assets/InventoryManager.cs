using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BagItemData
{
    public string id;            // نفس Item Id في كرت المتجر
    public string displayName;   // الاسم اللي يطلع في الشنطة
    public Sprite icon;          // صورته في الشنطة
    public GameObject worldPrefab; // الشكل اللي ينحط على الخريطة (نستخدمه بعدين)
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // أي نافذة تبي تتحدث لما تتغير الشنطة تسمع لهذا
    public static System.Action OnChanged;

    [Header("كل الأغراض اللي ممكن تدخل الشنطة")]
    public List<BagItemData> catalog = new List<BagItemData>();

    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        ShopItemView.OnItemBought += Add;
    }

    void OnDisable()
    {
        ShopItemView.OnItemBought -= Add;
    }

    public void Add(string id)
    {
        if (GetData(id) == null)
        {
            Debug.LogError("الغرض " + id + " مو موجود في Catalog حق InventoryManager!", this);
            return;
        }

        if (!counts.ContainsKey(id)) counts[id] = 0;
        counts[id]++;

        Debug.Log("انضاف للشنطة: " + id + " | العدد: " + counts[id]);
        if (OnChanged != null) OnChanged();
    }

    public bool Remove(string id)
    {
        if (!counts.ContainsKey(id) || counts[id] <= 0) return false;

        counts[id]--;
        if (counts[id] == 0) counts.Remove(id);

        if (OnChanged != null) OnChanged();
        return true;
    }

    public int GetCount(string id)
    {
        return counts.ContainsKey(id) ? counts[id] : 0;
    }

    public List<string> GetOwnedIds()
    {
        return new List<string>(counts.Keys);
    }

    public BagItemData GetData(string id)
    {
        foreach (BagItemData d in catalog)
            if (d.id == id) return d;
        return null;
    }
}