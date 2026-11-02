using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// ===== المويه =====
// البير يشيل ٥٠ مويه (تقدر تركب أكثر من بير). كل ناقة تشرب حبة مع كل أكلة،
// والمزرعة تشرب حبة لكل حبة علف أو تمر تطلعها.
// إذا خلصت المويه تطلب وايت يجي من الطريق العام: ١٠ طن بـ٢٠٠، ٢٥ بـ٤٥٠، ٥٠ بـ٩٠٠ (كل طن = حبة مويه).
// أول ما يخلص الشرح يجيك بير هدية في الشنطة، ويتعبى أول ما تركبه.
public class WaterSystem : MonoBehaviour
{
    public static WaterSystem Instance;

    public static int[] TruckTons { get { return GameRules.TruckTons; } }
    public static int[] TruckPrices { get { return GameRules.TruckPrices; } }

    // ٠ = ما انعطى البير الهدية، ١ = انعطى وننتظر يتركب عشان نعبيه، ٢ = خلاص
    public static int WellGift;
    // شرح المويه (بعد «اضغط على الإبل» وقبل «أطعم الإبل»): ركّب البير، واطلب وايت ٢٥ طن (أول مرة مجانًا)
    public static bool IntroDone;
    public static bool StallGift;                 // كشك البيع هدية بعد الشرح
    // بعد المويه: مزرعة علف هدية (نفس البير)، يركّبها ويزرعها ويحصدها، وبعدها «أطعم الإبل»
    public static bool FarmIntro;
    const string FeedFarm = "feed_farm";
    public const float RefillEvery = GameRules.WellRefillSeconds;   // كل بير يتعبى لحاله: حبة كل دقيقة (كل بير مستقل)
    static float refill;   // كم ثانية مرت من التعبئة الجاية (ينحفظ)
    public static float RefillProgress { get { return refill; } }

    // من الحفظ: نكمل عدّاد التعبئة، ونعبي الآبار على الوقت اللي غابه اللاعب (بحد السعة)
    public static void RestoreRefill(float progress, float away)
    {
        refill = Mathf.Clamp(progress, 0f, RefillEvery);
        FarmStore st = FarmStore.Instance;
        if (away <= 0f || st == null || !NeedWater || st.WaterCap <= 0 || st.Water >= st.WaterCap) return;
        float total = refill + away;
        int ticks = Mathf.FloorToInt(total / RefillEvery);
        refill = total - ticks * RefillEvery;
        int wells = Mathf.Max(1, st.WaterCap / GameRules.WellCapacity);
        long add = (long)ticks * wells;
        int room = st.WaterCap - st.Water;
        int got = st.AddWater((int)System.Math.Min(add, room));
        if (st.Water >= st.WaterCap) refill = 0f;
        if (got > 0) Debug.Log("الآبار عبّت وانت طالع: +" + got + " ماء");
    }
    public const int IntroTruck = 1;   // رقم وايت الـ٢٥ طن
    float introDelay = -1f;
    public static bool TruckOnWay { get; private set; }

    float timer = 3f;   // ننتظر الحفظ يتحمل أول

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { refill = 0f; WellGift = 0; TruckOnWay = false; IntroDone = false; StallGift = false; FarmIntro = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        Instance = new GameObject("WaterSystem").AddComponent<WaterSystem>();
        FarmPlot.OnHarvest -= OnFarmHarvest;
        FarmPlot.OnHarvest += OnFarmHarvest;
    }

    static void OnFarmHarvest(FarmPlot f)
    {
        if (f != null && f.IsFeed && IntroActive) FarmIntro = true;   // أول حصاد في الشرح
    }

    void OnDestroy() { if (Instance == this) Instance = null; TruckOnWay = false; }

    public static bool TutorialDone
    {
        get
        {
            OrderManager om = FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
            return om == null || om.IsUpgraded;
        }
    }

    // هل الشرح الحين عند خطوة المويه؟
    public static bool IntroActive
    {
        get
        {
            if (IntroDone || TutorialDone || TaskManager.Instance == null) return false;
            bool[] d = TaskManager.Instance.GetDone();
            return d.Length > 1 && d[0] && !d[1];
        }
    }

    // الإبل تحتاج مويه: بعد الشرح، أو من أول ما يبدأ شرح المويه
    public static bool NeedWater { get { return TutorialDone || IntroDone || IntroActive; } }

