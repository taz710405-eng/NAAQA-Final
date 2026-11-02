using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ===== مهرجان المزاين =====
// تختار ناقتك وتدخل المزاين مع ٣ نوق ثانية.
// كل ناقة تمشي على السجادة الحمرا وتطلع المنصة، وتطلع مواصفاتها (الرقبة، السنام، اللون، الوقفة).
// أنت تقيّمها من ١٠، وبعدها المحكمين الثلاثة. تقييمك ينحسب بس لو كان عادل (قريب من المحكمين)،
// عشان ما أحد يعطي ناقته ١٠ وغيرها ١ ويفوز. الأعلى مجموع (من ٤٠) تفوز.
public class MazayenScreen : MonoBehaviour
{
    public static MazayenScreen Instance;

    // مسابقة وحدة
    public const int Fee = GameRules.MazayenFee;
    public const int Prize = GameRules.MazayenWin;

    static readonly string[] JudgeLines =
    {
        "ما شاء الله، زينة الخلقة", "شف رقبتها ما أطولها", "لونها يشرح الصدر", "سنامها مرتب",
        "وقفتها وقفة فخر", "عليها هيبة", "الله يحفظها لراعيها", "مشافرها مدلّية",
    };
    static readonly string[] RivalBreeds = { "wadh", "shaqh", "majaheem", "shaal", "sufr", "humr", "awarik", "sahiliyat" };

    // ===== أماكن من صورة المهرجان (١٩٢٠×١٠٨٠) =====
    static readonly Vector2 PlatformPx = new Vector2(1567, 392);
    static readonly Vector2 CarpetPx = new Vector2(1555, 760);
    static readonly Vector2[] JudgePx = { new Vector2(690, 300), new Vector2(820, 296), new Vector2(950, 292) };

    Canvas canvas;
    CanvasGroup group;
    RectTransform canvasRect, scene, lobby, card, results;
    RawImage camelImg;
    RectTransform camelRt;
    // مكان الناقة بإحداثيات صورة الخلفية (١٩٢٠×١٠٨٠)، وينحسب على الشاشة كل فريم:
    // لو تغيّر مقاس الشاشة (تدوير الجوال، تغيير حجم المتصفح) تبقى على المنصة
    Vector2 camelPx;
    float camelBob;
    RTLTextMeshPro bigText, status, pickText, pickInfo, enterText;
    RTLTextMeshPro cardName, cardJudges, cardTraits, cardAsk;
    readonly System.Collections.Generic.List<Button> rateButtons = new System.Collections.Generic.List<Button>();
    int rated = -1;
    static readonly string[] TraitNames = { "الرقبة", "السنام", "اللون", "الوقفة" };
    Button enterBtn;
    readonly List<RTLTextMeshPro> resultRows = new List<RTLTextMeshPro>();
    RTLTextMeshPro resultTitle, resultSub;
    readonly List<NpcWalker> judges = new List<NpcWalker>();
    readonly List<NpcWalker> crowd = new List<NpcWalker>();
    int pick = 0;
    bool judging = false;

    class Entry
    {
        public string breed;
        public bool mine;
        public int[] judge = new int[3];
        public int[] traits = new int[4];   // الرقبة، السنام، اللون، الوقفة
        public int player = -1;             // تقييمك (من ١٠)
        public bool fair;                   // تقييمك قريب من المحكمين؟
        public int Total { get { return judge[0] + judge[1] + judge[2]; } }
        public float Avg { get { return Total / 3f; } }
        // المجموع من ٤٠: المحكمين + تقييمك (لو عادل)، ولو مو عادل ناخذ متوسط المحكمين بداله
        public int Final { get { return Total + (fair ? player : Mathf.RoundToInt(Avg)); } }
    }

