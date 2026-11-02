using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== شكل واجهة اللعب (HUD) =====
// يركّب نفسه لحاله ويوحّد كل الأيقونات بنفس الستايل (دايرة غامقة شفافة ورسمة كريمية):
// - فوق يسار: القروش، الذهب، الحليب، العلف، (والماء لو عندك بير أو خزان)
// - فوق يمين: المستوى بالزاوية، وجنبه الإعدادات والبريد وحسابي
// - يسار: سباق الهجن، المزاين، الشنطة، وضع التعديل
// - تحت يسار: السوق الشعبي (فيه المتجر والعلف وسوق الإبل)
// - يمين: الطلبات، المهام، وتطوير الحوش
// ما يعدّل main.unity، كل شي يصير وقت التشغيل.
public class HudSkin : MonoBehaviour
{
    public static HudSkin Instance;

    OrderManager order;
    RTLTextMeshPro waterText, mailBadgeText;
    GameObject datesBar;
    RectTransform topBar;
    float topFitTimer;
    RTLTextMeshPro datesText;
    int shownDates = -1;
    float datesCheck;
    GameObject waterBar, mailBadge, tasksBadge, ordersBtn, indexBtn, workersBtn, bagBadge;
    Image raceImg, mazImg, workImg;
    GameObject raceLock, mazLock, workLock;
    int shownWater = -1, shownCap = -1, shownMail = -1, shownLevel = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (UIManager.Instance == null || UIManager.Instance.hud == null) return;
        if (FindAnyObjectByType<HudSkin>(FindObjectsInactive.Include) != null) return;
        UIManager.Instance.hud.AddComponent<HudSkin>();
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    IEnumerator Start()
    {
        order = FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
        // ننتظر فريم عشان كل السكربتات الثانية تخلص Awake/Start (ونستبدل أزرارها بعدها)
        yield return null;
        // كل خطوة لحالها: لو وحدة صار فيها خطأ، الباقي يكمل
        Safe(SkinTopBar, "الشريط فوق");
        Safe(SkinButtons, "الأزرار");
        Safe(BuildLeftColumn, "أزرار اليسار");
        Safe(BuildTopRight, "فوق يمين");
    }

    static void Safe(System.Action a, string what)
    {
        try { a(); }
        catch (System.Exception e) { Debug.LogError("HudSkin (" + what + "): " + e); }
    }