    void Update()
    {
        Intro();
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 1f;
        FarmStore st = FarmStore.Instance;
        if (st == null || PlacementManager.Instance == null || PlacementManager.IsPlacing) return;
        st.RecalcWaterCap();

        // البير يتعبى لحاله شوي شوي (مجانًا)
        if (NeedWater && st.WaterCap > 0 && st.Water < st.WaterCap)
        {
            // كل بير له تجدد مستقل: بيرين = حبتين كل دقيقة
            refill += 1f;
            if (refill >= RefillEvery)
            {
                refill = 0f;
                int wells = Mathf.Max(1, st.WaterCap / GameRules.WellCapacity);
                st.AddWater(Mathf.Min(wells, st.WaterCap - st.Water));
            }
        }
        else refill = 0f;

        if (!TutorialDone) return;
        // (كشك البيع الهدية صار في آخر الشرح: TutorialEnd)
        // البير الهدية
        if (WellGift == 0 && InventoryManager.Instance != null)
        {
            WellGift = 1;
            if (st.WaterCap == 0) InventoryManager.Instance.Add("well");
            else WellGift = 2;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.Show("جاك بير هدية في الشنطة! ركّبه عشان تشرب إبلك");
        }
        if (WellGift == 1 && st.WaterCap > 0)
        {
            WellGift = 2;
            if (IntroDone && !TutorialDone) return;   // في الشرح يطلب الوايت بنفسه
            st.AddWater(st.WaterCap);
            if (NotificationManager.Instance != null) NotificationManager.Instance.Show("تعبّى البير مويه");
        }
    }

    // ===== شرح المويه =====
    void Intro()
    {
        TutorialSpotlight spot = TutorialSpotlight.Instance;
        TaskBanner tb = TaskBanner.Instance;
        if (!IntroActive)
        {
            if (introDelay >= 0f)
            {
                // خلص: نرجّع الشرح الأصلي
                introDelay = -1f;
                if (spot != null) { spot.custom = null; spot.customHide = false; }
                if (tb != null && tb.taskText != null && TaskManager.Instance != null)
                {
                    bool[] d = TaskManager.Instance.GetDone();
                    for (int i = 0; i < d.Length && i < tb.taskNames.Length; i++) if (!d[i]) { tb.taskText.text = tb.taskNames[i]; break; }
                }
            }
            return;
        }
        // ننتظر احتفال «كفو» حق الخطوة الأولى يخلص
        if (introDelay < 0f) introDelay = 2.8f;
        if (introDelay > 0f) { introDelay -= Time.unscaledDeltaTime; if (introDelay <= 0f) introDelay = 0.0001f; else return; }

        FarmStore st = FarmStore.Instance;
        InventoryManager inv = InventoryManager.Instance;
        if (st == null || inv == null) return;

        // البير في الشنطة
        if (WellGift == 0) WellGift = 1;
        // لو البير مو متركب ومو في الشنطة (حفظ قديم أو حساب انحذف) نرجّعه، عشان اللاعب ما يعلق
        if (st.WaterCap == 0 && inv.GetCount("well") == 0 && !PlacementManager.IsPlacing && !HasPlacedWell()) inv.Add("well");

        bool windowOpen = false;
        foreach (NaaqaWindow w in FindObjectsByType<NaaqaWindow>(FindObjectsInactive.Exclude)) if (w.IsOpen) { windowOpen = true; break; }

        string text;
        Transform target = null;
        if (st.WaterCap <= 0)
        {
            text = PlacementManager.IsPlacing ? "حط البير قريب من الحوش واضغط زر الصح" : "إبلك عطشانة! ركّب البير من الشنطة";
            GameObject bag = GameObject.Find("BagButton");
            if (bag != null) target = bag.transform;
        }
        else if (st.Water <= 0)
        {
            WellGift = 2;   // البير تركب (ما نعبيه هدية، يطلب الوايت)
            text = TruckOnWay ? "الوايت جاي بالطريق..." : "اطلب وايت مويه ٢٥ طن (أول مرة مجانًا)";
            GameObject bar = GameObject.Find("bar_water");
            Transform plus = bar != null ? bar.transform.Find("PlusButton") : null;
            if (!TruckOnWay) target = plus != null ? plus : (bar != null ? bar.transform : null);
        }
        else if (!FarmIntro)
        {
            // ===== مزرعة العلف (هدية مثل البير) =====
            GameObject farmGo = PlacedFarm();
            FarmPlot fp = farmGo != null ? farmGo.GetComponent<FarmPlot>() : null;
            bool placing = PlacementManager.IsPlacing;
            if (farmGo == null)
            {
                if (inv.GetCount(FeedFarm) == 0 && !placing)
                {
                    inv.Add(FeedFarm);
                    if (NotificationManager.Instance != null) NotificationManager.Instance.Show("جاتك مزرعة علف هدية في الشنطة!");
                }
                text = placing ? "حط المزرعة جنب الحوش واضغط زر الصح" : "جاتك مزرعة علف هدية! ركّبها من الشنطة";
                GameObject bag = GameObject.Find("BagButton");
                if (bag != null) target = bag.transform;
            }
            else if (fp == null || fp.state == FarmPlot.State.Empty)
            {
                text = "اضغط على المزرعة وازرع علف";
                target = farmGo.transform;
            }
            else if (fp.state == FarmPlot.State.Growing)
            {
                if (fp.left > 6f) fp.left = 6f;   // في الشرح ينبت بسرعة
                text = "العلف ينبت... استنى شوي";
            }
            else
            {
                text = "العلف جهز! اضغط على المزرعة واحصده";
                target = farmGo.transform;
            }
        }
        else
        {
            IntroDone = true;   // صار عنده مويه وعلف: يكمل الشرح «أطعم الإبل»
            if (NotificationManager.Instance != null) NotificationManager.Instance.Show("كفو! صار عندك مويه وعلف، الحين أطعم إبلك");
            return;
        }

        if (tb != null && tb.taskText != null && tb.taskText.text != text) tb.taskText.text = text;
        if (spot != null)
        {
            bool hide = windowOpen || PlacementManager.IsPlacing || target == null;
            spot.custom = hide ? null : target;
            spot.customHide = hide;
        }
    }

