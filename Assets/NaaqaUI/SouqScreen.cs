using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ===== السوق الشعبي =====
// سوق نجدي يخدم العزبة:
//   - المتجر: الخيمة، بسطة الحليب، وزينة بسيطة (معلف، حوض ماء، نخلة)
//   (بيع الحليب صار من بسطة الحليب في العزبة: StallCustomers)
//   - العلف، وسوق الإبل
// وفيه ناس يمشون ويتكلمون نجدي، وزباين يجون للبسطة ويسلّمون.
public class SouqScreen : MonoBehaviour
{
    public static SouqScreen Instance;

    public const int MilkPrice = GameRules.MilkPrice;     // سعر الحليب للحبة (صفحة قديمة مخفية)

    Canvas canvas;
    CanvasGroup group;
    RectTransform scene, panel, canvasRect, card;
    const float CardSize = 1000f;
    AudioSource ambience;
    readonly List<NpcWalker> walkers = new List<NpcWalker>();
    NpcWalker seller;

    enum Tab { Stall, Shop }
    GameObject stallPage, shopPage;
    Image stallTab, shopTab;

    // بيع الحليب
    RTLTextMeshPro haveText, qtyText, sellText, sellAllText, status;
    Button sellBtn, sellAllBtn;
    int qty = 1;

    // المتجر
    RectTransform shopGrid;
    readonly List<GameObject> shopCells = new List<GameObject>();
    string lastBought;
    GameObject placeNowBtn;
    GameObject workBtn;
    Transform backBtn;

    // للشرح: كرت غرض في المتجر، وزر الرجوع
    public static Transform Tile(string id)
    {
        if (Instance == null || Instance.shopGrid == null) return null;
        Transform t = Instance.shopGrid.Find("Item_" + id);
        return t != null && t.gameObject.activeInHierarchy ? t : null;
    }
    public static Transform BackButton { get { return Instance != null ? Instance.backBtn : null; } }   // التوظيف: يطلع بس لما يفتح (مستوى ١٣)

    // زباين يجون للبسطة (شكل وحوار بس)
    float nextVisitor = 3f;
    static readonly string[] Shoppers = { "npc_man", "npc_bisht", "npc_woman", "npc_elder", "npc_boy" };
    static readonly string[] VisitorLines = { "السلام عليكم يا الغالي", "حليبكم طازج ما شاء الله", "الله يبارك لك في حلالك", "عساك طيب، وش علومك؟" };
    static readonly string[] SellerLines = { "هلا والله، حيّاك", "وعليكم السلام، آمر", "أبشر بالخير", "الله يحييك" };

    public static void Show()
    {
        if (Instance == null)
        {
            Instance = new GameObject("SouqScreen").AddComponent<SouqScreen>();
            Instance.Build();
        }
        Instance.gameObject.SetActive(true);
        Instance.Open();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ===== البناء =====
    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("SouqCanvas", 700);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        // اللعبة وراها تغمق، والسوق نافذة مربعة بالنص خلفيتها صورة السوق نفسها (بدون ناس يمشون)
        Image blocker = NaaqaUI.Img(root, "Blocker", null, new Color(0.03f, 0.015f, 0f, 0.72f));
        NaaqaUI.Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;

        card = NaaqaUI.Rect("Card", root);
        NaaqaUI.Center(card, new Vector2(CardSize, CardSize), Vector2.zero);
        Image cshadow = NaaqaUI.Img(card, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.6f), 36f);
        NaaqaUI.Stretch(cshadow.rectTransform, -40, -40, -30, -50);

