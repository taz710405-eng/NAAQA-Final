using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ===== سباقات الهجن =====
// ميدان سباق في نجد: تختار ناقتك والشوط، والهجن تتسابق لفة كاملة على المضمار.
// شوط واحد: نسبة الفوز ٦٠٪ وتزيد أو تنقص حسب سلالة ناقتك
// (الأصايل أسرع، والمجاهيم بطيئة لأنها للحليب).
// النتيجة تنحسب أول، وبعدها كل ناقة تاخذ سرعة تتغير على طول الشوط عشان يصير فيه تجاوز وإثارة.
public class RaceScreen : MonoBehaviour
{
    public static RaceScreen Instance;

    class RaceTier
    {
        public string name; public int fee, prize, level; public float odds;
        public RaceTier(string n, int f, int p, int l, float o) { name = n; fee = f; prize = p; level = l; odds = o; }
    }

    // شوط واحد مضبوط (نقدر نزيد أشواط بعدين)
    static readonly RaceTier[] Tiers =
    {
        new RaceTier("شوط الهجن", GameRules.RaceFee, GameRules.RaceWin, GameRules.RaceUnlock, GameRules.RaceOdds1),
    };

    static readonly string[] RivalNames = { "الشاهين", "مزنة", "الريم", "صقرة", "الغزالة", "سحاب" };

    // ===== المضمار (من صورة الميدان ١٩٢٠×١٠٨٠) =====
    const float CX = 986f, CY = 448f, RX = 857f, RY = 231f, ROT = -5.09f, EXP = 0.723f;
    const float StartT = -0.45f;     // عند بوابة الانطلاق (يمين فوق)
    const float WinnerTime = 16f;    // كم ثانية ياخذ الفايز تقريبًا

    Canvas canvas;
    CanvasGroup group;
    RectTransform scene, panel, canvasRect, board;
    RTLTextMeshPro bigText, statusText, winsText, pickText, oddsHint;
    GameObject resultPanel;
    RTLTextMeshPro resultTitle, resultSub;
    readonly List<Button> tierButtons = new List<Button>();
    readonly List<RTLTextMeshPro> tierLabels = new List<RTLTextMeshPro>();
    readonly List<RTLTextMeshPro> boardRows = new List<RTLTextMeshPro>();
    readonly List<RectTransform> fans = new List<RectTransform>();   // الجمهور القاعد على المدرجات
    bool cheering;
    bool racing = false;
    int pick = 0;   // أي ناقة من إبلك

    class Runner
    {
        public RectTransform rt, body, ring, tag;
        public float s, lane, phase;
        public bool player;
        public int place;
        public string name;
        public float[] table;   // الوقت عند كل جزء من المسافة
        public float total;
    }
    readonly List<Runner> runners = new List<Runner>();
    const int Steps = 200;

    public static void Show()
    {
        if (Instance == null)
        {
            Instance = new GameObject("RaceScreen").AddComponent<RaceScreen>();
            Instance.Build();
        }
        Instance.gameObject.SetActive(true);
        Instance.Open();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ===== البناء =====
    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("RaceCanvas", 700);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        Image blocker = NaaqaUI.Img(root, "Blocker", null, new Color32(60, 35, 15, 255));
        NaaqaUI.Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;

        // خلفية تعبّي الأطراف (نفس صورة الميدان بس أغمق)، عشان ما يطلع فراغ على الآيباد والجوالات العريضة
        RectTransform fill = NaaqaUI.Rect("BgFill", root);
        NaaqaUI.Stretch(fill);
        AspectRatioFitter ff = fill.gameObject.AddComponent<AspectRatioFitter>();
        ff.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ff.aspectRatio = 16f / 9f;
        Image fillImg = NaaqaUI.Img(fill, "Img", NaaqaUI.Load("race_bg"), new Color(0.55f, 0.5f, 0.45f, 1f));
        NaaqaUI.Stretch(fillImg.rectTransform);

        // الميدان كامل يدخل في الشاشة على كل المقاسات (قبل كان يتقص: الأطراف على الآيباد، وفوق وتحت على الجوال)
        scene = NaaqaUI.Rect("Scene", root);
        NaaqaUI.Stretch(scene);
        AspectRatioFitter f = scene.gameObject.AddComponent<AspectRatioFitter>();
        f.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        f.aspectRatio = 16f / 9f;
        Image bg = NaaqaUI.Img(scene, "Bg", NaaqaUI.Load("race_bg"), Color.white);
        NaaqaUI.Stretch(bg.rectTransform);

        BuildTopBar(root);
        BuildBoard(root);
        BuildPanel(root);
        BuildOverlayTexts(root);
    }

