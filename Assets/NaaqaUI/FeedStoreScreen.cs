using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== شراء العلف =====
// باقات علف (كل ما كبرت الباقة رخص سعر الحبة)، وأحيانًا يطلع خصم على باقة،
// وتقدر تختار الكمية بنفسك.
public class FeedStoreScreen : MonoBehaviour
{
    static FeedStoreScreen instance;
    NaaqaWindow win;

    public const int UnitPrice = 2;   // سعر الحبة لو اخترت الكمية بنفسك (الباقات أرخص)

    struct Pack { public int amount, price; public Pack(int a, int p) { amount = a; price = p; } }
    // ١٠ بـ١٥، ٢٥ بـ٣٥، ٥٠ بـ٦٥ (GameRules)
    static readonly Pack[] Packs =
    {
        new Pack(GameRules.FeedPackAmount[0], GameRules.FeedPackPrice[0]),
        new Pack(GameRules.FeedPackAmount[1], GameRules.FeedPackPrice[1]),
        new Pack(GameRules.FeedPackAmount[2], GameRules.FeedPackPrice[2]),
    };

    class PackView { public RTLTextMeshPro price, old; public GameObject badge; public RTLTextMeshPro badgeText; public Button btn; public RTLTextMeshPro btnText; }
    readonly List<PackView> views = new List<PackView>();
    int qty = 10;
    RTLTextMeshPro qtyText, qtyPrice;
    Button qtyBuy;

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("FeedStoreScreen").AddComponent<FeedStoreScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    // ===== الخصم: يتغير كل ١٠ دقايق، ومرات ما فيه خصم =====
    static int DealIndex(out int percent)
    {
        long slot = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 600;
        System.Random r = new System.Random((int)(slot % 100000) * 7919);
        if (r.NextDouble() < 1.0) { percent = 0; return -1; }   // (الأسعار ثابتة: ما فيه خصومات)
        percent = 15 + r.Next(0, 5) * 5;    // ١٥٪ لين ٣٥٪
        return r.Next(0, Packs.Length);
    }

    static int PriceOf(int i)
    {
        int pct;
        int deal = DealIndex(out pct);
        int p = Packs[i].price;
        return deal == i ? Mathf.RoundToInt(p * (100 - pct) / 100f) : p;
    }

