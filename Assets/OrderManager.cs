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
    // تطوير الحوش في الشرح يكبّره بس (ما يعطي ناقة ثانية). الحفظ القديم اللي جته الناقة تبقى له
    public static bool SecondCamel;
    public GameObject upgradeWindow;

    [Header("Final Message")]
    public string finalMessage = "بدأت رحلتك في ناقة! هلا بك!!";
    public float finalMessageTime = 5f;

    [Header("Shared")]
    public GameObject dimBackground;

    [Header("طلبات تتجدد (بعد الشرح)")]
    public int minMilk = 2;
    public int maxMilk = 6;
    public int coinsPerMilk = 20;
    public int maxBonus = 20;          // مكافأة إضافية عشوائية (من مضاعفات ٥)
    public string newOrderMessage = "وصلك طلب جديد!";
    public float orderCooldown = 25f;  // كم ثانية بين الطلب والطلب اللي بعده

    private bool isUpgraded = false;
    private bool orderDelivered = false;
    private int ordersCompleted = 0;
    private float waitLeft = 0f;       // الوقت الباقي للطلب الجاي
    private bool unseen = false;       // وصل طلب واللاعب ما فتح النافذة
    private GameObject badge;          // العلامة الحمراء على زر الطلبات

    // ننتظر الطلب الجاي؟
    public bool IsWaiting { get { return isUpgraded && waitLeft > 0f; } }
    public float WaitLeft { get { return waitLeft; } }

    public bool OrderDelivered { get { return orderDelivered; } }
    public bool IsUpgraded { get { return isUpgraded; } }
    public int OrdersCompleted { get { return ordersCompleted; } }

    // فيه طلب ينتظر التسليم؟ (وقت الشرح طلب واحد بس، وبعد التطوير تتجدد)
    public bool HasOpenOrder { get { return !orderDelivered || (isUpgraded && waitLeft <= 0f); } }

    void Start()
    {
        // الشرح: الطلب الأول ثابت ٢٠ قرش، وتطوير الحوش الأول مجاني (الشرح يعلّم بس)
        upgradeCost = 0;
        if (!orderDelivered) rewardCoins = 20;
        // العلامة الموجودة على زر الطلبات (OrderBadge)
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "OrdersButton") continue;
            Transform b = t.Find("OrderBadge");
            if (b != null) badge = b.gameObject;
            break;
        }
        // أول طلب (وقت الشرح) جديد بعد
        unseen = !orderDelivered;
    }

    void Update()
    {
        if (isUpgraded && waitLeft > 0f)
        {
            waitLeft -= Time.deltaTime;
            if (waitLeft <= 0f)
            {
                waitLeft = 0f;
                NewOrder();
                unseen = true;
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.Show(newOrderMessage);
            }
        }

        // فتح نافذة الطلبات = شافه
        if (orderWindow != null && orderWindow.activeInHierarchy) unseen = false;

        if (badge != null)
        {
            bool show = unseen && HasOpenOrder;
            if (badge.activeSelf != show) badge.SetActive(show);
        }
    }

    // نافذة الطلبات الجديدة فتحت = اللاعب شاف الطلب
    public void MarkSeen() { unseen = false; }

    // ===== طلب جديد =====
    public void NewOrder()
    {
        int top = Mathf.Clamp(minMilk + 1 + ordersCompleted / 2, minMilk, Mathf.Max(minMilk, maxMilk));
        requiredMilk = Random.Range(minMilk, top + 1);
        rewardCoins = requiredMilk * coinsPerMilk + Random.Range(0, maxBonus / 5 + 1) * 5;
    }

    // من الحفظ: يرجّع حالة الطلب والتطوير بدون مكافآت ولا رسائل
    public void RestoreState(bool delivered, bool upgraded, int savedMilk = 0, int savedReward = 0, int savedCompleted = 0, float savedWait = 0f)
    {
        if (delivered) orderDelivered = true;
        waitLeft = Mathf.Max(0f, savedWait);
        unseen = false;
        if (savedCompleted > ordersCompleted) ordersCompleted = savedCompleted;
        if (savedMilk > 0) requiredMilk = savedMilk;
        if (savedReward > 0) rewardCoins = savedReward;

        if (upgraded && !isUpgraded)
        {
            isUpgraded = true;
            camelCapacity++;
            if (newCamel != null && SecondCamel) newCamel.SetActive(true);
        }
    }

    public void CompleteOrder()
    {
        if (!HasOpenOrder)
        {
            NotifyWarning("تم تسليم الطلب! طوّر العزبة عشان تجيك طلبات جديدة");
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
        int paid = rewardCoins;
        orderDelivered = true;
        ordersCompleted++;
        Quests.Add("orders");
        RefreshUI();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.DeliverOrder);

        if (orderWindow != null) orderWindow.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);

        Debug.Log("تم تسليم الطلب! +" + paid + " قرش");

        // خبرة على الطلب
        LevelManager.AddXp(LevelManager.XpPerOrder);

        // بعد الشرح: الطلب الجاي يوصل بعد شوي
        if (isUpgraded)
        {
            waitLeft = orderCooldown;
            unseen = false;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.Show("+" + ToArabicNumbers(paid) + " قرش");
        }
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

        // (ما نعطيه ناقة ثانية: الحوش يتطور بس)

        RefreshUI();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.UpgradePen);

        if (upgradeWindow != null) upgradeWindow.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);


        // خلص الشرح: تبدأ الطلبات المتجددة
        NewOrder();
        waitLeft = 0f;
        unseen = true;

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