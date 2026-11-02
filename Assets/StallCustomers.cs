using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== بسطة الحليب وزباينها =====
// لو ركّبت «بسطة الحليب» في العزبة، يطلع طريق رملي لها، وكل شوي يجي زبون يمشي على الطريق
// ويوقف عند البسطة ويطلب حليب. تضغط على فقاعته وتبيع له، ويمشي.
// ولو ما فيه بسطة، ما يجي أحد.
public class StallCustomers : MonoBehaviour
{
    public const string StallId = "milk_stall";
    public const int MilkPrice = GameRules.MilkPrice;   // سعر الحليب للحبة (٦)
    public const float RoadLength = 16f;      // طول الطريق (من البسطة لطرف الخريطة)
    public const float RoadWidth = 1.7f;
    public const float PersonHeight = 1.7f;
    public const float StandGap = 0.05f;      // الزبون يوقف لاصق بالكشك (كان ٠٫٤٥ ثم بعيد ١٫٧)
    public const float Patience = 25f;        // كم ينتظر الزبون

    static readonly string[] People = { "npc_man", "npc_bisht", "npc_woman", "npc_elder", "npc_boy", "npc_phone", "npc_oldbisht", "npc_niqab", "npc_hijab", "npc_kid", "npc_girl", "npc_traveler", "npc_delivery" };
    static readonly string[] AskLines = { "السلام عليكم، أبي {0} حليب", "يا هلا، عندك {0} حليب؟", "الله يحييك، أبي {0} حليب طازج", "أبي {0} حليب للضيوف" };
    static readonly string[] ThanksLines = { "الله يبارك لك!", "بيعة مباركة!", "الله يعطيك العافية!", "حليبكم ما شاء الله!" };

    GameObject stall;
    SpriteRenderer road;
    Camera cam;
    float checkTimer, nextCustomer = 4f;

    // الزبون الحالي
    Transform person;
    SpriteRenderer personSr;
    int want, pay, wantDates;
    string who, askLine;
    public const int OrderCoinsPerMilk = GameRules.MilkPrice;    // كل حليب بـ ٦ قروش، والتمرة بـ ٤
    bool waiting, leaving;
    float waitLeft;
    System.Collections.Generic.List<Vector3> way;   // لو فيه طريق ترابي واصل للبسطة يمشي عليه
    int wi;
    bool pathLinked;
    bool tutorialCustomer;   // أول زبون في الشرح: يبي حليبة وحدة ويصبر
    System.Action carDone;   // زبون نازل من سيارة: لما يرجع لها تمشي

    // سيارة وقفت جنب الطريق: ينزل منها زبون يمشي للكشك، ولما يخلص يرجع لها
    public static bool ArriveFromCar(Vector3 carSide, System.Action done)
    {
        StallCustomers c = inst;
        if (c == null || c.person != null || c.stall == null || !c.stall.activeInHierarchy) return false;
        if (!TutorialEnd.CustomersAllowed || TutorialEnd.WantFirstCustomer || c.onMenu) return false;
        c.Spawn();
        if (c.person == null) return false;
        Vector3 stand = c.Front() + new Vector3(0f, 0.05f, -StandGap);
        carSide.y = stand.y;
        c.way = new System.Collections.Generic.List<Vector3> { carSide, stand };
        c.wi = 0;
        c.person.position = carSide;
        c.carDone = done;
        return true;
    }

    // زبون السيارة للحين موجود؟
    public static bool CarGuestActive { get { return inst != null && inst.person != null && inst.carDone != null; } }

    void FinishCarGuest()
    {
        System.Action d = carDone;
        carDone = null;
        if (d != null) d();
    }
    static StallCustomers inst;

    // ===== الحفظ: الزبون اللي عند الكشك وطلبه ووقت انتظاره =====
    [System.Serializable]
    public class Saved
    {
        public bool has;
        public string who, ask;
        public int milk, dates, pay;
        public float wait;      // كم باقي من صبره (ثواني)
    }

    static Saved pending;   // زبون راجع من الحفظ، يطلع أول ما يكون الكشك جاهز

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { pending = null; inst = null; }