    public static void Show()
    {
        if (Instance == null)
        {
            Instance = new GameObject("MazayenScreen").AddComponent<MazayenScreen>();
            Instance.Build();
        }
        Instance.gameObject.SetActive(true);
        Instance.Open();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ===== البناء =====
    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("MazayenCanvas", 720);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        Image blocker = NaaqaUI.Img(root, "Blocker", null, new Color32(60, 35, 15, 255));
        NaaqaUI.Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;

        scene = NaaqaUI.Rect("Scene", root);
        NaaqaUI.Stretch(scene);
        AspectRatioFitter f = scene.gameObject.AddComponent<AspectRatioFitter>();
        f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        f.aspectRatio = 16f / 9f;
        Image bg = NaaqaUI.Img(scene, "Bg", NaaqaUI.Load("mazayen_bg"), Color.white);
        NaaqaUI.Stretch(bg.rectTransform);

        camelRt = NaaqaUI.Rect("Camel", scene);
        camelRt.anchorMin = camelRt.anchorMax = new Vector2(0.5f, 0.5f);
        camelRt.pivot = new Vector2(0.5f, 0.04f);
        camelImg = camelRt.gameObject.AddComponent<RawImage>();
        camelImg.uvRect = new Rect(0.232f, 0.033f, 0.599f, 0.863f);   // نقص الفراغ حول الناقة
        camelImg.raycastTarget = false;
        camelRt.gameObject.SetActive(false);

        BuildTopBar(root);
        BuildLobby(root);
        BuildCard(root);
        BuildResults(root);

        bigText = NaaqaUI.Text(root, "Big", "", 60, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        NaaqaUI.Center(bigText.rectTransform, new Vector2(1300, 110), new Vector2(-200, 380));
        CaptionOutline bo = bigText.gameObject.AddComponent<CaptionOutline>();
        bo.width = 0.2f;
        bo.color = new Color32(60, 22, 0, 255);
    }

    void BuildTopBar(Transform root)
    {
        Image pill = NaaqaUI.Img(root, "TitlePill", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 30f);
        NaaqaUI.Place(pill.rectTransform, new Vector2(1, 1), new Vector2(420, 84), new Vector2(-30, -22));
        Image icon = NaaqaUI.IconImg(pill.transform, "mazayen", 72);
        NaaqaUI.Place(icon.rectTransform, new Vector2(1, 0.5f), new Vector2(72, 72), new Vector2(-6, 0));
        RTLTextMeshPro t = NaaqaUI.Text(pill.transform, "Title", "مهرجان المزاين", 36, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Stretch(t.rectTransform, 16, 92, 0, 3);

        Button back = NaaqaUI.IconButton(root, "back", "رجوع للعزبة", 88, Close);
        NaaqaUI.Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(88, 88), new Vector2(40, -20));
    }

    static Image Card(Transform parent, string name)
    {
        Image bg = NaaqaUI.Img(parent, name, NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 242), new Color32(22, 13, 8, 246), "mazCard"), Color.white, 30f);
        bg.raycastTarget = true;
        Image border = NaaqaUI.Img(bg.transform, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.3f), 30f);
        NaaqaUI.Stretch(border.rectTransform);
        return bg;
    }

    // ===== قبل المسابقة =====
    void BuildLobby(Transform root)
    {
        lobby = Card(root, "Lobby").rectTransform;
        NaaqaUI.Place(lobby, new Vector2(0.5f, 0), new Vector2(1000, 330), new Vector2(-150, 24));

        RTLTextMeshPro how = NaaqaUI.Text(lobby, "How", "ناقتك تطلع المنصة، والمحكمين الثلاثة يقيّمونها، والأعلى درجة تفوز", 23, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Place(how.rectTransform, new Vector2(0.5f, 1), new Vector2(940, 40), new Vector2(0, -16));

        RTLTextMeshPro h = NaaqaUI.Text(lobby, "H", "ناقتك:", 28, NaaqaUI.OrangeLight, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(h.rectTransform, new Vector2(1, 1), new Vector2(120, 56), new Vector2(-30, -62));
        pickText = NaaqaUI.Text(lobby, "Pick", "", 30, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(pickText.rectTransform, new Vector2(1, 1), new Vector2(240, 56), new Vector2(-150, -62));
        RTLTextMeshPro st;
        Button swap = NaaqaUI.MakeButton(lobby, "Swap", "بدّل الناقة", NaaqaUI.ButtonStyle.Secondary, 24, out st);
        NaaqaUI.Place((RectTransform)swap.transform, new Vector2(1, 1), new Vector2(200, 54), new Vector2(-400, -63));
        swap.onClick.AddListener(() => { if (!judging) { pick++; Refresh(); } });
        pickInfo = NaaqaUI.Text(lobby, "Info", "", 22, NaaqaUI.Muted, TextAlignmentOptions.MidlineLeft);
        NaaqaUI.Place(pickInfo.rectTransform, new Vector2(0, 1), new Vector2(340, 56), new Vector2(30, -62));

        enterBtn = NaaqaUI.MakeButton(lobby, "Enter", "", NaaqaUI.ButtonStyle.Primary, 28, out enterText);
        NaaqaUI.Place((RectTransform)enterBtn.transform, new Vector2(0.5f, 0), new Vector2(680, 130), new Vector2(0, 60));
        enterBtn.onClick.AddListener(Enter);

        status = NaaqaUI.Text(lobby, "Status", "", 22, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Place(status.rectTransform, new Vector2(0.5f, 0), new Vector2(940, 44), new Vector2(0, 14));
    }

    // ===== وقت التحكيم =====
    void BuildCard(Transform root)
    {
        card = Card(root, "JudgeCard").rectTransform;
        NaaqaUI.Place(card, new Vector2(0, 0), new Vector2(900, 380), new Vector2(40, 30));
        cardName = NaaqaUI.Text(card, "Name", "", 34, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(cardName.rectTransform, new Vector2(1, 1), new Vector2(840, 52), new Vector2(-30, -18));
        cardTraits = NaaqaUI.Text(card, "Traits", "", 25, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        NaaqaUI.Place(cardTraits.rectTransform, new Vector2(1, 1), new Vector2(840, 42), new Vector2(-30, -72));
        cardAsk = NaaqaUI.Text(card, "Ask", "", 26, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(cardAsk.rectTransform, new Vector2(1, 1), new Vector2(840, 44), new Vector2(-30, -120));

        // أزرار تقييمك من ١ لين ١٠
        RectTransform row = NaaqaUI.Rect("Rate", card);
        row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1);
        row.pivot = new Vector2(0.5f, 1f);
        row.offsetMin = new Vector2(30, -240); row.offsetMax = new Vector2(-30, -172);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.reverseArrangement = true; h.spacing = 8;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = true;
        for (int i = 1; i <= 10; i++)
        {
            int v = i;
            RTLTextMeshPro t;
            Button b = NaaqaUI.MakeButton(row, "R" + i, NaaqaUI.Ar(i), NaaqaUI.ButtonStyle.Secondary, 28, out t);
            b.onClick.AddListener(() => rated = v);
            rateButtons.Add(b);
        }

        cardJudges = NaaqaUI.Text(card, "Judges", "", 26, NaaqaUI.OrangeLight, TextAlignmentOptions.MidlineRight, true);
        cardJudges.textWrappingMode = TextWrappingModes.Normal;
        NaaqaUI.Place(cardJudges.rectTransform, new Vector2(1, 0), new Vector2(840, 110), new Vector2(-30, 16));
        card.gameObject.SetActive(false);
    }

    void BuildResults(Transform root)
    {
        results = Card(root, "Results").rectTransform;
        NaaqaUI.Place(results, new Vector2(0.5f, 0.5f), new Vector2(720, 580), new Vector2(-150, 0));
        Image tr = NaaqaUI.IconImg(results, "trophy", 100);
        NaaqaUI.Place(tr.rectTransform, new Vector2(0.5f, 1), new Vector2(100, 100), new Vector2(0, -18));
        resultTitle = NaaqaUI.Text(results, "T", "", 42, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(resultTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(680, 60), new Vector2(0, -124));
        for (int i = 0; i < 4; i++)
        {
            RTLTextMeshPro r = NaaqaUI.Text(results, "R" + i, "", 27, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight);
            NaaqaUI.Place(r.rectTransform, new Vector2(0.5f, 1), new Vector2(560, 46), new Vector2(0, -196 - i * 50));
            resultRows.Add(r);
        }
        resultSub = NaaqaUI.Text(results, "S", "", 27, NaaqaUI.OkColor, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(resultSub.rectTransform, new Vector2(0.5f, 0), new Vector2(680, 50), new Vector2(0, 116));
        RTLTextMeshPro ok;
        Button okb = NaaqaUI.MakeButton(results, "Ok", "تمام", NaaqaUI.ButtonStyle.Primary, 28, out ok);
        NaaqaUI.Place((RectTransform)okb.transform, new Vector2(0.5f, 0), new Vector2(260, 74), new Vector2(0, 28));
        okb.onClick.AddListener(() => { results.gameObject.SetActive(false); lobby.gameObject.SetActive(true); camelRt.gameObject.SetActive(false); });
        results.gameObject.SetActive(false);
    }

    // ===== أدوات =====
    Vector2 ToScene(Vector2 px)
    {
        Rect s = scene.rect;
        return new Vector2((px.x / 1920f - 0.5f) * s.width, (0.5f - px.y / 1080f) * s.height);
    }

    float K { get { return scene.rect.height / 1080f; } }

    List<string> Owned() { return CamelBreeds.OwnedBreeds(); }

    CamelBreeds.Breed Picked()
    {
        List<string> o = Owned();
        if (o.Count == 0) return CamelBreeds.Get(CamelBreeds.StarterBreed);
        pick = ((pick % o.Count) + o.Count) % o.Count;
        return CamelBreeds.Get(o[pick]);
    }

    static float WinChance(CamelBreeds.Breed b)
    {
        int beauty = b != null ? b.beauty : 1;
        float factor = beauty >= 3 ? 1.4f : beauty == 2 ? 1.0f : 0.6f;
        return Mathf.Clamp(GameRules.MazayenOdds1 * factor, 0.05f, 0.85f);
    }

    // ===== فتح =====
    void Open()
    {
        StopAllCoroutines();
        judging = false;
        lobby.gameObject.SetActive(true);
        card.gameObject.SetActive(false);
        results.gameObject.SetActive(false);
        camelRt.gameObject.SetActive(false);
        bigText.text = "";
        status.text = "";
        // نبدأ بأجمل ناقة عندك
        List<string> o = Owned();
        int best = 0, bestB = -1;
        for (int i = 0; i < o.Count; i++) { CamelBreeds.Breed b = CamelBreeds.Get(o[i]); if (b != null && b.beauty > bestB) { bestB = b.beauty; best = i; } }
        pick = best;
        Refresh();
        StartCoroutine(FadeIn());
        StartCoroutine(SetupPeople());
    }

    IEnumerator SetupPeople()
    {
        yield return null;
        foreach (NpcWalker w in judges) if (w != null) Destroy(w.gameObject);
        foreach (NpcWalker w in crowd) if (w != null) Destroy(w.gameObject);
        judges.Clear(); crowd.Clear();

        string[] jw = { "npc_elder", "npc_bisht", "npc_man" };
        for (int i = 0; i < 3; i++)
        {
            NpcWalker j = NpcWalker.Spawn(scene, jw[i], ToScene(JudgePx[i]), 120f * K, NpcWalker.Place.Race, new Rect(0, 0, 1, 1), true);
            j.chatty = false;
            judges.Add(j);
        }
        // ناس يتفرجون
        Vector2[] spots = { new Vector2(300, 540), new Vector2(520, 580), new Vector2(760, 560), new Vector2(1000, 600), new Vector2(420, 720) };
        string[] who = { "npc_man", "npc_woman", "npc_boy", "npc_bisht", "npc_elder" };
        for (int i = 0; i < spots.Length; i++)
        {
            float h = (who[i] == "npc_boy" ? 105f : 140f) * K;
            NpcWalker w = NpcWalker.Spawn(scene, who[i], ToScene(spots[i]), h, NpcWalker.Place.Race, new Rect(0, 0, 1, 1), true);
            w.chatty = false;
            w.Face(1f);
            crowd.Add(w);
        }
        SortScene();
    }

    void SortScene()
    {
        List<Transform> all = new List<Transform>();
        foreach (Transform c in scene) if (c.name != "Bg") all.Add(c);
        all.Sort((a, b) => ((RectTransform)b).anchoredPosition.y.CompareTo(((RectTransform)a).anchoredPosition.y));
        for (int i = 0; i < all.Count; i++) all[i].SetSiblingIndex(1 + i);
    }

    void Refresh()
    {
        int lvl = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        CamelBreeds.Breed b = Picked();
        pickText.text = b != null ? b.name : "";
        int wins = FarmStore.Instance != null ? FarmStore.Instance.MazayenWins : 0;
        pickInfo.text = b == null ? "" : "جمالها " + NaaqaUI.Ar(b.beauty) + " من ٣  •  فزت " + NaaqaUI.Ar(wins) + " مرة";
        bool open = lvl >= GameRules.MazayenUnlock;
        enterBtn.interactable = open && !judging;
        enterText.text = open
            ? "دخّلها المزاين • الدخول " + NaaqaUI.Ar(Fee) + " • الجائزة " + NaaqaUI.Ar(Prize) + " • فرصتك " + NaaqaUI.Ar(Mathf.RoundToInt(WinChance(b) * 100f)) + "٪"
            : "يفتح في المستوى " + NaaqaUI.Ar(GameRules.MazayenUnlock);
    }

    void SetRating(bool on)
    {
        foreach (Button b in rateButtons) b.interactable = on;
    }

    // ===== المسابقة =====
    void Enter()
    {
        if (judging) return;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (!gm.SpendMoney(Fee)) { status.color = NaaqaUI.ErrorColor; status.text = "الدخول يبي " + NaaqaUI.Ar(Fee) + " قرش"; return; }
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        StartCoroutine(Run());
    }

    // درجات ٣ محكمين مجموعها total (كل وحدة من ١ لين ١٠)
    static void Split(Entry e, int total)
    {
        total = Mathf.Clamp(total, 6, 30);
        int a = Mathf.Clamp(Mathf.RoundToInt(total / 3f + Random.Range(-1, 2)), 1, 10);
        int b = Mathf.Clamp(Mathf.RoundToInt((total - a) / 2f + Random.Range(-1, 2)), 1, 10);
        int c = Mathf.Clamp(total - a - b, 1, 10);
        e.judge[0] = a; e.judge[1] = b; e.judge[2] = c;
        float avg = (a + b + c) / 3f;
        for (int k = 0; k < 4; k++) e.traits[k] = Mathf.Clamp(Mathf.RoundToInt(avg + Random.Range(-1.4f, 1.4f)), 2, 10);
    }

    // المتسابقين والنتيجة (حسب فرصة الفوز)
    List<Entry> MakeEntries(CamelBreeds.Breed mine)
    {
        List<Entry> rivals = new List<Entry>();
        List<string> pool = new List<string>(RivalBreeds);
        for (int i = 0; i < 3; i++)
        {
            int j = Random.Range(0, pool.Count);
            Entry e = new Entry { breed = pool[j] };
            pool.RemoveAt(j);
            CamelBreeds.Breed rb = CamelBreeds.Get(e.breed);
            Split(e, 15 + (rb != null ? rb.beauty : 1) * 3 + Random.Range(0, 5));
            rivals.Add(e);
        }
        rivals.Sort((x, y) => y.Total.CompareTo(x.Total));

        bool win = Random.value < WinChance(mine);
        int rank = win ? 0 : (Random.value < 0.55f ? 1 : Random.value < 0.6f ? 2 : 3);
        int hi = rank == 0 ? 31 : rivals[rank - 1].Total;   // لازم أقل من اللي فوقه
        int lo = rank >= rivals.Count ? 5 : rivals[rank].Total;   // وأعلى من اللي تحته
        int myTotal = rank == 0 ? Mathf.Min(30, lo + Random.Range(1, 4)) : Mathf.Clamp((hi + lo) / 2, lo + 1, hi - 1);
        if (rank > 0 && hi - lo < 2) myTotal = lo;   // ما فيه مكان بينهم: تعادل، وتطلع تحته بالترتيب
        Entry me = new Entry { breed = mine != null ? mine.id : CamelBreeds.StarterBreed, mine = true };
        Split(me, myTotal);
        if (rank == 0 && me.Total <= rivals[0].Total) Split(me, rivals[0].Total + 1);

        List<Entry> all = new List<Entry>(rivals) { me };
        // ترتيب عشوائي للطلعة على المنصة
        for (int i = 0; i < all.Count; i++) { int j = Random.Range(i, all.Count); Entry t = all[i]; all[i] = all[j]; all[j] = t; }
        return all;
    }

    IEnumerator Run()
    {
        judging = true;
        lobby.gameObject.SetActive(false);
        List<Entry> entries = MakeEntries(Picked());

        bigText.text = "المزاين بدأ!";
        CamelSounds.Play("crowd");
        yield return new WaitForSecondsRealtime(1.2f);

        for (int n = 0; n < entries.Count; n++)
        {
            Entry e = entries[n];
            CamelBreeds.Breed b = CamelBreeds.Get(e.breed);
            bigText.text = e.mine ? "ناقتك على المنصة!" : "الناقة " + NaaqaUI.Ar(n + 1) + " من " + NaaqaUI.Ar(entries.Count);

            // تمشي على السجادة وتطلع المنصة
            camelImg.texture = Resources.Load<Texture2D>("NaaqaCamels/camel_" + e.breed);
            camelPx = CarpetPx; camelBob = 0f;
            PlaceCamel();
            camelRt.gameObject.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.6f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                camelPx = Vector2.Lerp(CarpetPx, PlatformPx, k);
                camelBob = Mathf.Abs(Mathf.Sin(t * 18f)) * 6f * (1f - k);
                PlaceCamel();
                camelRt.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
                camelImg.color = new Color(1, 1, 1, Mathf.Clamp01(t * 3f));
                yield return null;
            }
            camelPx = PlatformPx; camelBob = 0f;
            PlaceCamel();
            camelRt.localScale = Vector3.one;
            SortScene();

            card.gameObject.SetActive(true);
            cardName.text = (b != null ? b.name : "") + (e.mine ? "  (ناقتك)" : "");
            cardName.color = e.mine ? NaaqaUI.OrangeLight : NaaqaUI.Cream;
            string tr = "";
            for (int k = 0; k < 4; k++) tr += (k > 0 ? "  •  " : "") + TraitNames[k] + " " + NaaqaUI.Ar(e.traits[k]) + "/١٠";
            cardTraits.text = tr;
            cardJudges.text = "";

            // أنت تقيّم أول (شوف مواصفاتها وقيّم بعدل)
            rated = -1;
            SetRating(true);
            float limit = 15f;
            while (rated < 0 && limit > 0f)
            {
                limit -= Time.unscaledDeltaTime;
                cardAsk.text = "قيّمها أنت بعد (شوف مواصفاتها):  " + NaaqaUI.Ar(Mathf.CeilToInt(limit));
                yield return null;
            }
            SetRating(false);
            e.player = rated;
            cardAsk.text = rated > 0 ? "تقييمك: " + NaaqaUI.Ar(rated) + " من ١٠" : "ما قيّمت، ناخذ رأي المحكمين";

            // المحكمين
            string js = "";
            for (int k = 0; k < 3; k++)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                if (judges.Count > k && judges[k] != null)
                    judges[k].Say(JudgeLines[Random.Range(0, JudgeLines.Length)] + " • " + NaaqaUI.Ar(e.judge[k]), 2.6f);
                js += (k > 0 ? "   " : "") + NaaqaUI.Ar(e.judge[k]);
                cardJudges.text = "المحكمين:  " + js;
            }
            yield return new WaitForSecondsRealtime(0.5f);

            // عدالة تقييمك: لازم يكون قريب من رأي المحكمين (فرق ٢ أو أقل)
            e.fair = rated > 0 && Mathf.Abs(rated - e.Avg) <= 2f;
            string fairText = rated <= 0 ? "" : e.fair ? "تقييمك عادل وانحسب  •  " : "تقييمك بعيد عن المحكمين وما انحسب  •  ";
            cardJudges.text = "المحكمين:  " + js + "\n" + fairText + "المجموع " + NaaqaUI.Ar(e.Final) + " من ٤٠";
            yield return new WaitForSecondsRealtime(2f);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                camelImg.color = new Color(1, 1, 1, 1f - t);
                yield return null;
            }
            camelRt.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
        }

        // النتيجة (لو تعادل، ناقتك تطلع بعد الأعلى منها)
        List<Entry> rank = new List<Entry>(entries);
        rank.Sort((x, y) => y.Final != x.Final ? y.Final.CompareTo(x.Final) : (x.mine ? 1 : y.mine ? -1 : 0));
        int place = rank.FindIndex(x => x.mine) + 1;
        for (int i = 0; i < resultRows.Count; i++)
        {
            Entry e = i < rank.Count ? rank[i] : null;
            if (e == null) { resultRows[i].text = ""; continue; }
            CamelBreeds.Breed b = CamelBreeds.Get(e.breed);
            resultRows[i].text = NaaqaUI.Ar(i + 1) + ".  " + (b != null ? b.name : "") + (e.mine ? " (ناقتك)" : "") + "  •  " + NaaqaUI.Ar(e.Final) + " من ٤٠";
            resultRows[i].color = e.mine ? NaaqaUI.OrangeLight : NaaqaUI.Cream;
            resultRows[i].fontStyle = e.mine ? FontStyles.Bold : FontStyles.Normal;
        }

        // الفائزة ١٨، وغير الفائزة ٥ (مرة وحدة لكل جولة)
        int prize = place == 1 ? Prize : GameRules.MazayenLose;
        if (prize > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(prize);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }
        if (place == 1 && FarmStore.Instance != null) FarmStore.Instance.AddMazayenWin();
        LevelManager.AddXp(LevelManager.XpPerMazayen);

        resultTitle.text = place == 1 ? "ناقتك ملكة المزاين!" : "ناقتك جات " + (place == 2 ? "الثانية" : place == 3 ? "الثالثة" : "الرابعة");
        resultSub.text = place == 1 ? "الجائزة +" + NaaqaUI.Ar(prize) + " قرش" : "مكافأة المشاركة +" + NaaqaUI.Ar(prize) + " قرش، والمرة الجاية إن شاء الله";
        resultSub.color = prize > 0 ? NaaqaUI.OkColor : NaaqaUI.Muted;
        results.gameObject.SetActive(true);
        bigText.text = "";
        if (place == 1)
        {
            CamelSounds.Play("crowd");
            if (NotificationManager.Instance != null) NotificationManager.Instance.Show("فازت ناقتك في المزاين! +" + NaaqaUI.Ar(prize) + " قرش");
            foreach (NpcWalker w in crowd) if (w != null && Random.value < 0.6f) w.Say("مبروك! تستاهل", 2.5f);
        }

        judging = false;
        Refresh();
    }

    IEnumerator FadeIn()
    {
        group.alpha = 0f; group.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f) { group.alpha = t; yield return null; }
        group.alpha = 1f;
    }

    // الناقة على المنصة بمقاس الخلفية الحالي
    void PlaceCamel()
    {
        float h = 330f * K;
        camelRt.sizeDelta = new Vector2(h * 1.04f, h);
        camelRt.anchoredPosition = ToScene(camelPx) + new Vector2(0f, camelBob * K);
    }

    void Update()
    {
        if (camelRt != null && camelRt.gameObject.activeSelf) PlaceCamel();
        float fit = Mathf.Clamp(canvasRect.rect.width / 1800f, 0.6f, 1f);
        lobby.localScale = card.localScale = results.localScale = Vector3.one * fit;
    }

    public void Close()
    {
        if (judging) return;
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
