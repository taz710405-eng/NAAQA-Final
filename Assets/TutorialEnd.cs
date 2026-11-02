using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;

// ===== آخر الشرح (بعد «طوّر العزبة») =====
// (البير ومزرعة العلف صاروا في أول الشرح مع المويه: WaterSystem)
// يمشّي اللاعب خطوة خطوة، والظل يأشر على المطلوب ويمسك أي ضغطة ثانية:
//   ١. يجيه كشك البيع هدية في الشنطة، يركّبه، ويجيه أول زبون يبي حليبة وحدة يبيعها له
//   ٢. يشتري خيمة الراعي من السوق الشعبي ويركّبها
//   ٣. «يا هلا ومرحبا بك في ناقة»
// المرحلة تنحفظ مع التقدم (SaveData.endStage).
public class TutorialEnd : MonoBehaviour
{
    public const int NotStarted = 0, PlaceKiosk = 4, FirstSale = 5, BuyTent = 6, PlaceTent = 7, Welcome = 8, Done = 9;
    public const int TentPrice = 10;

    public static int Stage;
    const string Kiosk = "milk_stall", Tent = "tent";

    static TutorialEnd instance;
    float startDelay = 3f, tick;
    bool gaveMilk, gaveCoins, shown, sawProfile;
    NaaqaWindow welcome;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Stage = NotStarted; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (instance != null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        instance = new GameObject("TutorialEnd").AddComponent<TutorialEnd>();
    }

    void OnDestroy() { if (instance == this) instance = null; if (welcome != null) Destroy(welcome.gameObject); }

    // ===== للسكربتات الثانية =====
    public static bool Active { get { return Stage > NotStarted && Stage < Done; } }
    public static bool WantFirstCustomer { get { return Stage == FirstSale; } }
    // الزباين ما يجون إلا بعد ما يوصل لخطوة أول زبون
    public static bool CustomersAllowed { get { return WaterSystem.TutorialDone && (Stage == NotStarted || Stage >= FirstSale); } }

    public static void OnSale()
    {
        if (Stage != FirstSale) return;
        Stage = BuyTent;
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("كفو! أول بيعة لك من كشك البيع");
    }

    // ===== التحديث =====
    void Update()
    {
        if (Stage >= Done) { Release(); return; }
        if (!WaterSystem.TutorialDone || OnMenu()) { GuideShade.Point(null); return; }

        if (Stage < PlaceKiosk)
        {
            startDelay -= Time.unscaledDeltaTime;
            if (startDelay > 0f) return;
            // لاعب قديم خلص الشرح من زمان: ما نعيد عليه، بس نتأكد إن عنده الكشك
            int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
            if (Stage == NotStarted && (lv >= 4 || WaterSystem.StallGift)) { Stage = Done; GiveKiosk(false); return; }
            Stage = PlaceKiosk;
            GiveKiosk(true);
        }

        tick -= Time.unscaledDeltaTime;
        if (tick <= 0f) { tick = 0.25f; Advance(); }
        if (Stage < Done) Guide();
    }