    public static Saved Capture()
    {
        Saved s = new Saved();
        StallCustomers c = inst;
        // زبون الشرح الأول ما ينحفظ (الشرح يجيبه من جديد)، واللي ماشي يطلع ما ينحفظ
        if (c != null && c.person != null && !c.leaving && !c.delivered && !c.tutorialCustomer)
        {
            s.has = true; s.who = c.who; s.ask = c.askLine;
            s.milk = c.want; s.dates = c.wantDates; s.pay = c.pay;
            s.wait = c.waiting ? Mathf.Max(0f, c.waitLeft) : Patience;
        }
        else if (pending != null && pending.has)
            s = pending;   // لسه ما طلع (الكشك ما تجهز)، نحفظه زي ما هو
        return s;
    }

    // نرجّعه واقف عند الكشك بنفس طلبه، وننقص من صبره الوقت اللي غابه اللاعب
    public static void Restore(Saved s, float away)
    {
        pending = null;
        if (inst != null && inst.person != null && inst.carDone == null)
        {
            Destroy(inst.person.gameObject); inst.person = null; inst.way = null;
            if (inst.bubble != null) inst.bubble.gameObject.SetActive(false);
        }
        if (s == null || !s.has || string.IsNullOrEmpty(s.who)) return;
        float left = s.wait - away;
        if (left <= 1f) return;   // خلص صبره وراح
        pending = new Saved { has = true, who = s.who, ask = s.ask, milk = s.milk, dates = s.dates, pay = s.pay, wait = left };
    }