    static GameObject PlacedFarm()
    {
        if (PlacementManager.Instance == null) return null;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == FeedFarm && p.go != null) return p.go;
        return null;
    }

    static bool HasPlacedWell()
    {
        if (PlacementManager.Instance == null) return false;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == "well" && p.go != null) return true;
        return false;
    }

    // تشرب n مويه. loud = يطلع تنبيه لو ما فيه
    public static bool TryDrink(int n, bool loud)
    {
        if (!NeedWater) return true;   // قبل شرح المويه ما نحتاج مويه
        FarmStore st = FarmStore.Instance;
        if (st == null) return true;
        if (st.WaterCap <= 0)
        {
            if (loud) Warn("ما عندك بير! ركّب بير من الشنطة أو اشتره من السوق");
            return false;
        }
        if (!st.UseWater(n))
        {
            if (loud)
            {
                Warn("خلصت المويه! اطلب وايت");
                WaterTruckScreen.Show();
            }
            return false;
        }
        return true;
    }

    static void Warn(string m)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(m);
    }

    // ===== الوايت =====
    public static string Order(int i)
    {
        FarmStore st = FarmStore.Instance;
        GameManager gm = GameManager.Instance;
        if (st == null || gm == null || Instance == null) return "صار خطأ";
        if (TruckOnWay) return "فيه وايت جاي بالطريق، انتظره";
        if (st.WaterCap <= 0) return "ركّب بير أول";
        int tons = TruckTons[i], price = TruckPrices[i];
        if (st.WaterCap - st.Water < tons) return "ما فيه مكان في الآبار لـ" + NaaqaUI.Ar(tons) + " طن";
        bool free = IntroActive && i == IntroTruck;   // أول وايت في الشرح مجاني
        if (IntroActive && !free) return "في الشرح اطلب وايت ٢٥ طن";
        if (!free && !gm.SpendMoney(price)) return "تحتاج " + NaaqaUI.Ar(price) + " قرش";
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        Instance.StartCoroutine(Instance.Drive(tons));
        return null;
    }

    IEnumerator Drive(int tons)
    {
        TruckOnWay = true;
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("الوايت جاي من الطريق...");

        Vector3 p, dir, nrm; float half;
        Texture2D tex = Resources.Load<Texture2D>("NaaqaUI/water_truck");
        if (HighwayRoad.Line(out p, out dir, out nrm, out half) && tex != null && PenVisual.Instance != null)
        {
            // الوايت يمشي على الطريق العام (بالمسار اليمين)، ويوقف شوي بالنص
            GameObject g = new GameObject("WaterTruck");
            g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.25f), tex.width / 4.2f);
            SpriteRenderer pen = PenVisual.Instance.GetComponent<SpriteRenderer>();
            if (pen != null) { sr.sortingLayerID = pen.sortingLayerID; sr.sortingOrder = pen.sortingOrder + 6; }

            Camera cam = Camera.main;
            float mid = 0f;
            if (cam != null)
            {
                // أقرب نقطة على الطريق لنص الشاشة
                Vector3 cc = cam.transform.position; cc.y = p.y;
                mid = Vector3.Dot(cc - p, dir);
            }
            Vector3 lane = -nrm * half * 0.45f + Vector3.up * 0.03f;
            float from = mid - 30f, to = mid + 30f, t = 0f, dur = 9f;
            bool delivered = false;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                // يدخل بسرعة، يهدّي بالنص، ويطلع
                float s = k < 0.4f ? Mathf.Lerp(from, mid, Mathf.SmoothStep(0, 1, k / 0.4f))
                        : k < 0.6f ? mid
                        : Mathf.Lerp(mid, to, Mathf.SmoothStep(0, 1, (k - 0.6f) / 0.4f));
                g.transform.position = p + dir * s + lane + Vector3.up * (Mathf.Abs(Mathf.Sin(t * 16f)) * 0.03f);
                if (!delivered && k >= 0.5f) { delivered = true; Fill(tons); }
                yield return null;
            }
            Destroy(g);
            if (!delivered) Fill(tons);
        }
        else
        {
            WaterTruckFx.Play();
            yield return new WaitForSeconds(2.5f);
            Fill(tons);
        }
        TruckOnWay = false;
    }

    static void Fill(int tons)
    {
        FarmStore st = FarmStore.Instance;
        if (st == null) return;
        int added = st.AddWater(tons);
        CamelSounds.Play("pour");
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show("وصل الوايت! +" + NaaqaUI.Ar(added) + " مويه");
    }
}