    void BuildTopBar(Transform root)
    {
        Image pill = NaaqaUI.Img(root, "TitlePill", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 30f);
        NaaqaUI.Place(pill.rectTransform, new Vector2(1, 1), new Vector2(380, 84), new Vector2(-30, -22));
        Image icon = NaaqaUI.IconImg(pill.transform, "race", 72);
        NaaqaUI.Place(icon.rectTransform, new Vector2(1, 0.5f), new Vector2(72, 72), new Vector2(-6, 0));
        RTLTextMeshPro t = NaaqaUI.Text(pill.transform, "Title", "ميدان الهجن", 36, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Stretch(t.rectTransform, 16, 92, 0, 3);

        Button back = NaaqaUI.IconButton(root, "back", "رجوع للحوش", 88, Close);
        NaaqaUI.Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(88, 88), new Vector2(40, -20));
    }

    // الترتيب المباشر وقت الشوط
    void BuildBoard(Transform root)
    {
        Image b = NaaqaUI.Img(root, "Board", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.82f), 24f);
        NaaqaUI.Place(b.rectTransform, new Vector2(1, 1), new Vector2(330, 250), new Vector2(-30, -124));
        board = b.rectTransform;
        RTLTextMeshPro h = NaaqaUI.Text(board, "H", "الترتيب", 26, NaaqaUI.OrangeLight, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(h.rectTransform, new Vector2(1, 1), new Vector2(290, 44), new Vector2(-20, -8));
        for (int i = 0; i < 4; i++)
        {
            RTLTextMeshPro r = NaaqaUI.Text(board, "R" + i, "", 24, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight);
            NaaqaUI.Place(r.rectTransform, new Vector2(1, 1), new Vector2(290, 44), new Vector2(-20, -52 - i * 46));
            boardRows.Add(r);
        }
        board.gameObject.SetActive(false);
    }