    // ننقل للخطوة اللي بعدها لو خلصت
    void Advance()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        if (Stage == PlaceKiosk)
        {
            GameObject k = Placed(Kiosk);
            if (k != null && !Moving(k)) Stage = FirstSale;
            else if (k == null && inv.GetCount(Kiosk) == 0 && !PlacementManager.IsPlacing) GiveKiosk(false);
        }
        if (Stage == FirstSale)
        {
            if (Placed(Kiosk) == null) { Stage = PlaceKiosk; return; }
            // يحتاج حليبة وحدة يبيعها: لو ما عنده نعطيه وحدة
            GameManager gm = GameManager.Instance;
            if (!gaveMilk && gm != null && gm.milkCount < 1)
            {
                gaveMilk = true;
                gm.AddMilk(1);
                if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
                if (NotificationManager.Instance != null) NotificationManager.Instance.Show("عطيناك حليبة تبيعها لأول زبون");
            }
        }
        if (Stage == BuyTent)
        {
            if (ShopItemView.IsBought(Tent) || inv.GetCount(Tent) > 0 || Placed(Tent) != null) Stage = PlaceTent;
            else
            {
                // لازم يقدر يشتريها (عشان ما يعلق)
                GameManager gm = GameManager.Instance;
                if (!gaveCoins && gm != null && gm.money < TentPrice)
                {
                    gaveCoins = true;
                    gm.AddMoney(TentPrice - gm.money);
                    if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
                }
            }
        }
        if (Stage == PlaceTent)
        {
            // بعد ما يركّب الخيمة يفتحها ويشوف ملفه، ولما يسكّره يطلع الترحيب
            GameObject t = Placed(Tent);
            if (t != null && !Moving(t))
            {
                bool loggedIn = AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn;
                if (!loggedIn) Stage = Welcome;              // بدون حساب ما فيه ملف
                else if (ProfileOpen()) sawProfile = true;
                else if (sawProfile) Stage = Welcome;
            }
        }
        if (Stage == Welcome && !shown) ShowWelcome();
    }

    // الشريط فوق والظل اللي يأشر
    void Guide()
    {
        TaskBanner tb = TaskBanner.Instance;
        bool placing = PlacementManager.IsPlacing;
        bool souq = SouqScreen.Instance != null && SouqScreen.Instance.gameObject.activeInHierarchy;
        bool window = AnyWindowOpen();

        string text = null;
        Transform target = null;
        switch (Stage)
        {
            case PlaceKiosk:
                text = placing ? "حط الكشك وين تبي واضغط زر الصح" : "جاك كشك البيع هدية! ركّبه من الشنطة";
                target = window ? null : Find("BagButton");
                break;
            case FirstSale:
                text = "جاك أول زبون! اضغط عليه وبع له الحليب";
                target = window ? null : StallCustomers.WaitingBubble;
                break;
            case BuyTent:
                text = "اشترِ خيمة الراعي من السوق الشعبي";
                target = souq ? SouqScreen.Tile(Tent) : window ? null : Find("ShopButton");
                break;
            case PlaceTent:
                GameObject tent = Placed(Tent);
                if (tent != null && !placing)
                {
                    bool prof = ProfileOpen();
                    text = prof ? "هذا ملفك: صورتك واسمك وإحصائياتك. سكّره لما تخلص" : "اضغط على خيمتك وشوف ملفك";
                    target = prof || window ? null : tent.transform;
                }
                else
                {
                    text = placing ? "حط الخيمة وين تبي واضغط زر الصح" : "ركّب خيمة الراعي من الشنطة";
                    target = souq ? SouqScreen.BackButton : window ? null : Find("BagButton");
                }
                break;
            case Welcome:
                text = "يا هلا ومرحبا بك في ناقة!";
                break;
        }

        if (tb != null)
        {
            if (!tb.gameObject.activeSelf) tb.gameObject.SetActive(true);
            CanvasGroup g = tb.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = 1f;
            if (tb.taskText != null && text != null && tb.taskText.text != text) tb.taskText.text = text;
        }
        // دائرة الشرح القديمة ما نحتاجها هنا
        TutorialSpotlight spot = TutorialSpotlight.Instance;
        if (spot != null) { spot.custom = null; spot.customHide = true; }
        GuideShade.Point(placing ? null : target);
    }

    // خلص الشرح: نرجّع كل شي
    bool released;
    void Release()
    {
        if (released || !WaterSystem.TutorialDone) return;
        released = true;
        GuideShade.Point(null);
        TutorialSpotlight spot = TutorialSpotlight.Instance;
        if (spot != null) { spot.custom = null; spot.customHide = false; spot.Hide(); }
        TaskBanner tb = TaskBanner.Instance;
        if (tb != null && tb.gameObject.activeSelf) tb.gameObject.SetActive(false);
    }

    // كشك البيع هدية في الشنطة
    static void GiveKiosk(bool loud)
    {
        WaterSystem.StallGift = true;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;
        if (Placed(Kiosk) != null || inv.GetCount(Kiosk) > 0) return;
        inv.Add(Kiosk);
        ShopItemView.MarkBought(Kiosk);
        // (الشريط فوق يقول له، ما نكرر بإشعار)
    }

    // ===== الترحيب =====
    void ShowWelcome()
    {
        shown = true;
        GuideShade.Point(null);
        welcome = NaaqaWindow.Create("Welcome", "يا هلا ومرحبا بك في ناقة!", "camel_index", new Vector2(1000, 560), 800);
        welcome.subtitle.text = "خلصت الشرح، والعزبة صارت لك";
        RTLTextMeshPro body = NaaqaUI.Text(welcome.body, "Body",
            "أكّل إبلك واحلبها، وبع حليبك في كشك البيع، وازرع علفك بنفسك.\nكل ما كبر مستواك ينفتح لك شي جديد: سلالات نادرة، سباق الهجن، المزاين، والعمال.",
            30, NaaqaUI.Cream, TextAlignmentOptions.Center, false);
        NaaqaUI.Place(body.rectTransform, new Vector2(0.5f, 1), new Vector2(880, 220), new Vector2(0, -10));
        RTLTextMeshPro bt;
        Button go = NaaqaUI.MakeButton(welcome.body, "Go", "يلا نبدأ", NaaqaUI.ButtonStyle.Primary, 32, out bt);
        NaaqaUI.Place((RectTransform)go.transform, new Vector2(0.5f, 0), new Vector2(420, 92), new Vector2(0, 24));
        go.onClick.AddListener(() => welcome.Close());
        welcome.onClose = () => { Stage = Done; };
        welcome.Open();
    }

    // ===== أدوات =====
    static GameObject Placed(string id)
    {
        if (PlacementManager.Instance == null) return null;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == id && p.go != null) return p.go;
        return null;
    }

    static bool Moving(GameObject go)
    {
        return PlacementManager.IsPlacing && go != null && go == PlacementManager.MovingObject;
    }

    static bool ProfileOpen()
    {
        return ProfileScreen.Instance != null && ProfileScreen.Instance.gameObject.activeInHierarchy;
    }

    static Transform Find(string name)
    {
        GameObject g = GameObject.Find(name);
        return g != null ? g.transform : null;
    }

    float windowCheck;
    bool windowCache;
    bool AnyWindowOpen()
    {
        windowCheck -= Time.unscaledDeltaTime;
        if (windowCheck > 0f) return windowCache;
        windowCheck = 0.2f;
        windowCache = false;
        foreach (NaaqaWindow w in FindObjectsByType<NaaqaWindow>(FindObjectsInactive.Exclude))
            if (w.IsOpen && w != welcome) { windowCache = true; break; }
        return windowCache;
    }

    static bool OnMenu()
    {
        UIManager ui = UIManager.Instance;
        return ui != null && ((ui.startScreen != null && ui.startScreen.activeInHierarchy) || (ui.hud != null && !ui.hud.activeInHierarchy));
    }
}