        RectTransform clip = NaaqaUI.Rect("Clip", card);
        NaaqaUI.Stretch(clip);
        clip.gameObject.AddComponent<RectMask2D>();
        scene = NaaqaUI.Rect("Scene", clip);
        NaaqaUI.Stretch(scene);
        AspectRatioFitter fitter = scene.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;
        Image bg = NaaqaUI.Img(scene, "Bg", NaaqaUI.Load("souq_bg"), Color.white);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Image cborder = NaaqaUI.Img(card, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.75f), 36f);
        NaaqaUI.Stretch(cborder.rectTransform);

        BuildTopBar(card);
        BuildPanel(card);
        // البيع صار من بسطة الحليب في العزبة نفسها، فالسوق للمتجر والعلف وسوق الإبل بس
        stallTab.gameObject.SetActive(false);
        Select(Tab.Shop);
    }

    void BuildTopBar(Transform root)
    {
        Image pill = NaaqaUI.Img(root, "TitlePill", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 30f);
        NaaqaUI.Place(pill.rectTransform, new Vector2(1, 1), new Vector2(400, 84), new Vector2(-24, -20));
        Image icon = NaaqaUI.IconImg(pill.transform, "souq", 72);
        NaaqaUI.Place(icon.rectTransform, new Vector2(1, 0.5f), new Vector2(72, 72), new Vector2(-6, 0));
        RTLTextMeshPro t = NaaqaUI.Text(pill.transform, "Title", "السوق الشعبي", 36, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Stretch(t.rectTransform, 16, 92, 0, 3);

        Button back = NaaqaUI.IconButton(root, "back", "رجوع للعزبة", 88, Close);
        backBtn = back.transform;
        NaaqaUI.Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(88, 88), new Vector2(26, -18));

        // العلف ينشرى من ➕ اللي جنب عداد العلف، وسوق الإبل صار زر في الشاشة الرئيسية
        // التوظيف (الحلاب)
        Button work = NaaqaUI.IconButton(root, "worker", "التوظيف", 88, () => WorkerScreen.Show());
        NaaqaUI.Place((RectTransform)work.transform, new Vector2(0, 1), new Vector2(88, 88), new Vector2(140, -18));
        workBtn = work.gameObject;
    }

    // ===== اللوحة يمين =====
    void BuildPanel(Transform root)
    {
        panel = NaaqaUI.Rect("Panel", root);
        NaaqaUI.Stretch(panel, 30, 30, 140, 30);

        Image shadow = NaaqaUI.Img(panel, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.6f), 32f);
        NaaqaUI.Stretch(shadow.rectTransform, -36, -36, -26, -44);
        Image bg = NaaqaUI.Img(panel, "Bg", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 225), new Color32(22, 13, 8, 235), "marketBg2"), Color.white, 32f);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Image border = NaaqaUI.Img(panel, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.3f), 32f);
        NaaqaUI.Stretch(border.rectTransform);

        RectTransform tabs = NaaqaUI.Rect("Tabs", panel);
        tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = new Vector2(1, 1);
        tabs.pivot = new Vector2(0.5f, 1f);
        tabs.offsetMin = new Vector2(26, -100); tabs.offsetMax = new Vector2(-26, -22);
        HorizontalLayoutGroup h = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.reverseArrangement = true; h.spacing = 14;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = true;
        stallTab = TabButton(tabs, "milk", "بيع الحليب", Tab.Stall);
        shopTab = TabButton(tabs, "shop", "المتجر", Tab.Shop);

        RectTransform pages = NaaqaUI.Rect("Pages", panel);
        NaaqaUI.Stretch(pages, 26, 26, 118, 70);
        stallPage = BuildStall(pages);
        shopPage = BuildShop(pages);

        status = NaaqaUI.Text(panel, "Status", "", 24, NaaqaUI.Muted, TextAlignmentOptions.Center);
        status.rectTransform.anchorMin = new Vector2(0, 0); status.rectTransform.anchorMax = new Vector2(1, 0);
        status.rectTransform.pivot = new Vector2(0.5f, 0f);
        status.rectTransform.sizeDelta = new Vector2(-40, 56);
        status.rectTransform.anchoredPosition = new Vector2(0, 10);
    }

    Image TabButton(Transform parent, string icon, string label, Tab tab)
    {
        RTLTextMeshPro t;
        Button b = NaaqaUI.MakeButton(parent, "Tab" + tab, label, NaaqaUI.ButtonStyle.Secondary, 28, out t);
        Image ic = NaaqaUI.IconImg(b.transform, icon, 56);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(56, 56), new Vector2(-10, 0));
        t.rectTransform.offsetMax = new Vector2(-72, 0);
        b.onClick.AddListener(() => Select(tab));
        return b.GetComponent<Image>();
    }

    void Select(Tab t)
    {
        stallPage.SetActive(t == Tab.Stall);
        shopPage.SetActive(t == Tab.Shop);
        stallTab.color = t == Tab.Stall ? new Color(1f, 0.6f, 0.15f, 0.55f) : new Color(1f, 1f, 1f, 0.08f);
        shopTab.color = t == Tab.Shop ? new Color(1f, 0.6f, 0.15f, 0.55f) : new Color(1f, 1f, 1f, 0.08f);
        if (t == Tab.Shop) RebuildShop();
        Refresh();
    }

    // ===== صفحة بيع الحليب =====
    GameObject BuildStall(RectTransform pages)
    {
        RectTransform page = NaaqaUI.Rect("Stall", pages);
        NaaqaUI.Stretch(page);

        Image tile = NaaqaUI.Tile(page, "Milk");
        NaaqaUI.Place(tile.rectTransform, new Vector2(0.5f, 1), new Vector2(760, 330), new Vector2(0, -10));
        Image ic = NaaqaUI.IconImg(tile.transform, "milk", 170);
        NaaqaUI.Place(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(170, 170), new Vector2(0, -22));
        haveText = NaaqaUI.Text(tile.transform, "Have", "", 36, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(haveText.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 56), new Vector2(0, -204));
        RTLTextMeshPro price = NaaqaUI.Text(tile.transform, "Price", "سعر الحليب: " + NaaqaUI.Ar(MilkPrice) + " قرش للحبة", 26, NaaqaUI.OrangeLight, TextAlignmentOptions.Center);
        NaaqaUI.Place(price.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 44), new Vector2(0, -262));

        // كم حبة
        RTLTextMeshPro t;
        Button minus = NaaqaUI.MakeButton(page, "Minus", "", NaaqaUI.ButtonStyle.Secondary, 40, out t);
        NaaqaUI.PlusSign(minus.transform, 30f, true, NaaqaUI.Cream);
        NaaqaUI.Place((RectTransform)minus.transform, new Vector2(0.5f, 1), new Vector2(96, 90), new Vector2(150, -370));
        minus.onClick.AddListener(() => Step(-1));
        Image qbg = NaaqaUI.Img(page, "Qty", NaaqaUI.Rounded(), new Color(0, 0, 0, 0.3f), 18f);
        NaaqaUI.Place(qbg.rectTransform, new Vector2(0.5f, 1), new Vector2(180, 90), new Vector2(0, -370));
        qtyText = NaaqaUI.Text(qbg.transform, "N", "", 40, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(qtyText.rectTransform, 0, 0, 0, 4);
        Button plus = NaaqaUI.MakeButton(page, "Plus", "", NaaqaUI.ButtonStyle.Secondary, 40, out t);
        NaaqaUI.PlusSign(plus.transform, 30f, false, NaaqaUI.Cream);
        NaaqaUI.Place((RectTransform)plus.transform, new Vector2(0.5f, 1), new Vector2(96, 90), new Vector2(-150, -370));
        plus.onClick.AddListener(() => Step(1));

        sellBtn = NaaqaUI.MakeButton(page, "Sell", "", NaaqaUI.ButtonStyle.Primary, 30, out sellText);
        NaaqaUI.Place((RectTransform)sellBtn.transform, new Vector2(0.5f, 1), new Vector2(560, 92), new Vector2(0, -486));
        sellBtn.onClick.AddListener(() => Sell(qty));
        sellAllBtn = NaaqaUI.MakeButton(page, "SellAll", "", NaaqaUI.ButtonStyle.Secondary, 26, out sellAllText);
        NaaqaUI.Place((RectTransform)sellAllBtn.transform, new Vector2(0.5f, 1), new Vector2(560, 76), new Vector2(0, -592));
        sellAllBtn.onClick.AddListener(() => Sell(Milk()));

        return page.gameObject;
    }

    int Milk() { return GameManager.Instance != null ? GameManager.Instance.milkCount : 0; }

    void Step(int d)
    {
        qty = Mathf.Clamp(qty + d, 1, Mathf.Max(1, Milk()));
        Refresh();
    }

    void Sell(int n)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        n = Mathf.Min(n, gm.milkCount);
        if (n <= 0) { SetStatus("ما عندك حليب، اجمعه من إبلك", NaaqaUI.ErrorColor); return; }
        int total = n * MilkPrice;
        gm.milkCount -= n;
        gm.AddMoney(total);
        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        LevelManager.AddXp(LevelManager.XpPerOrder);
        Quests.Add("sell", n);
        CamelSounds.Play("pour");
        SetStatus("بعت " + NaaqaUI.Ar(n) + " حليب بـ " + NaaqaUI.Ar(total) + " قرش", NaaqaUI.OkColor);
        if (seller != null) seller.Say(Random.value < 0.5f ? "الله يبارك لك فيها" : "بيعة مباركة يا الغالي", 2.5f);
        qty = 1;
        Refresh();
    }

    // ===== صفحة المتجر =====
    GameObject BuildShop(RectTransform pages)
    {
        RectTransform page = NaaqaUI.Rect("Shop", pages);
        NaaqaUI.Stretch(page);

        RectTransform area = NaaqaUI.Rect("Area", page);
        NaaqaUI.Stretch(area, 0, 0, 0, 0);
        RectTransform view = NaaqaUI.Rect("Viewport", area);
        NaaqaUI.Stretch(view);
        view.gameObject.AddComponent<RectMask2D>();
        Image hit = view.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);
        shopGrid = NaaqaUI.Rect("Content", view);
        shopGrid.anchorMin = new Vector2(0, 1); shopGrid.anchorMax = new Vector2(1, 1);
        shopGrid.pivot = new Vector2(0.5f, 1f);
        shopGrid.sizeDelta = Vector2.zero;
        GridLayoutGroup g = shopGrid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(246, 330);
        g.spacing = new Vector2(12, 12);
        g.startCorner = GridLayoutGroup.Corner.UpperRight;
        g.childAlignment = TextAnchor.UpperCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;
        shopGrid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect sr = view.gameObject.AddComponent<ScrollRect>();
        sr.viewport = view; sr.content = shopGrid; sr.horizontal = false; sr.scrollSensitivity = 30f;

        // بعد الشراء: «ركّبه الحين»
        RTLTextMeshPro pt;
        Button pn = NaaqaUI.MakeButton(page, "PlaceNow", "ركّبه في الحوش الحين", NaaqaUI.ButtonStyle.Primary, 28, out pt);
        NaaqaUI.Place((RectTransform)pn.transform, new Vector2(0.5f, 0), new Vector2(460, 76), new Vector2(0, 6));
        pn.onClick.AddListener(PlaceNow);
        placeNowBtn = pn.gameObject;
        placeNowBtn.SetActive(false);
        return page.gameObject;
    }

    struct ShopItem { public string id, name; public int price, level; public bool once; public Sprite pic; }

    List<ShopItem> ShopItems()
    {
        List<ShopItem> l = new List<ShopItem>();
        InventoryManager inv = InventoryManager.Instance;
        // الخيمة: مرة وحدة لكل حساب، وبعدها تختفي من المتجر
        if (!ShopItemView.IsBought("tent"))
        {
            BagItemData d = inv != null ? inv.GetData("tent") : null;
            if (d != null) l.Add(new ShopItem { id = "tent", name = d.displayName, price = 10, level = 1, once = true, pic = d.icon });
        }
        foreach (ShopExtraItems.ItemDef def in ShopExtraItems.Items)
        {
            BagItemData d = inv != null ? inv.GetData(def.id) : null;
            if (d == null) continue;   // ما له صورة
            if (def.hidden) continue;
            bool once = def.once;
            if (once && ShopItemView.IsBought(def.id)) continue;
            if (def.id == "trough" && FarmStore.Instance != null && FarmStore.Instance.HasTrough) continue;
            int price = def.price, level = def.unlockLevel;
            if (def.id == "pen_extra")
            {
                // الأول بـ٦٠٠ (مستوى ٨)، والثاني بـ٩٠٠ (مستوى ١٤)، وبعدها ما فيه (٣ أحواش كلها)
                int owned = ExtraPen.Owned();
                if (owned >= GameRules.MaxExtraPens) continue;
                price = GameRules.ExtraPenPrice(owned);
                level = GameRules.ExtraPenUnlock(owned);
            }
            l.Add(new ShopItem { id = def.id, name = def.name, price = price, level = level, once = once, pic = d.icon });
        }
        return l;
    }

    void RebuildShop()
    {
        foreach (GameObject g in shopCells) if (g != null) Destroy(g);
        shopCells.Clear();
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        int money = GameManager.Instance != null ? GameManager.Instance.money : 0;

        foreach (ShopItem it in ShopItems())
        {
            ShopItem item = it;
            Image tile = NaaqaUI.Tile(shopGrid, "Item_" + it.id);
            if (it.pic != null)
            {
                Image pic = NaaqaUI.Img(tile.transform, "Pic", it.pic, Color.white);
                pic.preserveAspect = true;
                NaaqaUI.Place(pic.rectTransform, new Vector2(0.5f, 1), new Vector2(200, 150), new Vector2(0, -10));
            }
            float y = -166;
            NaaqaWindow.Line(tile.transform, it.name, 25, NaaqaUI.Cream, true, ref y, 40);
            NaaqaWindow.Line(tile.transform, NaaqaUI.Ar(it.price) + " قرش" + (it.once ? " • مرة وحدة" : "") + (it.id == "trough" ? " • يسرّع الحليب" : ""), 21, NaaqaUI.Muted, false, ref y, 30);
            RTLTextMeshPro bt;
            bool locked = lv < it.level;
            Button b = NaaqaWindow.BottomButton(tile.transform, locked ? "مستوى " + NaaqaUI.Ar(it.level) : "اشترِ", locked ? NaaqaUI.ButtonStyle.Secondary : NaaqaUI.ButtonStyle.Primary, out bt, 54);
            if (locked)
            {
                Image li = NaaqaUI.IconImg(tile.transform, "lock", 50);
                NaaqaUI.Place(li.rectTransform, new Vector2(0, 1), new Vector2(50, 50), new Vector2(10, -10));
                b.onClick.AddListener(() => SetStatus("يفتح في المستوى " + NaaqaUI.Ar(item.level), NaaqaUI.ErrorColor));
            }
            else
            {
                b.interactable = money >= it.price;
                b.onClick.AddListener(() => Buy(item));
            }
            shopCells.Add(tile.gameObject);
        }
    }

    void Buy(ShopItem it)
    {
        string err = ShopItemView.Purchase(it.id, it.name, it.price, it.once);
        if (err != null) { SetStatus(err, NaaqaUI.ErrorColor); return; }
        if (it.id == "trough")
        {
            // الحوض يتركب على الحوش لحاله (مو في الشنطة)
            if (InventoryManager.Instance != null) InventoryManager.Instance.Remove("trough");
            if (FarmStore.Instance != null) FarmStore.Instance.SetTrough(true);
            SetStatus("انركب حوض الماء في حوشك! إبلك بتنتج أسرع", NaaqaUI.OkColor);
            if (seller != null) seller.Say("مبروك، الماء بركة", 3f);
            RebuildShop();
            return;
        }
        lastBought = it.id;
        SetStatus("انضاف " + it.name + " لشنطتك", NaaqaUI.OkColor);
        if (seller != null) seller.Say("مبروك عليك، الله يهنيك فيه", 3f);
        RebuildShop();   // (زر «ركّبه الحين» انشال: الغرض ينحط في الشنطة بس)
    }

    void PlaceNow()
    {
        if (string.IsNullOrEmpty(lastBought)) return;
        string id = lastBought;
        lastBought = null;
        placeNowBtn.SetActive(false);
        Close();
        if (PlacementManager.Instance != null && InventoryManager.Instance != null && InventoryManager.Instance.GetCount(id) > 0)
            PlacementManager.Instance.StartPlacing(id);
    }


    // ===== الناس =====
    Rect Plaza { get { return new Rect(0.1f, 0.12f, 0.42f, 0.42f); } }   // الساحة يسار اللوحة

    Vector2 ScenePoint(float fx, float fy)
    {
        Rect r = scene.rect;
        return new Vector2(r.xMin + r.width * fx, r.yMin + r.height * fy);
    }

    void SpawnPeople()
    {
        foreach (NpcWalker w in walkers) if (w != null) Destroy(w.gameObject);
        walkers.Clear();
        if (seller != null) Destroy(seller.gameObject);

        string[] people = { "npc_man", "npc_bisht", "npc_woman", "npc_elder", "npc_boy", "npc_man", "npc_woman" };
        Rect pz = Plaza;
        for (int i = 0; i < people.Length; i++)
        {
            Vector2 pos = ScenePoint(pz.x + Random.value * pz.width, pz.y + Random.value * pz.height);
            float h = people[i] == "npc_boy" ? 130f : 175f;
            walkers.Add(NpcWalker.Spawn(scene, people[i], pos, h, NpcWalker.Place.Souq, pz));
        }

        // بياعك واقف عند بسطته
        seller = NpcWalker.Spawn(scene, "npc_seller", ScenePoint(0.47f, 0.1f), 210f, NpcWalker.Place.Souq, pz, true);
        seller.chatty = false;
        seller.Face(-1f);
    }

    // زبون يجي للبسطة ويسلّم ويمشي (حوار بسيط)
    void Visit()
    {
        string who = Shoppers[Random.Range(0, Shoppers.Length)];
        NpcWalker npc = NpcWalker.Spawn(scene, who, ScenePoint(Random.value < 0.5f ? 0.04f : 0.3f, 0.06f), who == "npc_boy" ? 130f : 180f, NpcWalker.Place.Souq, Plaza);
        npc.chatty = false;
        npc.GoTo(ScenePoint(0.39f, 0.16f) + new Vector2(Random.Range(-30f, 30f), 0f), () =>
        {
            npc.Face(1f);
            npc.Say(VisitorLines[Random.Range(0, VisitorLines.Length)], 3f);
            if (seller != null) seller.Say(SellerLines[Random.Range(0, SellerLines.Length)], 2.5f);
            StartCoroutine(LeaveLater(npc, 4f));
        });
        walkers.Add(npc);
    }

    IEnumerator LeaveLater(NpcWalker npc, float after)
    {
        yield return new WaitForSecondsRealtime(after);
        if (npc == null) yield break;
        npc.GoTo(ScenePoint(Random.value < 0.5f ? -0.05f : 0.2f, 0.02f), () => { if (npc != null) { walkers.Remove(npc); Destroy(npc.gameObject); } });
    }

    void SortPeople()
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform c in scene) if (c.GetComponent<NpcWalker>() != null) list.Add(c);
        list.Sort((a, b) => ((RectTransform)b).anchoredPosition.y.CompareTo(((RectTransform)a).anchoredPosition.y));
        for (int i = 0; i < list.Count; i++) list[i].SetSiblingIndex(1 + i);
    }

    // ===== التحديث =====
    void Open()
    {
        StopAllCoroutines();
        status.text = "";
        qty = 1;
        Refresh();
        int lvNow = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (workBtn != null) workBtn.SetActive(lvNow >= GameRules.WorkersUnlock);
        StartCoroutine(FadeIn());
        PlayAmbience();
    }

    IEnumerator Greet()
    {
        yield return null;            // ننتظر الخلفية تاخذ مقاسها
        SpawnPeople();
        nextVisitor = 4f;
        yield return new WaitForSecondsRealtime(0.6f);
        if (seller != null) seller.Say("حيّ الله من جانا! تفضّل", 3.5f);
    }

    void Update()
    {
        if (Time.frameCount % 10 == 0) Refresh();
        // النافذة المربعة على قد الشاشة
        float k = Mathf.Min(1f, (canvasRect.rect.height - 40f) / CardSize, (canvasRect.rect.width - 40f) / CardSize);
        card.localScale = Vector3.one * Mathf.Max(0.4f, k);
        // صوت السوق يتبع إعداد «صوت الصحراء»، وأقل من النص
        if (ambience != null && ambience.isPlaying) ambience.volume = Mathf.MoveTowards(ambience.volume, AmbienceVolume(), Time.unscaledDeltaTime * 0.5f);
    }

    void Refresh()
    {
        int have = Milk();
        qty = Mathf.Clamp(qty, 1, Mathf.Max(1, have));
        haveText.text = "عندك " + NaaqaUI.Ar(have) + " حليب";
        qtyText.text = NaaqaUI.Ar(qty);
        sellText.text = "بِع " + NaaqaUI.Ar(qty) + " حليب • " + NaaqaUI.Ar(qty * MilkPrice) + " قرش";
        sellAllText.text = have > 0 ? "بِع الكل • " + NaaqaUI.Ar(have * MilkPrice) + " قرش" : "ما عندك حليب";
        sellBtn.interactable = have > 0;
        sellAllBtn.interactable = have > 0;
    }

    void SetStatus(string msg, Color c) { status.color = c; status.text = msg; }

    IEnumerator FadeIn()
    {
        group.alpha = 0f; group.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            group.alpha = t;
            yield return null;
        }
        group.alpha = 1f;
    }

    // ===== أجواء السوق =====
    // الملف: Assets/Resources/NaaqaSounds/souq_ambience (wav أو mp3 أو ogg): ناس يتكلمون وأصوات سوق
    void PlayAmbience()
    {
        if (ambience == null)
        {
            AudioClip clip = Resources.Load<AudioClip>("NaaqaSounds/souq_ambience");
            if (clip == null) return;
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = clip;
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0f;
        }
        ambience.volume = 0f;
        ambience.Play();
    }

    static float AmbienceVolume()
    {
        float setting = SoundManager.Instance != null ? Mathf.Clamp01(SoundManager.Instance.ambientVolume / 0.35f) : 1f;
        return 0.3f * setting;   // أقل من النص
    }

    public void Close()
    {
        if (ambience != null) ambience.Stop();
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