    void BuildPanel(Transform root)
    {
        panel = NaaqaUI.Rect("Panel", root);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(980, 0);
        panel.anchoredPosition = new Vector2(0, 24);

        VerticalLayoutGroup v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(30, 30, 22, 18);
        v.spacing = 12;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bg = NaaqaUI.Img(panel, "Bg", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 240), new Color32(22, 13, 8, 245), "raceCard"), Color.white, 30f);
        NaaqaUI.Stretch(bg.rectTransform); bg.raycastTarget = true;
        bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Image border = NaaqaUI.Img(panel, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.3f), 30f);
        NaaqaUI.Stretch(border.rectTransform);
        border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        // اختيار الناقة
        RectTransform head = NaaqaUI.Rect("Head", panel);
        NaaqaUI.Height(head, 64);
        RTLTextMeshPro h = NaaqaUI.Text(head, "Title", "ناقتك:", 28, NaaqaUI.OrangeLight, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(h.rectTransform, new Vector2(1, 0.5f), new Vector2(120, 60), Vector2.zero);
        pickText = NaaqaUI.Text(head, "Pick", "", 30, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(pickText.rectTransform, new Vector2(1, 0.5f), new Vector2(240, 60), new Vector2(-126, 0));
        RTLTextMeshPro pt;
        Button swap = NaaqaUI.MakeButton(head, "Swap", "بدّل الناقة", NaaqaUI.ButtonStyle.Secondary, 24, out pt);
        NaaqaUI.Place((RectTransform)swap.transform, new Vector2(1, 0.5f), new Vector2(200, 56), new Vector2(-380, 0));
        swap.onClick.AddListener(() => Pick(1));
        winsText = NaaqaUI.Text(head, "Wins", "", 22, NaaqaUI.Muted, TextAlignmentOptions.MidlineLeft);
        NaaqaUI.Place(winsText.rectTransform, new Vector2(0, 0.5f), new Vector2(300, 60), Vector2.zero);

        RectTransform row = NaaqaUI.Rect("Tiers", panel);
        NaaqaUI.Height(row, 140);
        HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.reverseArrangement = true; hl.spacing = 14;
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;

        for (int i = 0; i < Tiers.Length; i++)
        {
            int idx = i;
            RTLTextMeshPro lbl;
            Button b = NaaqaUI.MakeButton(row, "Tier" + i, "", NaaqaUI.ButtonStyle.Primary, 23, out lbl);
            b.onClick.AddListener(() => StartRace(idx));
            tierButtons.Add(b);
            tierLabels.Add(lbl);
        }

        oddsHint = NaaqaUI.Text(panel, "Hint", "", 21, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(oddsHint, 30);
        statusText = NaaqaUI.Text(panel, "Status", "", 22, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(statusText, 30);
    }

    void BuildOverlayTexts(Transform root)
    {
        bigText = NaaqaUI.Text(root, "Big", "", 130, Color.white, TextAlignmentOptions.Center, true);
        NaaqaUI.Center(bigText.rectTransform, new Vector2(1000, 220), new Vector2(0, 60));
        CaptionOutline bo = bigText.gameObject.AddComponent<CaptionOutline>();
        bo.width = 0.18f;
        bo.color = new Color32(80, 30, 0, 255);

        Image r = NaaqaUI.Img(root, "Result", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 246), new Color32(22, 13, 8, 250), "raceCard"), Color.white, 32f);
        NaaqaUI.Center(r.rectTransform, new Vector2(680, 340), new Vector2(0, 40));
        r.raycastTarget = true;
        Image rb = NaaqaUI.Img(r.transform, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.4f), 32f);
        NaaqaUI.Stretch(rb.rectTransform);
        Image tr = NaaqaUI.IconImg(r.transform, "trophy", 96);
        NaaqaUI.Place(tr.rectTransform, new Vector2(0.5f, 1), new Vector2(96, 96), new Vector2(0, -18));
        resultTitle = NaaqaUI.Text(r.transform, "Title", "", 46, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        NaaqaUI.Center(resultTitle.rectTransform, new Vector2(620, 70), new Vector2(0, 30));
        resultSub = NaaqaUI.Text(r.transform, "Sub", "", 27, NaaqaUI.Cream, TextAlignmentOptions.Center);
        NaaqaUI.Center(resultSub.rectTransform, new Vector2(620, 60), new Vector2(0, -32));
        RTLTextMeshPro ok;
        Button okb = NaaqaUI.MakeButton(r.transform, "Ok", "تمام", NaaqaUI.ButtonStyle.Primary, 28, out ok);
        NaaqaUI.Center((RectTransform)okb.transform, new Vector2(240, 70), new Vector2(0, -115));
        okb.onClick.AddListener(() => { resultPanel.SetActive(false); panel.gameObject.SetActive(true); board.gameObject.SetActive(false); });
        resultPanel = r.gameObject;
        resultPanel.SetActive(false);
    }

    // ===== المضمار =====
    // lane: موجب = برّا، سالب = داخل
    Vector2 TrackPx(float t, float lane)
    {
        float r = ROT * Mathf.Deg2Rad;
        float ct = Mathf.Cos(t), st = Mathf.Sin(t);
        float rx = RX + lane, ry = RY + lane * 0.75f;
        float x = rx * Mathf.Sign(ct) * Mathf.Pow(Mathf.Abs(ct), EXP);
        float y = ry * Mathf.Sign(st) * Mathf.Pow(Mathf.Abs(st), EXP);
        return new Vector2(CX + x * Mathf.Cos(r) - y * Mathf.Sin(r), CY + x * Mathf.Sin(r) + y * Mathf.Cos(r));
    }

    Vector2 ToScene(Vector2 px)
    {
        Rect s = scene.rect;
        return new Vector2((px.x / 1920f - 0.5f) * s.width, (0.5f - px.y / 1080f) * s.height);
    }

    float SceneScale { get { return scene.rect.height / 1080f; } }

    // ===== الناقة المختارة =====
    // بس السلالات المؤهلة للسباق اللي عند اللاعب
    List<string> Owned()
    {
        List<string> l = new List<string>();
        foreach (string id in CamelBreeds.OwnedBreeds())
        {
            CamelBreeds.Breed b = CamelBreeds.Get(id);
            if (b != null && b.RacingEligible) l.Add(id);
        }
        return l;
    }

    CamelBreeds.Breed Picked()
    {
        List<string> o = Owned();
        if (o.Count == 0) return null;   // ما عنده ناقة مؤهلة
        pick = ((pick % o.Count) + o.Count) % o.Count;
        return CamelBreeds.Get(o[pick]);
    }

    void Pick(int d)
    {
        if (racing) return;
        pick += d;
        RefreshTiers();
    }

    // نسبة الفوز: الأساس للشوط × قوة السلالة في السباق
    static float WinChance(RaceTier t, CamelBreeds.Breed b)
    {
        if (b == null || !b.RacingEligible) return 0f;
        // السرعة والتحمل يأثرون: الحرة تقريبًا ٥٨٪، العمانيات تقريبًا ٧٣٪ (مو مضمون أبد)
        float factor = 0.55f + b.RaceSpeed * 3.5f + b.Stamina * 0.25f;
        return Mathf.Clamp(t.odds * factor, 0.05f, 0.8f);
    }

    // ===== فتح =====
    void Open()
    {
        StopAllCoroutines();
        racing = false;
        resultPanel.SetActive(false);
        board.gameObject.SetActive(false);
        panel.gameObject.SetActive(true);
        bigText.text = "";
        statusText.text = "";
        // نبدأ بأسرع ناقة عندك
        List<string> o = Owned();
        CamelBreeds.Breed best = CamelBreeds.BestRacer();
        pick = best != null ? Mathf.Max(0, o.IndexOf(best.id)) : 0;
        RefreshTiers();
        StartCoroutine(FadeIn());
        StartCoroutine(SetupScene());
    }

    IEnumerator SetupScene()
    {
        yield return null;   // ننتظر مقاس الخلفية
        foreach (RectTransform w in fans) if (w != null) Destroy(w.gameObject);
        fans.Clear();
        ClearRunners();

        float k = SceneScale;
        // الجمهور قاعد على كراسي المدرجات (يبان نصهم الفوقاني بس، صغار لأنهم بعيدين)
        // من ورا لقدام عشان اللي قدام يغطي اللي وراه
        Vector2[] spots =
        {
            new Vector2(1150, 112), new Vector2(1225, 108), new Vector2(925, 132), new Vector2(1010, 128), new Vector2(1060, 126),
            new Vector2(1120, 136), new Vector2(1195, 132), new Vector2(700, 156), new Vector2(790, 152), new Vector2(950, 150),
            new Vector2(1035, 146), new Vector2(455, 180), new Vector2(540, 176), new Vector2(735, 176), new Vector2(820, 172),
            new Vector2(430, 204), new Vector2(505, 200), new Vector2(580, 196),
        };
        System.Array.Sort(spots, (a, b) => a.y.CompareTo(b.y));
        string[] who = { "npc_man", "npc_bisht", "npc_elder", "npc_man", "npc_woman", "npc_boy" };
        for (int i = 0; i < spots.Length; i++)
            fans.Add(Seated(who[(i * 5 + 1) % who.Length], ToScene(spots[i]), 30f * k));
        // الهجن عند البوابة: ناقتك بالحارة الثانية
        float[] lanes = { -36f, -12f, 12f, 36f };
        int me = 1;
        List<string> names = new List<string>(RivalNames);
        for (int i = 0; i < 4; i++)
        {
            string n = "ناقتك";
            if (i != me) { int j = Random.Range(0, names.Count); n = names[j]; names.RemoveAt(j); }
            AddRunner(i == me, lanes[i], n);
        }
        PlaceRunners();
    }

    // واحد من الجمهور قاعد: نقص نصه الفوقاني من صورته
    RectTransform Seated(string sprite, Vector2 pos, float height)
    {
        Sprite full = NaaqaUI.Load(sprite);
        RectTransform rt = NaaqaUI.Rect("Fan", scene);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        if (full == null) { rt.sizeDelta = Vector2.zero; return rt; }
        Rect r = full.rect;
        float keep = 0.55f;
        Sprite top = Sprite.Create(full.texture, new Rect(r.x, r.y + r.height * (1f - keep), r.width, r.height * keep), new Vector2(0.5f, 0f), 100f);
        Image img = NaaqaUI.Img(rt, "Body", top, Color.white);
        img.preserveAspect = true;
        img.raycastTarget = false;
        NaaqaUI.Stretch(img.rectTransform, 0, 0, 0, 0);
        rt.sizeDelta = new Vector2(height * (r.width / (r.height * keep)), height);
        if (Random.value < 0.5f) rt.localScale = new Vector3(-1, 1, 1);
        return rt;
    }

    // وقت الشوط الجمهور يتحمس (يقفز شوي)، وبعده يهدأ
    void CheerFans(float t)
    {
        for (int i = 0; i < fans.Count; i++)
        {
            RectTransform f = fans[i];
            if (f == null || f.childCount == 0) continue;
            RectTransform body = (RectTransform)f.GetChild(0);
            float jump = cheering ? Mathf.Max(0f, Mathf.Sin(t * (7f + i % 3) + i * 1.7f)) * 5f * SceneScale : 0f;
            body.anchoredPosition = new Vector2(0f, jump);
        }
    }

    void AddRunner(bool player, float lane, string name)
    {
        float k = SceneScale;
        RectTransform rt = NaaqaUI.Rect(player ? "MyCamel" : "Camel", scene);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.08f);
        Sprite s = NaaqaUI.Load("race_camel");
        float aspect = s != null ? s.rect.width / s.rect.height : 1f;
        float h = 112f * k;
        rt.sizeDelta = new Vector2(h * aspect, h);

        // دائرة تحت ناقتك عشان تبان
        Image ring = NaaqaUI.Img(rt, "Ring", NaaqaUI.Circle(), player ? new Color(1f, 0.6f, 0.1f, 0.75f) : new Color(0, 0, 0, 0.22f));
        ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.08f);
        ring.rectTransform.sizeDelta = new Vector2(h * aspect * 0.95f, h * 0.26f);

        Image img = NaaqaUI.Img(rt, "Body", s, Color.white);
        NaaqaUI.Stretch(img.rectTransform);
        img.preserveAspect = true;
        img.rectTransform.pivot = new Vector2(0.5f, 0.08f);
        if (!player)
        {
            Color[] tint = { new Color(0.86f, 0.86f, 1f), new Color(1f, 0.86f, 0.8f), new Color(0.88f, 1f, 0.86f) };
            img.color = tint[runners.Count % tint.Length];
        }

        Runner r = new Runner { rt = rt, body = img.rectTransform, ring = ring.rectTransform, lane = lane, player = player, name = name, phase = Random.value * 6f };

        if (player)
        {
            // اسم واضح فوقها
            Image tag = NaaqaUI.Img(rt, "Tag", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.9f), 16f);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tag.rectTransform.pivot = new Vector2(0.5f, 0f);
            tag.rectTransform.sizeDelta = new Vector2(150, 44);
            tag.rectTransform.anchoredPosition = new Vector2(0, 8);
            Image tl = NaaqaUI.Img(tag.transform, "Line", NaaqaUI.RoundedOutline(), NaaqaUI.Orange, 16f);
            NaaqaUI.Stretch(tl.rectTransform);
            RTLTextMeshPro tt = NaaqaUI.Text(tag.transform, "T", "ناقتك", 24, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
            NaaqaUI.Stretch(tt.rectTransform, 4, 4, 0, 3);
            r.tag = tag.rectTransform;
        }
        runners.Add(r);
    }

    void ClearRunners()
    {
        foreach (Runner r in runners) if (r.rt != null) Destroy(r.rt.gameObject);
        runners.Clear();
    }

    void PlaceRunners()
    {
        foreach (Runner r in runners)
        {
            float t = StartT - r.s * Mathf.PI * 2f;
            Vector2 p = ToScene(TrackPx(t, r.lane));
            Vector2 ahead = ToScene(TrackPx(t - 0.03f, r.lane));
            r.rt.anchoredPosition = p;
            // الصورة تطالع يسار: لو تجري يمين نقلبها
            float dir = ahead.x >= p.x ? -1f : 1f;
            r.body.localScale = new Vector3(dir, 1f, 1f);
            // الأبعد (فوق) أصغر شوي عشان العمق
            float depth = Mathf.Lerp(0.85f, 1.1f, Mathf.InverseLerp(scene.rect.height * 0.3f, -scene.rect.height * 0.3f, p.y));
            r.rt.localScale = Vector3.one * depth;
        }
        // الأقرب لتحت قدام
        List<Transform> all = new List<Transform>();
        foreach (Transform c in scene) if (c.name != "Bg") all.Add(c);
        all.Sort((a, b) => ((RectTransform)b).anchoredPosition.y.CompareTo(((RectTransform)a).anchoredPosition.y));
        for (int i = 0; i < all.Count; i++) all[i].SetSiblingIndex(1 + i);
    }

    void RefreshTiers()
    {
        int lvl = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        CamelBreeds.Breed b = Picked();
        for (int i = 0; i < Tiers.Length; i++)
        {
            RaceTier t = Tiers[i];
            bool open = lvl >= t.level;
            tierButtons[i].interactable = open && !racing && b != null;
            tierLabels[i].text = open
                ? t.name + "\nالدخول " + NaaqaUI.Ar(t.fee) + " • الجائزة " + NaaqaUI.Ar(t.prize) + " • فرصتك " + NaaqaUI.Ar(Mathf.RoundToInt(WinChance(t, b) * 100f)) + "٪"
                : t.name + "\nيفتح في المستوى " + NaaqaUI.Ar(t.level);
        }
        int wins = FarmStore.Instance != null ? FarmStore.Instance.RaceWins : 0;
        pickText.text = b != null ? b.name : "ما عندك ناقة مؤهلة";
        winsText.text = "فزت " + NaaqaUI.Ar(wins) + " مرة";
        oddsHint.text = b == null ? "الناقة غير مؤهلة للسباق. السلالات المؤهلة: الحرة، الأصايل، شقح السباق، السودانيات، العمانيات"
                      : b.RaceSpeed >= 0.08f ? "سلالة سباق قوية! فرصتها أعلى" : "سلالة سباق";
    }

    // ===== السباق =====
    void StartRace(int tier)
    {
        if (racing) return;
        RaceTier t = Tiers[tier];
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (Picked() == null) { statusText.color = NaaqaUI.ErrorColor; statusText.text = "الناقة غير مؤهلة للسباق"; return; }
        if (!gm.SpendMoney(t.fee)) { statusText.color = NaaqaUI.ErrorColor; statusText.text = "ما عندك قروش كافية! الدخول يبي " + NaaqaUI.Ar(t.fee) + " قرش"; return; }
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        StartCoroutine(Race(t));
    }

    // يجهز سرعة الناقة على طول الشوط (تتغير) بحيث تخلص بالضبط بالوقت المطلوب
    void Plan(Runner r, float finishTime)
    {
        int knots = 7;
        float[] k = new float[knots];
        for (int i = 0; i < knots; i++) k[i] = Random.Range(0.82f, 1.18f);
        float[] tab = new float[Steps + 1];
        float time = 0f;
        tab[0] = 0f;
        for (int j = 1; j <= Steps; j++)
        {
            float u = (j - 0.5f) / Steps;
            float f = u * (knots - 1);
            int a = Mathf.Min(knots - 2, Mathf.FloorToInt(f));
            float w = f - a;
            w = w * w * (3f - 2f * w);
            float pace = Mathf.Lerp(k[a], k[a + 1], w);
            float ramp = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(u / 0.07f));   // تسارع عند الانطلاق
            time += (1f / Steps) / (pace * ramp);
            tab[j] = time;
        }
        float scale = finishTime / time;
        for (int j = 0; j <= Steps; j++) tab[j] *= scale;
        r.table = tab;
        r.total = finishTime;
    }

    // المسافة عند وقت معيّن
    static float DistanceAt(Runner r, float t)
    {
        if (t <= 0f) return 0f;
        if (t >= r.total) return 1f;
        int lo = 0, hi = Steps;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (r.table[mid] <= t) lo = mid; else hi = mid;
        }
        float a = r.table[lo], b = r.table[hi];
        return (lo + (t - a) / Mathf.Max(0.0001f, b - a)) / Steps;
    }

    IEnumerator Race(RaceTier tier)
    {
        racing = true;
        panel.gameObject.SetActive(false);
        RefreshTiers();

        // النتيجة تنحسب أول (حسب النسبة)
        CamelBreeds.Breed b = Picked();
        bool win = Random.value < WinChance(tier, b);
        Runner me = runners.Find(x => x.player);
        List<Runner> order = new List<Runner>(runners);
        order.Remove(me);
        for (int i = 0; i < order.Count; i++) { int j = Random.Range(i, order.Count); Runner tmp = order[i]; order[i] = order[j]; order[j] = tmp; }
        int myPlace = win ? 0 : (Random.value < 0.5f ? 1 : Random.value < 0.6f ? 2 : 3);
        order.Insert(myPlace, me);

        float t0 = WinnerTime + Random.Range(-0.8f, 0.8f);
        float fin = t0;
        for (int i = 0; i < order.Count; i++)
        {
            if (i > 0) fin += Random.Range(0.18f, 0.75f);   // فرق قريب عشان الإثارة
            Plan(order[i], fin);
            order[i].s = 0f;
            order[i].place = 0;
        }
        PlaceRunners();

        // ٣ ٢ ١ انطلق
        string[] count = { "٣", "٢", "١", "انطلق!" };
        foreach (string c in count)
        {
            bigText.text = c;
            yield return new WaitForSecondsRealtime(0.75f);
        }
        bigText.text = "";
        board.gameObject.SetActive(true);
        CamelSounds.Play("crowd");
        cheering = true;

        float time = 0f;
        float cheer = 0f;
        int finished = 0;
        while (finished < runners.Count)
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            foreach (Runner r in runners)
            {
                float before = r.s;
                r.s = DistanceAt(r, time);
                if (r.s >= 1f && r.place == 0) { finished++; r.place = finished; }
                // حركة الجري: تهتز لفوق وتحت حسب سرعتها
                float speed = (r.s - before) / Mathf.Max(0.0001f, dt);
                r.phase += dt * (6f + speed * 260f);
                float bob = r.place == 0 ? Mathf.Abs(Mathf.Sin(r.phase)) * 7f * SceneScale : 0f;
                r.body.anchoredPosition = new Vector2(0f, bob);
                r.body.localRotation = Quaternion.Euler(0, 0, r.place == 0 ? Mathf.Sin(r.phase) * 2.5f : 0f);
            }
            PlaceRunners();
            UpdateBoard();

            cheer += dt;
            CheerFans(cheer);
            yield return null;
        }

        // النتيجة
        int place = me != null ? me.place : 4;
        // الفائز ١٢، وغير الفائز ٣ (مرة وحدة لكل شوط)
        int prize = place == 1 ? tier.prize : GameRules.RaceLose;
        if (prize > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(prize);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }
        if (place == 1 && FarmStore.Instance != null) FarmStore.Instance.AddRaceWin();
        // جائزة نادرة: كأس السباق (بعد مستوى ٢٠، مرة من كل ٧ تقريبًا)
        int lvNow = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (place == 1 && lvNow >= GameRules.TrophyLevel && Random.value < 0.15f && InventoryManager.Instance != null && InventoryManager.Instance.GetData("trophy") != null)
        {
            InventoryManager.Instance.Add("trophy");
            if (NotificationManager.Instance != null) NotificationManager.Instance.Show("جائزة نادرة! كأس السباق صار في شنطتك، ركّبه في حوشك");
            MailBox.Add("مبروك الفوز! عطيناك كأس السباق النادر، تلقاه في الشنطة", 0, 0);
        }
        LevelManager.AddXp(LevelManager.XpPerRace);

        string[] titles = { "", "ناقتك الأولى!", "ناقتك الثانية", "ناقتك الثالثة", "ناقتك الرابعة" };
        resultTitle.text = titles[Mathf.Clamp(place, 1, 4)];
        resultSub.text = place == 1 ? "الجائزة +" + NaaqaUI.Ar(prize) + " قرش" : "مكافأة المشاركة +" + NaaqaUI.Ar(prize) + " قرش، والمرة الجاية إن شاء الله تفوز";
        resultPanel.SetActive(true);
        if (place == 1 && NotificationManager.Instance != null)
            NotificationManager.Instance.Show("فازت " + (b != null ? b.name : "ناقتك") + " في " + tier.name + "! +" + NaaqaUI.Ar(prize) + " قرش");
        cheering = false;
        CheerFans(0f);

        racing = false;
        RefreshTiers();

        yield return new WaitForSecondsRealtime(1.5f);
        foreach (Runner r in runners) { r.s = 0f; r.place = 0; r.body.anchoredPosition = Vector2.zero; r.body.localRotation = Quaternion.identity; }
        PlaceRunners();
    }

    void UpdateBoard()
    {
        List<Runner> sorted = new List<Runner>(runners);
        sorted.Sort((a, b) =>
        {
            if (a.place > 0 && b.place > 0) return a.place.CompareTo(b.place);
            if (a.place > 0) return -1;
            if (b.place > 0) return 1;
            return b.s.CompareTo(a.s);
        });
        for (int i = 0; i < boardRows.Count && i < sorted.Count; i++)
        {
            boardRows[i].text = NaaqaUI.Ar(i + 1) + ".  " + sorted[i].name;
            boardRows[i].color = sorted[i].player ? NaaqaUI.OrangeLight : NaaqaUI.Cream;
            boardRows[i].fontStyle = sorted[i].player ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    void Update()
    {
        if (panel.rect.height > 0f) panel.localScale = Vector3.one * Mathf.Min(1f, (canvasRect.rect.width - 60f) / 980f);
        // دائرة ناقتك تنبض
        foreach (Runner r in runners)
            if (r.player && r.ring != null)
                r.ring.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f);
    }

    IEnumerator FadeIn()
    {
        group.alpha = 0f; group.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f) { group.alpha = t; yield return null; }
        group.alpha = 1f;
    }

    public void Close()
    {
        if (racing) return;   // ما نطلع في نص الشوط
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