    void Build()
    {
        win = NaaqaWindow.Create("FeedStore", "العلف", "feed", new Vector2(1480, 820));
        win.transform.SetParent(transform, false);
        win.onTick = Tick;

        RectTransform grid = NaaqaUI.Rect("Packs", win.body);
        NaaqaUI.Stretch(grid, 0, 0, 0, 200);
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(330, 400);
        g.spacing = new Vector2(18, 18);
        g.startCorner = GridLayoutGroup.Corner.UpperRight;
        g.childAlignment = TextAnchor.UpperCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;

        for (int i = 0; i < Packs.Length; i++)
        {
            int idx = i;
            PackView v = new PackView();
            Image tile = NaaqaUI.Tile(grid, "Pack" + i);
            Image ic = NaaqaUI.IconImg(tile.transform, "feed", 130 + i * 6);
            NaaqaUI.Place(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(130, 130), new Vector2(0, -18));
            float y = -160;
            NaaqaWindow.Line(tile.transform, NaaqaUI.Ar(Packs[i].amount) + " علف", 36, NaaqaUI.Cream, true, ref y, 52);
            v.old = NaaqaWindow.Line(tile.transform, "", 22, new Color(1, 1, 1, 0.45f), false, ref y, 30);
            v.price = NaaqaWindow.Line(tile.transform, "", 24, NaaqaUI.Muted, false, ref y, 34);

            Image badge = NaaqaUI.Img(tile.transform, "Deal", NaaqaUI.Rounded(), new Color32(214, 60, 40, 255), 18f);
            NaaqaUI.Place(badge.rectTransform, new Vector2(0, 1), new Vector2(130, 46), new Vector2(12, -12));
            v.badgeText = NaaqaUI.Text(badge.transform, "T", "", 22, Color.white, TextAlignmentOptions.Center, true);
            NaaqaUI.Stretch(v.badgeText.rectTransform, 4, 4, 0, 2);
            v.badge = badge.gameObject;

            v.btn = NaaqaWindow.BottomButton(tile.transform, "", NaaqaUI.ButtonStyle.Primary, out v.btnText, 62);
            v.btn.onClick.AddListener(() => Buy(Packs[idx].amount, PriceOf(idx)));
            views.Add(v);
        }

        // اختيار الكمية
        Image row = NaaqaUI.Tile(win.body, "Custom");
        row.rectTransform.anchorMin = new Vector2(0, 0); row.rectTransform.anchorMax = new Vector2(1, 0);
        row.rectTransform.pivot = new Vector2(0.5f, 0f);
        row.rectTransform.sizeDelta = new Vector2(0, 170);
        row.rectTransform.anchoredPosition = new Vector2(0, 6);

        RTLTextMeshPro lbl = NaaqaUI.Text(row.transform, "L", "اختر الكمية بنفسك", 28, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(lbl.rectTransform, new Vector2(1, 0.5f), new Vector2(380, 60), new Vector2(-30, 26));
        RTLTextMeshPro lbl2 = NaaqaUI.Text(row.transform, "L2", "الحبة بـ " + NaaqaUI.Ar(UnitPrice) + " قرش", 22, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        NaaqaUI.Place(lbl2.rectTransform, new Vector2(1, 0.5f), new Vector2(380, 40), new Vector2(-30, -26));

        RTLTextMeshPro t;
        Button minus = NaaqaUI.MakeButton(row.transform, "Minus", "", NaaqaUI.ButtonStyle.Secondary, 40, out t);
        NaaqaUI.PlusSign(minus.transform, 30f, true, NaaqaUI.Cream);
        NaaqaUI.Place((RectTransform)minus.transform, new Vector2(0.5f, 0.5f), new Vector2(84, 84), new Vector2(-150, 0));
        minus.onClick.AddListener(() => Step(-5));
        Image qbg = NaaqaUI.Img(row.transform, "Qty", NaaqaUI.Rounded(), new Color(0, 0, 0, 0.3f), 18f);
        NaaqaUI.Place(qbg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170, 84), new Vector2(-20, 0));
        qtyText = NaaqaUI.Text(qbg.transform, "N", "", 38, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(qtyText.rectTransform, 0, 0, 0, 4);
        Button plus = NaaqaUI.MakeButton(row.transform, "Plus", "", NaaqaUI.ButtonStyle.Secondary, 40, out t);
        NaaqaUI.PlusSign(plus.transform, 30f, false, NaaqaUI.Cream);
        NaaqaUI.Place((RectTransform)plus.transform, new Vector2(0.5f, 0.5f), new Vector2(84, 84), new Vector2(110, 0));
        plus.onClick.AddListener(() => Step(5));

        qtyBuy = NaaqaUI.MakeButton(row.transform, "Buy", "", NaaqaUI.ButtonStyle.Primary, 26, out qtyPrice);
        NaaqaUI.Place((RectTransform)qtyBuy.transform, new Vector2(0, 0.5f), new Vector2(300, 84), new Vector2(30, 0));
        qtyBuy.onClick.AddListener(() => Buy(qty, qty * UnitPrice));
    }

    void Step(int d)
    {
        qty = Mathf.Clamp(qty + d, 5, 200);
        Refresh();
    }

    void Buy(int amount, int price)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (!gm.SpendMoney(price))
        {
            win.SetStatus("القروش ما تكفي! تحتاج " + NaaqaUI.Ar(price) + " قرش", false);
            return;
        }
        gm.AddFeed(amount);
        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        win.SetStatus("+" + NaaqaUI.Ar(amount) + " علف (−" + NaaqaUI.Ar(price) + " قرش)", true);
        Refresh();
    }

    void Refresh()
    {
        GameManager gm = GameManager.Instance;
        int money = gm != null ? gm.money : 0;
        int feed = gm != null ? gm.feedCount : 0;
        int pct;
        int deal = DealIndex(out pct);
        win.subtitle.text = "عندك " + NaaqaUI.Ar(feed) + " علف  •  " + NaaqaUI.Ar(money) + " قرش" +
            (deal >= 0 ? "  •  عرض اليوم: خصم " + NaaqaUI.Ar(pct) + "٪!" : "");

        for (int i = 0; i < views.Count; i++)
        {
            PackView v = views[i];
            int price = PriceOf(i);
            bool isDeal = deal == i;
            v.badge.SetActive(isDeal);
            if (isDeal) v.badgeText.text = "خصم " + NaaqaUI.Ar(pct) + "٪";
            v.old.text = isDeal ? "بدل " + NaaqaUI.Ar(Packs[i].price) : "";
            float each = price / (float)Packs[i].amount;
            v.price.text = "الحبة بـ " + (each < 3.95f ? NaaqaUI.Ar(Mathf.RoundToInt(each * 10f) / 10) + "٫" + NaaqaUI.Ar(Mathf.RoundToInt(each * 10f) % 10) : NaaqaUI.Ar(Mathf.RoundToInt(each))) + " قرش";
            v.btnText.text = "اشترِ • " + NaaqaUI.Ar(price);
            v.btn.interactable = money >= price;
        }

        qtyText.text = NaaqaUI.Ar(qty);
        qtyPrice.text = "اشترِ • " + NaaqaUI.Ar(qty * UnitPrice) + " قرش";
        qtyBuy.interactable = money >= qty * UnitPrice;
    }

    float tick;
    void Tick()
    {
        tick += Time.unscaledDeltaTime;
        if (tick < 0.5f) return;
        tick = 0f;
        Refresh();
    }
}
