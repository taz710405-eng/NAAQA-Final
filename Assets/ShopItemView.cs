using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;

public class ShopItemView : MonoBehaviour
{
    // الشنطة تسمع لهذا
    public static System.Action<string> OnItemBought;

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
        RefreshTexts();
        if (comingSoon) MakeGray();
    }

    void RefreshTexts()
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