using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("Order Settings")]
    public int requiredMilk = 2;
    public int rewardCoins = 50;
    public GameObject orderWindow;

    [Header("Pen Upgrade")]
    public int upgradeCost = 100;
    public int camelCapacity = 1;
    public GameObject newCamel;
    public GameObject upgradeWindow;

    [Header("Final Message")]
    public string finalMessage = "بدأت رحلتك في ناقة! هلا بك!!";
    public float finalMessageTime = 5f;

    [Header("Shared")]
    public GameObject dimBackground;

    private bool isUpgraded = false;
    private bool orderDelivered = false;

    public bool OrderDelivered { get { return orderDelivered; } }
    public bool IsUpgraded { get { return isUpgraded; } }

    public void CompleteOrder()
    {
        if (orderDelivered)
        {
            NotifyWarning("تم تسليم الطلب من قبل!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager غير موجود!");
            return;
        }

        if (GameManager.Instance.milkCount < requiredMilk)
        {
            NotifyWarning("الحليب ما يكفي! المطلوب " + ToArabicNumbers(requiredMilk) + " حليب");
            Debug.LogWarning("الحليب غير كافٍ لإتمام الطلب!");
            return;
        }

        GameManager.Instance.milkCount -= requiredMilk;
        GameManager.Instance.AddMoney(rewardCoins);
        orderDelivered = true;
        RefreshUI();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.DeliverOrder);

        if (orderWindow != null) orderWindow.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);

        Debug.Log("تم تسليم الطلب! +" + rewardCoins + " قرش");
    }

    public void UpgradePen()
    {
        if (isUpgraded)
        {
            NotifyWarning("العزبة مطورة من قبل!");
            return;
        }

        if (!orderDelivered)
        {
            NotifyWarning("سلّم الطلب أول عشان تطوّر العزبة!");
            Debug.LogWarning("لازم يتسلم الطلب قبل التطوير!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager غير موجود!");
            return;
        }

        int money = GameManager.Instance.money;

        if (!GameManager.Instance.SpendMoney(upgradeCost))
        {
            if (money <= 0)
                ShowToast("ما معك ولا قرش!");
            else
                ShowToast("القروش ما تكفي! المطلوب " + ToArabicNumbers(upgradeCost) + " قرش");

            Debug.LogWarning("القروش غير كافية لتطوير العزبة!");
            return;
        }

        isUpgraded = true;
        camelCapacity++;

        if (newCamel != null)
            newCamel.SetActive(true);
        else
            Debug.LogError("New Camel غير مربوط في OrderManager!", this);

        RefreshUI();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.UpgradePen);

        if (upgradeWindow != null) upgradeWindow.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);

        if (ToastView.Instance != null)
            ToastView.Instance.Show(finalMessage, finalMessageTime);

        Debug.Log("تم تطوير العزبة! سعة الإبل: " + camelCapacity);
    }

    void ShowToast(string message)
    {
        if (ToastView.Instance != null)
            ToastView.Instance.Show(message);
        else
            Debug.LogWarning("ToastView غير موجود في المشهد!");
    }

    void NotifyWarning(string message)
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(message);
        else
            ShowToast(message);
    }

    void RefreshUI()
    {
        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
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