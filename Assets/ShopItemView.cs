using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;

public class ShopItemView : MonoBehaviour
{
    // الشنطة تسمع لهذا
    public static System.Action<string> OnItemBought;

    // كل اللي انشرى (ينحفظ مع تقدم اللاعب)
    private static readonly System.Collections.Generic.HashSet<string> boughtIds =
        new System.Collections.Generic.HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { boughtIds.Clear(); }

    public static System.Collections.Generic.List<string> GetBoughtIds()
    {
        return new System.Collections.Generic.List<string>(boughtIds);
    }

    public static void RestoreBoughtIds(System.Collections.Generic.IEnumerable<string> ids)
    {
        boughtIds.Clear();
        if (ids != null)
            foreach (string id in ids)
                if (!string.IsNullOrEmpty(id)) boughtIds.Add(id);

        foreach (ShopItemView v in FindObjectsByType<ShopItemView>(FindObjectsInactive.Include))
        {
            v.bought = boughtIds.Contains(v.itemId);
            v.RefreshTexts();
        }
    }

    public static bool IsBought(string id) { return boughtIds.Contains(id); }
    public static void MarkBought(string id) { if (!string.IsNullOrEmpty(id)) boughtIds.Add(id); }

    // شراء من أي مكان (متجر السوق الشعبي). يرجع رسالة الخطأ، أو null لو تم
    public static string Purchase(string id, string name, int price, bool once)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return "صار خطأ";
        if (once && boughtIds.Contains(id)) return "اشتريته من قبل!";
        if (!gm.SpendMoney(price))
            return gm.money == 0 ? "ما معك ولا قرش!" : "القروش ما تكفي! المطلوب " + NaaqaUI.Ar(price) + " قرش";

        boughtIds.Add(id);
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        foreach (ShopItemView v in FindObjectsByType<ShopItemView>(FindObjectsInactive.Include))
            if (v.itemId == id) { v.bought = true; v.RefreshTexts(); }
        if (OnItemBought != null) OnItemBought(id);
        return null;
    }

    [Header("بيانات المنتج")]
    public string itemId = "camel_pen";
    public string itemName = "عزبة الإبل";
    public int price = 100;
    public bool buyOnce = true;

    [Header("قريبًا (النسخة الكاملة)")]
    public bool comingSoon = false;
    public string comingSoonButton = "في النسخة الكاملة";
    public string comingSoonMessage = "متاح في النسخة الكاملة!";
    public Color grayColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("عناصر الكرت")]
    public RTLTextMeshPro nameText;
    public RTLTextMeshPro priceText;
    public Button buyButton;
    public RTLTextMeshPro buyButtonText;

    private bool bought = false;

    void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(Buy);
    }

    void OnEnable()
    {
        if (boughtIds.Contains(itemId)) bought = true;
        RefreshTexts();
        if (comingSoon) MakeGray();
    }

    public void RefreshTexts()
    {
        SetText(nameText, itemName);
        SetText(priceText, ToArabicNumbers(price) + " قرش");

        if (comingSoon)
            SetText(buyButtonText, comingSoonButton);
        else
            SetText(buyButtonText, bought ? "تم الشراء" : "شراء");
    }

    // يخلي الكرت كله رمادي (الصور بس، النصوص تبقى مقروءة)
    void MakeGray()
    {
        foreach (Image img in GetComponentsInChildren<Image>(true))
            img.color = grayColor;
    }

    public void Buy()
    {
        if (comingSoon)
        {
            Notify(comingSoonMessage);
            return;
        }

        if (GameManager.Instance == null) return;

        if (buyOnce && bought)
        {
            Warn("اشتريته من قبل!");
            return;
        }

        if (!GameManager.Instance.SpendMoney(price))
        {
            if (GameManager.Instance.money == 0)
                Warn("ما معك ولا قرش!");
            else
                Warn("القروش ما تكفي! المطلوب " + ToArabicNumbers(price) + " قرش");
            return;
        }

        bought = true;
        boughtIds.Add(itemId);

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.UpdateMoneyText();

        Notify("تم شراء " + itemName + "!");
        RefreshTexts();

        if (OnItemBought != null)
            OnItemBought(itemId);
    }

    void SetText(RTLTextMeshPro t, string value)
    {
        if (t == null) return;
        t.text = value;
        t.ForceMeshUpdate();
    }

    void Notify(string msg)
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show(msg);
    }

    void Warn(string msg)
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(msg);
    }

    string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }
        return new string(chars);
    }
}