    // ===== أدوات =====
    static Transform FindDeep(string name)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == name && t.GetComponentInParent<Canvas>(true) != null) return t;
        return null;
    }

    // نستبدل كل اللي يسويه الزر (حتى اللي مربوط في المشهد) بدالة وحدة
    static void Rewire(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(a);
        b.onClick.AddListener(() => { if (SoundManager.Instance != null) SoundManager.Instance.PlayClick(); });
    }

    bool TutorialDone { get { return order == null || order.IsUpgraded; } }

    static bool Busy()
    {
        return PlacementManager.IsPlacing || EditMode.Active;
    }

    // ===== الشريط فوق يسار =====
    void SkinTopBar()
    {
        Transform top = FindDeep("TopBar");
        if (top == null) return;
        topBar = (RectTransform)top;
        // الشريط كان مثبت على نص الشاشة: على الآيباد ينزل تحت، وعلى الجوال العريض يدخل لجوا.
        // نثبته على الزاوية فوق يسار بنفس مكانه على ١٩٢٠×١٠٨٠
        if (topBar.anchorMin == new Vector2(0.5f, 0.5f) && topBar.anchorMax == new Vector2(0.5f, 0.5f))
        {
            Vector2 p = topBar.anchoredPosition;
            topBar.anchorMin = topBar.anchorMax = new Vector2(0f, 1f);
            topBar.anchoredPosition = p + new Vector2(960f, -540f);
        }
        // مسافة بين العدادات عشان ما تطلع على بعض
        HorizontalOrVerticalLayoutGroup lg = top.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (lg != null) lg.spacing = Mathf.Max(lg.spacing, 40f);

        // القروش والحليب ما لها ➕ (تكسبها من اللعب)، والعلف ➕ يفتح شراء العلف
        Skin(top.Find("bar_coins"), "coin", "PlusButton", null);
        Skin(top.Find("bar_milk"), "milk", "PlusButton", null);
        Skin(top.Find("bar_feed"), "feed", "PlusButton (1)", () => FeedStoreScreen.Show());

        // المويه (بعد ما يركب بير): ➕ يطلب وايت
        RTLTextMeshPro wt;
        RectTransform wb = NewBar(top, "bar_water", "well", out wt, () => WaterTruckScreen.Show(), 270f);
        waterBar = wb.gameObject;
        waterText = wt;
        waterBar.SetActive(false);

        // التمر: يطلع بس لو عنده مزرعة تمر (أو تمر)
        RTLTextMeshPro dtx;
        RectTransform db = NewBar(top, "bar_dates", "dates_farm", out dtx, null);
        datesBar = db.gameObject;
        datesText = dtx;
        // نفس شكل رقم القروش والحليب بالضبط (بالنص، عريض، بدون تصغير)
        dtx.enableAutoSizing = false;
        dtx.fontSize = 32;
        dtx.fontStyle = TMPro.FontStyles.Bold;
        dtx.alignment = TextAlignmentOptions.Center;
        dtx.rectTransform.offsetMin = new Vector2(84, 30);
        dtx.rectTransform.offsetMax = new Vector2(-12, -20);
        datesBar.SetActive(false);
        // التمر جنب العلف (قبل المويه)
        Transform feedBar = top.Find("bar_feed");
        if (feedBar != null) datesBar.transform.SetSiblingIndex(feedBar.GetSiblingIndex() + 1);

    }

    void Skin(Transform bar, string icon, string plusName, UnityEngine.Events.UnityAction onPlus)
    {
        if (bar == null) return;
        Image img = bar.GetComponent<Image>();
        if (img != null) { img.sprite = null; img.color = new Color(0, 0, 0, 0); img.raycastTarget = false; }

        Image pill = NaaqaUI.Img(bar, "Pill", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.72f), 30f);
        pill.rectTransform.anchorMin = Vector2.zero; pill.rectTransform.anchorMax = Vector2.one;
        pill.rectTransform.offsetMin = new Vector2(38, 20); pill.rectTransform.offsetMax = new Vector2(-4, -20);
        pill.transform.SetAsFirstSibling();
        bool sadu = SaduFit.Apply(pill, "pill");
        if (!sadu)
        {
            Image line = NaaqaUI.Img(pill.transform, "Line", NaaqaUI.RoundedOutline(), new Color(1f, 0.8f, 0.5f, 0.35f), 30f);
            NaaqaUI.Stretch(line.rectTransform);
        }

        Image ic = NaaqaUI.IconImg(bar, icon, 80);
        NaaqaUI.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(80, 80), new Vector2(0, 0));
        ic.transform.SetSiblingIndex(1);

        foreach (TMP_Text t in bar.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.transform.parent != bar) continue;
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(84, sadu ? 32 : 20); rt.offsetMax = new Vector2(-58, -22);   // فوق شريط السدو
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = 32;
            t.enableAutoSizing = false;
            t.color = NaaqaUI.Cream;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        Transform plus = bar.Find(plusName);
        if (plus != null && onPlus == null)
        {
            plus.gameObject.SetActive(false);
            foreach (TMP_Text t in bar.GetComponentsInChildren<TMP_Text>(true))
                if (t.transform.parent == bar) t.rectTransform.offsetMax = new Vector2(-12, -22);
        }
        else if (plus != null)
        {
            SkinPlus(plus);
            ComingSoonButton cs = plus.GetComponent<ComingSoonButton>();
            if (cs != null) cs.enabled = false;
            FeedShopButton fs = plus.GetComponent<FeedShopButton>();
            if (fs != null) Destroy(fs);
            Rewire(plus.GetComponent<Button>(), () => { if (!Busy()) onPlus(); });
        }
    }

    static void SkinPlus(Transform plus)
    {
        Image pi = plus.GetComponent<Image>();
        Texture2D sp = Resources.Load<Texture2D>("NaaqaUI/sadu_plus");
        RectTransform prt = (RectTransform)plus;
        foreach (Transform c in plus) c.gameObject.SetActive(false);
        if (pi != null && sp != null)
        {
            // زر ➕ السدو (صورة جاهزة)
            pi.sprite = NaaqaUI.Load("sadu_plus"); pi.color = Color.white; pi.type = Image.Type.Simple; pi.preserveAspect = true;
            NaaqaUI.Place(prt, new Vector2(1, 0.5f), new Vector2(54, 54), new Vector2(-8, 0));
            return;
        }
        if (pi != null) { pi.sprite = NaaqaUI.Circle(); pi.color = NaaqaUI.Orange; pi.type = Image.Type.Simple; }
        NaaqaUI.Place(prt, new Vector2(1, 0.5f), new Vector2(46, 46), new Vector2(-10, 0));
        NaaqaUI.PlusSign(plus, 24f, false, Color.white);
    }

    RectTransform NewBar(Transform top, string name, string icon, out RTLTextMeshPro text, UnityEngine.Events.UnityAction onPlus, float width = 230f)
    {
        RectTransform bar = NaaqaUI.Rect(name, top);
        bar.anchorMin = bar.anchorMax = Vector2.zero;
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(width, 100);
        LayoutElement le = bar.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = width;
        le.preferredHeight = le.minHeight = 100;

        Image pill = NaaqaUI.Img(bar, "Pill", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.72f), 30f);
        pill.rectTransform.anchorMin = Vector2.zero; pill.rectTransform.anchorMax = Vector2.one;
        pill.rectTransform.offsetMin = new Vector2(38, 20); pill.rectTransform.offsetMax = new Vector2(-4, -20);
        bool sadu = SaduFit.Apply(pill, "pill");
        if (!sadu)
        {
            Image line = NaaqaUI.Img(pill.transform, "Line", NaaqaUI.RoundedOutline(), new Color(1f, 0.8f, 0.5f, 0.35f), 30f);
            NaaqaUI.Stretch(line.rectTransform);
        }

        Image ic = NaaqaUI.IconImg(bar, icon, 80);
        NaaqaUI.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(80, 80), Vector2.zero);

        text = NaaqaUI.Text(bar, "Text", "٠", 32, NaaqaUI.Cream, TextAlignmentOptions.Center);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(84, sadu ? 32 : 20);
        text.rectTransform.offsetMax = new Vector2(onPlus != null ? -68 : -10, -22);
        // الرقم يصغر لحاله لو طويل (ما يدخل تحت زر ➕)
        text.enableAutoSizing = true; text.fontSizeMax = 32; text.fontSizeMin = 16;

        if (onPlus != null)
        {
            Image p = NaaqaUI.Img(bar, "PlusButton", NaaqaUI.Circle(), NaaqaUI.Orange);
            p.raycastTarget = true;
            Button b = p.gameObject.AddComponent<Button>();
            b.targetGraphic = p;
            p.gameObject.AddComponent<NaaqaButtonFx>();
            SkinPlus(p.transform);
            Rewire(b, () => { if (!Busy()) onPlus(); });
        }
        return bar;
    }

    // ===== الأزرار الموجودة في المشهد =====
    void SkinButtons()
    {
        // السوق الشعبي (كان «المتجر»): فيه المتجر والزينة والعلف وبسطتك وسوق الإبل
        Transform shop = FindDeep("ShopButton");
        if (shop != null)
        {
            Icon(shop, "souq", "السوق الشعبي", 120);
            Rewire(shop.GetComponent<Button>(), () => { if (!Busy()) SouqScreen.Show(); });
            LockUntilTutorial lk = shop.GetComponent<LockUntilTutorial>();
            if (lk != null)
            {
                lk.lockedMessage = "يفتح السوق الشعبي بعد ما تخلص المهام!";
                lk.unlockedMessage = "انفتح السوق الشعبي! فيه المتجر والعلف وسوق الإبل";
            }
        }

        Transform bag = FindDeep("BagButton");
        if (bag != null)
        {
            bag.SetParent(transform, false);
            Icon(bag, "bag", "الشنطة", 100);
            NaaqaUI.Place((RectTransform)bag, new Vector2(1, 0.5f), new Vector2(100, 100), new Vector2(-44, 80));
            Rewire(bag.GetComponent<Button>(), () => { if (!PlacementManager.IsPlacing) BagScreen.Show(); });
            Transform bb = bag.Find("OrderBadge");   // النقطة الحمرا: بس لو فيه شي في الشنطة
            if (bb != null) { bagBadge = bb.gameObject; bagBadge.SetActive(false); }
        }

        // الطلبات: وقت الشرح نفس النافذة القديمة (عشان الشرح يأشر عليها)، وبعده النافذة الجديدة
        Transform orders = FindDeep("OrdersButton");
        ordersBtn = orders != null ? orders.gameObject : null;
        if (orders != null)
        {
            // وقت الشرح بس: مكانه في عمود اليمين (مكان سجل الإبل، اللي يطلع بعد الشرح)
            orders.SetParent(transform, false);
            Icon(orders, "orders", "الطلبات", 100);
            NaaqaUI.Place((RectTransform)orders, new Vector2(1, 0.5f), new Vector2(100, 100), new Vector2(-44, -90));
            Rewire(orders.GetComponent<Button>(), () =>
            {
                if (Busy()) return;
                // بعد الشرح الطلبات صارت من زباين البسطة، والزر يختفي
                if (UIManager.Instance != null) UIManager.Instance.OpenOrders();
            });
        }

        Transform tasks = FindDeep("TasksButton");
        if (tasks != null)
        {
            // فوق مع البريد والإعدادات (كان فوق الحوش ويغطيه)
            tasks.SetParent(transform, false);
            Icon(tasks, "tasks", "المهام", 100);
            NaaqaUI.Place((RectTransform)tasks, new Vector2(1, 0.5f), new Vector2(100, 100), new Vector2(-44, 250));
            Rewire(tasks.GetComponent<Button>(), () =>
            {
                if (Busy()) return;
                if (TutorialDone) TasksScreen.Show();
                else if (UIManager.Instance != null) UIManager.Instance.OpenTasks();
            });
            Image tb = NaaqaUI.Img(tasks, "Ready", NaaqaUI.Circle(), new Color32(226, 68, 48, 255));
            NaaqaUI.Place(tb.rectTransform, new Vector2(1, 1), new Vector2(30, 30), new Vector2(2, 2));
            tasksBadge = tb.gameObject;
            tasksBadge.SetActive(false);
        }

        Transform home = FindDeep("HomeButton");
        if (home != null)
        {
            // نطلعه من داخل زر المتجر ونحطه تحت يمين لحاله
            home.SetParent(transform, false);
            home.gameObject.SetActive(true);
            NaaqaUI.Place((RectTransform)home, new Vector2(1, 0), new Vector2(112, 112), new Vector2(-48, 70));
            Icon(home, "upgrade", "تطوير الحوش", 112);
            Rewire(home.GetComponent<Button>(), () =>
            {
                if (Busy()) return;
                if (TutorialDone) PenScreen.Show();
                else if (UIManager.Instance != null) UIManager.Instance.OpenUpgrade();
            });
        }

        Transform settings = FindDeep("SettingsButton");
        if (settings != null)
        {
            Icon(settings, "settings", null, 84);
            Rewire(settings.GetComponent<Button>(), () => SettingsScreen.Show());
        }

        Transform mail = FindDeep("messageButton (1)");
        if (mail != null)
        {
            Icon(mail, "mail", null, 84);
            Rewire(mail.GetComponent<Button>(), () => MailScreen.Show());
            Image b = NaaqaUI.Img(mail, "Unread", NaaqaUI.Circle(), new Color32(226, 68, 48, 255));
            NaaqaUI.Place(b.rectTransform, new Vector2(1, 1), new Vector2(36, 36), new Vector2(2, 2));
            mailBadgeText = NaaqaUI.Text(b.transform, "N", "", 20, Color.white, TextAlignmentOptions.Center, true);
            NaaqaUI.Stretch(mailBadgeText.rectTransform, 0, 0, 0, 2);
            mailBadge = b.gameObject;
            mailBadge.SetActive(false);
        }
    }

    // يبدّل شكل الزر بأيقونة موحّدة، ويحط اسمه تحته
    static void Icon(Transform btn, string icon, string label, float size)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = NaaqaUI.Icon(icon);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
        RectTransform rt = (RectTransform)btn;
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.one;

        // النص القديم يختفي، ونحط اسم موحّد
        // (نطفي النص نفسه بس، لأن فيه أزرار داخل نصوص قديمة، مثل زر التطوير داخل نص المتجر)
        foreach (TMP_Text t in btn.GetComponentsInChildren<TMP_Text>(true))
            if (t.transform.parent == btn) t.enabled = false;
        if (!string.IsNullOrEmpty(label)) NaaqaUI.Caption(btn, label, 17f);

        Transform badge = btn.Find("OrderBadge");
        if (badge != null) NaaqaUI.Place((RectTransform)badge, new Vector2(1, 1), new Vector2(34, 34), new Vector2(2, 2));
    }

    // ===== يسار: السباق، المزاين، وضع التعديل =====
    void BuildLeftColumn()
    {
        Transform hud = transform;

        Button race = NaaqaUI.IconButton(hud, "race", "سباق الهجن", 104, () => OpenLocked(GameRules.RaceUnlock, "سباق الهجن", RaceScreen.Show));
        NaaqaUI.Place((RectTransform)race.transform, new Vector2(0, 0.5f), new Vector2(104, 104), new Vector2(38, 290));
        raceImg = race.GetComponent<Image>();
        raceLock = LockMark(race.transform, GameRules.RaceUnlock);

        Button maz = NaaqaUI.IconButton(hud, "mazayen", "المزاين", 104, () => OpenLocked(GameRules.MazayenUnlock, "مهرجان المزاين", MazayenScreen.Show));
        NaaqaUI.Place((RectTransform)maz.transform, new Vector2(0, 0.5f), new Vector2(104, 104), new Vector2(38, 110));
        mazImg = maz.GetComponent<Image>();
        mazLock = LockMark(maz.transform, GameRules.MazayenUnlock);

        // سوق الإبل (كان داخل السوق الشعبي)
        Button camels = NaaqaUI.IconButton(hud, "camel_market", "سوق الإبل", 104, () => OpenLocked(1, "سوق الإبل", CamelMarketScreen.Show));
        NaaqaUI.Place((RectTransform)camels.transform, new Vector2(0, 0.5f), new Vector2(104, 104), new Vector2(38, -70));

        Button edit = NaaqaUI.IconButton(hud, "edit", "تعديل", 92, () =>
        {
            if (PlacementManager.IsPlacing) return;
            EditMode.Toggle();
        });
        // يمين: المهام، الشنطة، تعديل (عشان اليسار ما يزدحم)
        NaaqaUI.Place((RectTransform)edit.transform, new Vector2(1, 0.5f), new Vector2(96, 96), new Vector2(-46, -260));

        // سجل الإبل: تحت الشنطة وفوق تعديل
        Button index = NaaqaUI.IconButton(hud, "camel_index", "سجل الإبل", 100, () => { if (!Busy() && !PlacementManager.IsPlacing) CamelIndexScreen.Show(); });
        NaaqaUI.Place((RectTransform)index.transform, new Vector2(1, 0.5f), new Vector2(100, 100), new Vector2(-44, -90));
        indexBtn = index.gameObject;

        // العمال: تحت جنب تطوير الحوش
        Button workers = NaaqaUI.IconButton(hud, "worker", "العمال", 100, () => { if (!PlacementManager.IsPlacing) OpenLocked(GameRules.WorkersUnlock, "التوظيف", WorkerScreen.Show); });
        workImg = workers.GetComponent<Image>();
        workLock = LockMark(workers.transform, GameRules.WorkersUnlock);
        NaaqaUI.Place((RectTransform)workers.transform, new Vector2(1, 0), new Vector2(100, 100), new Vector2(-186, 76));
        workersBtn = workers.gameObject;
    }

    static GameObject LockMark(Transform btn, int level)
    {
        Image l = NaaqaUI.IconImg(btn, "lock", 46);
        NaaqaUI.Place(l.rectTransform, new Vector2(1, 1), new Vector2(46, 46), new Vector2(10, 10));
        return l.gameObject;
    }

    // اسم الزر: وقت القفل يصير «مستوى ٩»
    static void SetCaption(Image btn, bool open, string name, int level)
    {
        Transform c = btn.transform.Find("Caption");
        if (c == null) return;
        RTLTextMeshPro t = c.GetComponentInChildren<RTLTextMeshPro>();
        t.text = open ? name : "مستوى " + NaaqaUI.Ar(level);
        t.color = open ? Color.white : NaaqaUI.OrangeLight;
    }

    void OpenLocked(int level, string what, System.Action open)
    {
        if (Busy()) return;
        if (!TutorialDone)
        {
            Warn("خلّص المهام أول!");
            return;
        }
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv < level)
        {
            Warn(what + " يفتح في المستوى " + NaaqaUI.Ar(level) + " (أنت في " + NaaqaUI.Ar(lv) + ")");
            return;
        }
        open();
    }

    static void Warn(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(msg);
    }

    // ===== فوق يمين: المستوى بالزاوية، وجنبه الإعدادات والبريد وحسابي =====
    void BuildTopRight()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.MoveToCorner();

        Transform settings = FindDeep("SettingsButton");
        Transform mail = FindDeep("messageButton (1)");
        if (settings != null) NaaqaUI.Place((RectTransform)settings, new Vector2(1, 1), new Vector2(84, 84), new Vector2(-392, -12));
        if (mail != null) NaaqaUI.Place((RectTransform)mail, new Vector2(1, 1), new Vector2(84, 84), new Vector2(-484, -12));

        // حسابي: من خيمة الراعي أو من الإعدادات
    }

    // الشاشات الضيقة (آيباد): لو العدادات أعرض من المكان اللي قبل البريد والإعدادات والمستوى، نصغّرها شوي
    void FitTopBar()
    {
        if (topBar == null) return;
        RectTransform canvasRt = topBar.root as RectTransform;
        Canvas cv = topBar.GetComponentInParent<Canvas>();
        if (cv != null) canvasRt = (RectTransform)cv.rootCanvas.transform;
        if (canvasRt == null) return;
        HorizontalOrVerticalLayoutGroup lg = topBar.GetComponent<HorizontalOrVerticalLayoutGroup>();
        float spacing = lg != null ? lg.spacing : 0f;
        float row = 0f; int n = 0;
        foreach (Transform c in topBar)
        {
            if (!c.gameObject.activeSelf) continue;
            LayoutElement le = c.GetComponent<LayoutElement>();
            float w = le != null && le.preferredWidth > 0 ? le.preferredWidth : ((RectTransform)c).rect.width;
            row += w; n++;
        }
        if (n > 1) row += spacing * (n - 1);
        float left = topBar.anchoredPosition.x - topBar.rect.width * topBar.pivot.x;
        float avail = canvasRt.rect.width - 640f - left;   // ٦٤٠ = البريد والإعدادات والمستوى فوق يمين + فراغ بين الماء والبريد
        float k = row > 1f ? Mathf.Clamp(avail / row, 0.6f, 1f) : 1f;
        if (Mathf.Abs(topBar.localScale.x - k) > 0.01f) topBar.localScale = new Vector3(k, k, 1f);
    }

    void Update()
    {
        topFitTimer -= Time.unscaledDeltaTime;
        if (topFitTimer <= 0f) { topFitTimer = 0.5f; FitTopBar(); }
        FarmStore st = FarmStore.Instance;
        if (st != null)
        {
            if (datesBar != null)
            {
                datesCheck -= Time.unscaledDeltaTime;
                if (datesCheck <= 0f)
                {
                    datesCheck = 1f;
                    bool has = st.Dates > 0;
                    if (!has && PlacementManager.Instance != null)
                        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced()) if (p.id == "date_farm") { has = true; break; }
                    if (!has && InventoryManager.Instance != null) has = InventoryManager.Instance.GetCount("date_farm") > 0;
                    if (datesBar.activeSelf != has) datesBar.SetActive(has);
                }
                if (datesBar.activeSelf && st.Dates != shownDates) { shownDates = st.Dates; datesText.text = NaaqaUI.Ar(shownDates); }
            }
            if (waterBar != null)
            {
                bool show = st.WaterCap > 0;
                if (waterBar.activeSelf != show) waterBar.SetActive(show);
                if (show && (st.Water != shownWater || st.WaterCap != shownCap))
                {
                    shownWater = st.Water; shownCap = st.WaterCap;
                    waterText.text = NaaqaUI.Ar(shownWater) + "/" + NaaqaUI.Ar(shownCap);
                }
            }
        }

        if (mailBadge != null && MailBox.Unread != shownMail)
        {
            shownMail = MailBox.Unread;
            mailBadge.SetActive(shownMail > 0);
            mailBadgeText.text = NaaqaUI.Ar(Mathf.Min(shownMail, 9));
        }

        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv != shownLevel && raceImg != null)
        {
            shownLevel = lv;
            bool r = lv >= GameRules.RaceUnlock, m = lv >= GameRules.MazayenUnlock;
            raceImg.color = r ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            mazImg.color = m ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            raceLock.SetActive(!r);
            mazLock.SetActive(!m);
            SetCaption(raceImg, r, "سباق الهجن", GameRules.RaceUnlock);
            SetCaption(mazImg, m, "المزاين", GameRules.MazayenUnlock);
            bool w = lv >= GameRules.WorkersUnlock;
            if (workImg != null)
            {
                workImg.color = w ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
                workLock.SetActive(!w);
                SetCaption(workImg, w, "العمال", GameRules.WorkersUnlock);
            }
        }

        // تنبيه الأذان انشال

        if (ordersBtn != null && ordersBtn.activeSelf == TutorialDone) ordersBtn.SetActive(!TutorialDone);
        if (indexBtn != null && indexBtn.activeSelf != TutorialDone) indexBtn.SetActive(TutorialDone);
        // زر العمال يطلع بعد الشرح، وعليه قفل و«مستوى ١٣» لين يفتح (مثل السباق والمزاين)
        if (workersBtn != null && workersBtn.activeSelf != TutorialDone) workersBtn.SetActive(TutorialDone);

        if (tasksBadge != null)
        {
            bool show = TutorialDone && Quests.ReadyCount > 0;
            if (tasksBadge.activeSelf != show) tasksBadge.SetActive(show);
        }

        if (bagBadge != null)
        {
            bool any = false;
            if (InventoryManager.Instance != null)
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in InventoryManager.Instance.GetAllCounts())
                    if (kv.Value > 0) { any = true; break; }
            if (bagBadge.activeSelf != any) bagBadge.SetActive(any);
        }
    }
}