    // يطلع الزبون المحفوظ واقف قدام الكشك ينتظر
    bool SpawnSaved()
    {
        Saved s = pending;
        pending = null;
        Texture2D tex = NaaqaUI.LoadTexture(s.who);
        if (tex == null) return false;
        GameObject g = new GameObject("Customer_" + s.who);
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        personSr = g.AddComponent<SpriteRenderer>();
        float h = (s.who == "npc_boy" || s.who == "npc_kid" || s.who == "npc_girl") ? PersonHeight * 0.72f : PersonHeight;
        personSr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.04f), tex.height / h);
        SpriteRenderer ssr = stall.GetComponentInChildren<SpriteRenderer>();
        personSr.sortingLayerID = ssr != null ? ssr.sortingLayerID : 0;
        personSr.sortingOrder = (ssr != null ? ssr.sortingOrder : 0) + 2;
        person = g.transform;
        person.position = Front() + new Vector3(0f, 0.05f, -StandGap);
        way = null; wi = 0;
        who = s.who; askLine = s.ask;
        want = s.milk; wantDates = s.dates; pay = s.pay;
        tutorialCustomer = false;
        leaving = false; delivered = false;
        waiting = true;
        waitLeft = s.wait;
        ShowAsk();
        return true;
    }

    // فقاعة الزبون اللي ينتظر (عشان الشرح يأشر عليها)
    public static Transform WaitingBubble
    {
        get
        {
            if (inst == null || inst.bubble == null || !inst.waiting || inst.leaving || !inst.bubble.gameObject.activeInHierarchy) return null;
            return inst.bubble;
        }
    }

    // الفقاعة
    Canvas canvas;
    RectTransform bubble;
    RTLTextMeshPro bubbleText;
    Button bubbleBtn;
    Image bubbleBg;
    GameObject bubbleIcon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        if (FindAnyObjectByType<StallCustomers>() != null) return;
        new GameObject("StallCustomers").AddComponent<StallCustomers>();
    }

    void Awake() { inst = this; }

    void Start()
    {
        cam = Camera.main;
        BuildRoad();
        BuildBubble();
    }

    // ===== الطريق الرملي (يترسم بالكود) =====
    void BuildRoad()
    {
        int w = 64, h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color sand = new Color32(238, 214, 168, 255);
        Color dark = new Color32(214, 182, 132, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs(x - (w - 1) / 2f) / ((w - 1) / 2f);          // ٠ بالنص، ١ بالطرف
                float edge = 1f - Mathf.SmoothStep(0.55f, 1f, u + Mathf.PerlinNoise(x * 0.2f, y * 0.05f) * 0.15f);
                float fadeEnd = Mathf.SmoothStep(0f, 0.12f, y / (float)h);          // يختفي بهدوء عند طرف الخريطة
                float fadeTop = 1f - Mathf.SmoothStep(0.94f, 1f, y / (float)h);
                Color c = Color.Lerp(sand, dark, Mathf.PerlinNoise(x * 0.15f + 7f, y * 0.08f) * 0.6f);
                c.a = edge * fadeEnd * fadeTop * 0.95f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        float ppu = w / RoadWidth;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), ppu, 0, SpriteMeshType.FullRect);

        GameObject g = new GameObject("StallRoad");
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        road = g.AddComponent<SpriteRenderer>();
        road.sprite = s;
        road.drawMode = SpriteDrawMode.Sliced;
        road.size = new Vector2(RoadWidth, RoadLength);
        road.sortingOrder = -5;
        g.SetActive(false);
    }

    // ===== فقاعة الزبون (فوق راسه) =====
    void BuildBubble()
    {
        canvas = NaaqaUI.MakeCanvas("StallCanvas", 40);
        canvas.transform.SetParent(transform, false);

        bubbleBg = NaaqaUI.SpeechBubble(canvas.transform, "Bubble", 17, out bubbleText);
        bubble = bubbleBg.rectTransform;
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.pivot = new Vector2(0.5f, 0f);
        bubble.sizeDelta = new Vector2(330, 110);
        bubbleBg.raycastTarget = true;

        Image ic = NaaqaUI.IconImg(bubble, "milk", 36);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(36, 36), new Vector2(-6, 0));
        bubbleIcon = ic.gameObject;

        bubbleBtn = bubble.gameObject.AddComponent<Button>();
        bubbleBtn.targetGraphic = bubbleBg;
        bubbleBtn.onClick.AddListener(Sell);
        bubble.gameObject.AddComponent<NaaqaButtonFx>();
        bubble.gameObject.SetActive(false);
    }

    // ===== التحديث =====
    float menuTimer;
    bool onMenu;

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // شاشة البداية: ما يطلع زباين ولا كلام فوقها
        menuTimer -= Time.unscaledDeltaTime;
        if (menuTimer <= 0f) { menuTimer = 0.3f; UIManager ui = UIManager.Instance; onMenu = ui != null && ((ui.startScreen != null && ui.startScreen.activeInHierarchy) || (ui.hud != null && !ui.hud.activeInHierarchy)); }
        if (canvas != null && canvas.enabled == onMenu) canvas.enabled = !onMenu;
        if (onMenu) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = 0.5f;
            stall = FindStall();
            pathLinked = stall != null && DirtPaths.Route(Vector3.zero, Front() + new Vector3(0f, 0f, -StandGap), true) != null;
        }

        bool moving = stall != null && PlacementManager.IsMoving && PlacementManager.IsPlacing;
        bool active = stall != null && stall.activeInHierarchy;

        // الطريق يلحق البسطة
        if (road != null)
        {
            bool showRoad = active && !pathLinked && way == null;
            if (road.gameObject.activeSelf != showRoad) road.gameObject.SetActive(showRoad);
            if (active)
            {
                road.transform.position = Front() + new Vector3(0f, 0.01f, 0f);
                SpriteRenderer ssr = stall.GetComponentInChildren<SpriteRenderer>();
                if (ssr != null) { road.sortingLayerID = ssr.sortingLayerID; road.sortingOrder = ssr.sortingOrder - 1; }
            }
        }

        if (!active)
        {
            if (person != null) { Destroy(person.gameObject); person = null; FinishCarGuest(); }
            bubble.gameObject.SetActive(false);
            return;
        }

        if (person == null)
        {
            if (moving) return;
            if (!TutorialEnd.CustomersAllowed) return;          // وقت الشرح ما يجي زباين
            if (pending != null && pending.has && !TutorialEnd.WantFirstCustomer && SpawnSaved()) { UpdateBubble(); return; }
            if (TutorialEnd.WantFirstCustomer && nextCustomer > 2f) nextCustomer = 2f;
            nextCustomer -= Time.deltaTime;
            if (nextCustomer <= 0f) Spawn();
            return;
        }

        StepPerson();
        UpdateBubble();
    }

    GameObject FindStall()
    {
        if (PlacementManager.Instance == null) return null;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == StallId && p.go != null) return p.go;
        return null;
    }

    // قدام البسطة (تحتها على الشاشة)
    Vector3 Front()
    {
        Bounds b = GetBounds(stall);
        return new Vector3(b.center.x, stall.transform.position.y, b.min.z + 0.25f);
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ===== الزبون =====
    void Spawn()
    {
        string who = People[Random.Range(0, People.Length)];
        Texture2D tex = NaaqaUI.LoadTexture(who);
        if (tex == null) { nextCustomer = 5f; return; }

        GameObject g = new GameObject("Customer_" + who);
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        personSr = g.AddComponent<SpriteRenderer>();
        float h = (who == "npc_boy" || who == "npc_kid" || who == "npc_girl") ? PersonHeight * 0.72f : PersonHeight;
        personSr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.04f), tex.height / h);
        SpriteRenderer ssr2 = stall.GetComponentInChildren<SpriteRenderer>();
        personSr.sortingLayerID = ssr2 != null ? ssr2.sortingLayerID : 0;
        personSr.sortingOrder = (ssr2 != null ? ssr2.sortingOrder : 0) + 2;
        personSr.flipX = Random.value < 0.5f;
        person = g.transform;
        person.position = Front() + new Vector3(Random.Range(-0.3f, 0.3f), 0.05f, -RoadLength + 1f);

        // طريق ترابي واصل للبسطة؟ يجي عليه من أبعد طرف
        way = null; wi = 0;
        Vector3 stand = Front() + new Vector3(0f, 0.05f, -StandGap);
        System.Collections.Generic.List<Vector3> r = DirtPaths.Route(person.position, stand, true);
        if (r != null && r.Count >= 2)
        {
            way = new System.Collections.Generic.List<Vector3>();
            foreach (Vector3 v in r) way.Add(new Vector3(v.x, stand.y, v.z));
            way.Add(stand);
            person.position = way[0];
        }

        // الطلب: حليب، أو تمر، أو الاثنين (التمر بس لو عندك تمر أو مزرعة تمر). كميات صغيرة تكبر مع الحوش
        int pen = FarmStore.Instance != null ? FarmStore.Instance.PenLevel : 1;
        bool datesKnown = FarmStore.Instance != null && (FarmStore.Instance.Dates > 0 || HasDateFarm());
        float roll = Random.value;
        want = Random.Range(1, 3 + pen / 2);
        wantDates = 0;
        if (datesKnown)
        {
            if (roll < 0.25f) { want = 0; wantDates = Random.Range(1, 4); }        // تمر بس
            else if (roll < 0.55f) wantDates = Random.Range(1, 3);               // حليب وتمر
        }
        // أول زبون في الشرح: حليبة وحدة بس
        tutorialCustomer = TutorialEnd.WantFirstCustomer;
        if (tutorialCustomer) { want = 1; wantDates = 0; }
        pay = want * GameRules.MilkPrice + wantDates * GameRules.DatePrice + (tutorialCustomer ? 0 : Random.Range(0, GameRules.TipMax + 1));
        this.who = who;
        askLine = (want > 0 ? string.Format(AskLines[Random.Range(0, AskLines.Length)], NaaqaUI.Ar(want)) : "السلام عليكم، أبي " + NaaqaUI.Ar(wantDates) + " تمر")
                + (want > 0 && wantDates > 0 ? " و" + NaaqaUI.Ar(wantDates) + " تمر" : "") + " الله يعافيك";
        waiting = false;
        leaving = false;
        delivered = false;
        bubble.gameObject.SetActive(false);
    }

    // يمشي على الطريق الترابي نقطة نقطة (وبعد الشراء يرجع بنفس الطريق)
    bool StepOnWay()
    {
        if (way == null) return false;
        if (waiting)
        {
            if (!OrdersScreen.IsOpen && !tutorialCustomer) waitLeft -= Time.deltaTime;
            if (waitLeft <= 0f) Leave("شكلك مشغول، أجيك بعدين", false);
            return true;
        }
        Vector3 target = way[Mathf.Clamp(wi, 0, way.Count - 1)];
        Vector3 p = person.position;
        Vector3 np = Vector3.MoveTowards(p, target, 2.2f * Time.deltaTime);
        if (Mathf.Abs(np.x - p.x) > 0.0005f) personSr.flipX = np.x < p.x;
        person.position = np;
        if ((np - target).sqrMagnitude < 0.01f)
        {
            if (!leaving)
            {
                if (wi < way.Count - 1) wi++;
                else { waiting = true; waitLeft = Patience; ShowAsk(); }
            }
            else
            {
                if (wi > 0) wi--;
                else { Destroy(person.gameObject); person = null; way = null; NextIn(); FinishCarGuest(); }
            }
        }
        return true;
    }

    void StepPerson()
    {
        if (StepOnWay()) return;
        Vector3 target = leaving ? Front() + new Vector3(0f, 0.05f, -RoadLength + 0.5f) : Front() + new Vector3(0f, 0.05f, -StandGap);
        target.x = person.position.x;
        float speed = 2.2f;
        Vector3 p = person.position;
        if (!waiting)
        {
            Vector3 np = Vector3.MoveTowards(p, target, speed * Time.deltaTime);
            // يتمايل وهو يمشي
            np.x += Mathf.Sin(Time.time * 9f) * 0.004f;
            person.position = np;
            if ((np - target).sqrMagnitude < 0.01f)
            {
                if (leaving) { Destroy(person.gameObject); person = null; NextIn(); FinishCarGuest(); return; }
                waiting = true;
                waitLeft = Patience;
                ShowAsk();
            }
        }
        else
        {
            if (!OrdersScreen.IsOpen && !tutorialCustomer) waitLeft -= Time.deltaTime;   // ما يمل وأنت تشوف طلبه
            if (waitLeft <= 0f) Leave("شكلك مشغول، أجيك بعدين", false);
        }
    }

    void NextIn()
    {
        int pen = FarmStore.Instance != null ? FarmStore.Instance.PenLevel : 1;
        nextCustomer = Random.Range(10f, 16f) / (1f + 0.12f * (pen - 1));
        if (Musalla.Placed() != null) nextCustomer /= GameRules.MusallaCustomerBoost;   // المصلى يجيب زباين أكثر بـ١٥٪
    }

    // ===== الفقاعة =====
    void ShowAsk()
    {
        bubble.gameObject.SetActive(true);
        bubbleBtn.interactable = true;
        bubbleIcon.SetActive(true);
        bubbleText.color = NaaqaUI.Cream;
        NaaqaUI.FitBubble(bubble, bubbleText, "أبي " + WantText(want, wantDates) + "\nاضغط عليّ", 220f, 42f);
    }

    void UpdateBubble()
    {
        if (!bubble.gameObject.activeSelf || cam == null || person == null) return;
        Vector3 head = person.position + new Vector3(0f, 0f, PersonHeight * 1.05f);
        Vector3 sp = cam.WorldToScreenPoint(head);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out local);
        bubble.anchoredPosition = local;
        // نبضة خفيفة تقول «اضغطني»
        if (waiting && !leaving)
            bubble.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f);
    }

    // الضغط على الزبون: تطلع نافذة طلبه (شكله ووش يبي)
    void Sell()
    {
        if (!waiting || leaving) return;
        if (PlacementManager.IsPlacing || EditMode.Active) return;
        OrdersScreen.ShowCustomer(personSr != null ? NaaqaUI.Load(who) : null, askLine, want, pay, Deliver, () => Leave("الله يرزقك، مرة ثانية إن شاء الله", false), null, wantDates);
    }

    public static string WantText(int milk, int dates)
    {
        if (milk > 0 && dates > 0) return NaaqaUI.Ar(milk) + " حليب و" + NaaqaUI.Ar(dates) + " تمر";
        return milk > 0 ? NaaqaUI.Ar(milk) + " حليب" : NaaqaUI.Ar(dates) + " تمر";
    }

    static bool HasDateFarm()
    {
        if (PlacementManager.Instance == null) return false;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced()) if (p.id == "date_farm") return true;
        return false;
    }

    bool delivered;   // ما يتسلّم نفس الطلب مرتين
    bool Deliver()
    {
        if (delivered || !waiting || leaving) return false;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.milkCount < want) return false;
        if (wantDates > 0 && (FarmStore.Instance == null || FarmStore.Instance.Dates < wantDates)) return false;
        delivered = true;
        gm.milkCount -= want;
        if (wantDates > 0) FarmStore.Instance.AddDates(-wantDates);
        gm.AddMoney(pay);
        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        LevelManager.AddXp(LevelManager.XpPerOrder);
        Quests.Add("orders");
        if (want > 0) Quests.Add("sell", want);
        if (tutorialCustomer) { tutorialCustomer = false; TutorialEnd.OnSale(); }
        CamelSounds.Play("pour");
        Leave(ThanksLines[Random.Range(0, ThanksLines.Length)] + "  +" + NaaqaUI.Ar(pay) + " قرش", true);
        return true;
    }

    void Leave(string line, bool happy)
    {
        waiting = false;
        leaving = true;
        bubbleBtn.interactable = false;
        bubbleIcon.SetActive(false);
        bubbleText.color = happy ? NaaqaUI.OkColor : NaaqaUI.Cream;
        bubble.localScale = Vector3.one;
        NaaqaUI.FitBubble(bubble, bubbleText, line, 260f);
        StartCoroutine(HideBubbleLater(2.2f));
    }

    IEnumerator HideBubbleLater(float t)
    {
        yield return new WaitForSeconds(t);
        if (leaving) bubble.gameObject.SetActive(false);
    }
}
