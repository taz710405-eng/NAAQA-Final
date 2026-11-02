using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ===== سوق الإبل =====
// يفتح من السوق الشعبي: كروت لكل سلالة (صورتها، لونها، فايدتها، نجوم الحليب والسباق والمزاين، وسعرها)
// واللي تنشري تنضاف للحوش على طول.
public class CamelMarketScreen : MonoBehaviour
{
    public static CamelMarketScreen Instance;

    Canvas canvas;
    CanvasGroup group;
    RectTransform canvasRect, card;
    RTLTextMeshPro status, countText;
    readonly List<Button> buyButtons = new List<Button>();
    readonly List<CamelBreeds.Breed> breedsOrder = new List<CamelBreeds.Breed>();

    public static void Show()
    {
        if (Instance == null)
        {
            Instance = new GameObject("CamelMarketScreen").AddComponent<CamelMarketScreen>();
            Instance.Build();
        }
        Instance.gameObject.SetActive(true);
        Instance.Open();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("CamelMarketCanvas", 750);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        Image dim = NaaqaUI.Img(root, "Dim", null, new Color(0.04f, 0.02f, 0f, 0.7f));
        NaaqaUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        card = NaaqaUI.Rect("Card", root);
        NaaqaUI.Center(card, new Vector2(1640, 940), Vector2.zero);
        Image shadow = NaaqaUI.Img(card, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.6f), 36f);
        NaaqaUI.Stretch(shadow.rectTransform, -40, -40, -30, -50);
        Image bg = NaaqaUI.Img(card, "Bg", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 248), new Color32(22, 13, 8, 250), "marketBg"), Color.white, 36f);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Image border = NaaqaUI.Img(card, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.3f), 36f);
        NaaqaUI.Stretch(border.rectTransform);

        // العنوان
        RTLTextMeshPro title = NaaqaUI.Text(card, "Title", "سوق الإبل", 46, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(-236, 70);
        title.rectTransform.anchoredPosition = new Vector2(2, -26);

        countText = NaaqaUI.Text(card, "Count", "", 24, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        countText.rectTransform.anchorMin = new Vector2(0, 1); countText.rectTransform.anchorMax = new Vector2(1, 1);
        countText.rectTransform.pivot = new Vector2(0.5f, 1f);
        countText.rectTransform.sizeDelta = new Vector2(-236, 36);
        countText.rectTransform.anchoredPosition = new Vector2(2, -92);

        Button close = NaaqaUI.IconButton(card, "close", null, 64, Close);
        NaaqaUI.Place((RectTransform)close.transform, new Vector2(0, 1), new Vector2(64, 64), new Vector2(28, -26));

        Image tic = NaaqaUI.IconImg(card, "camel_market", 78);
        NaaqaUI.Place(tic.rectTransform, new Vector2(1, 1), new Vector2(78, 78), new Vector2(-26, -22));

        // قائمة تنسحب
        RectTransform view = NaaqaUI.Rect("Viewport", card);
        NaaqaUI.Stretch(view, 30, 30, 140, 70);
        view.gameObject.AddComponent<RectMask2D>();
        Image hit = view.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        RectTransform content = NaaqaUI.Rect("Content", view);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(420, 470);
        grid.spacing = new Vector2(16, 16);
        grid.startCorner = GridLayoutGroup.Corner.UpperRight;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = view.gameObject.AddComponent<ScrollRect>();
        sr.viewport = view;
        sr.content = content;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 30f;

        foreach (string id in CamelBreeds.Market) BreedCard(content, CamelBreeds.Get(id));

        status = NaaqaUI.Text(card, "Status", "", 24, NaaqaUI.Muted, TextAlignmentOptions.Center);
        status.rectTransform.anchorMin = new Vector2(0, 0); status.rectTransform.anchorMax = new Vector2(1, 0);
        status.rectTransform.pivot = new Vector2(0.5f, 0f);
        status.rectTransform.sizeDelta = new Vector2(-60, 50);
        status.rectTransform.anchoredPosition = new Vector2(0, 14);
    }

    void BreedCard(Transform parent, CamelBreeds.Breed b)
    {
        Image tile = NaaqaUI.Img(parent, "Breed_" + b.id, NaaqaUI.Rounded(), new Color(1, 1, 1, 0.06f), 22f);

        // الصورة
        Texture2D tex = Resources.Load<Texture2D>("NaaqaCamels/camel_" + b.id);
        RectTransform picArea = NaaqaUI.Rect("Pic", tile.transform);
        picArea.anchorMin = new Vector2(0, 1); picArea.anchorMax = new Vector2(1, 1);
        picArea.pivot = new Vector2(0.5f, 1f);
        picArea.sizeDelta = new Vector2(-12, 230);
        picArea.anchoredPosition = new Vector2(0, -4);
        if (tex != null)
        {
            RawImage pic = picArea.gameObject.AddComponent<RawImage>();
            pic.texture = tex;
            // نقص الفراغ حول الناقة في الصورة
            pic.uvRect = new Rect(0f, 0.03f, 1f, 0.87f);
            pic.raycastTarget = false;
        }

        // الندرة (شارة ملونة فوق الصورة)
        Image rb = NaaqaUI.Img(tile.transform, "Rarity", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 14f);
        NaaqaUI.Place(rb.rectTransform, new Vector2(1, 1), new Vector2(130, 40), new Vector2(-12, -12));
        RTLTextMeshPro rt = NaaqaUI.Text(rb.transform, "T", b.RarityName, 20, b.RarityColor, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(rt.rectTransform, 4, 4, 0, 2);

        float y = -238;
        Line(tile.transform, b.name, 30, NaaqaUI.Cream, true, ref y, 40);
        Line(tile.transform, b.color, 20, NaaqaUI.Muted, false, ref y, 28);
        Line(tile.transform, b.use, 20, new Color(1f, 0.9f, 0.75f, 0.9f), false, ref y, 52);

        // النجوم
        int milk = b.MilkStars;
        int race = b.RaceStars;
        Line(tile.transform, "حليب " + Stars(milk) + "  •  " + (b.RacingEligible ? "سباق " + Stars(race) : "ما تدخل السباق") + "  •  مزاين " + Stars(b.beauty), 18, NaaqaUI.OrangeLight, false, ref y, 30);

        RTLTextMeshPro bl;
        Button buy = NaaqaUI.MakeButton(tile.transform, "Buy", "اشترِ • " + NaaqaUI.Ar(b.price), NaaqaUI.ButtonStyle.Primary, 24, out bl);
        RectTransform brt = (RectTransform)buy.transform;
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(-28, 56);
        brt.anchoredPosition = new Vector2(0, 14);
        string id = b.id;
        buy.onClick.AddListener(() => OnBuy(id));
        buyButtons.Add(buy);
        breedsOrder.Add(b);
    }

    void Line(Transform parent, string text, float size, Color c, bool bold, ref float y, float h)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Line", text, size, c, TextAlignmentOptions.Center, bold);
        t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        t.rectTransform.sizeDelta = new Vector2(-20, h);
        t.rectTransform.anchoredPosition = new Vector2(0, y);
        y -= h;
    }

    static string Stars(int n)
    {
        return NaaqaUI.Ar(n) + "/" + NaaqaUI.Ar(3);
    }

    void OnBuy(string id)
    {
        string err = CamelBreeds.Buy(id);
        CamelBreeds.Breed b = CamelBreeds.Get(id);
        if (err != null) { status.color = NaaqaUI.ErrorColor; status.text = err; return; }
        status.color = NaaqaUI.OkColor;
        status.text = "مبروك! انضافت " + b.name + " لحوشك";
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("ناقة جديدة في الحوش: " + b.name);
        Refresh();
    }

    void Open()
    {
        status.text = "";
        Refresh();
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    void Refresh()
    {
        int n = CamelBreeds.CamelCount();
        countText.text = "في حوشك " + NaaqaUI.Ar(n) + " من " + NaaqaUI.Ar(CamelBreeds.Capacity) + " إبل  •  النادرة والأسطورية غالية وتفتح بمستوى أعلى";
        GameManager gm = GameManager.Instance;
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        for (int i = 0; i < buyButtons.Count; i++)
        {
            CamelBreeds.Breed b = breedsOrder[i];
            bool open = lv >= b.unlockLevel;
            buyButtons[i].interactable = open && n < CamelBreeds.Capacity && gm != null && gm.money >= b.price;
            RTLTextMeshPro bt = buyButtons[i].GetComponentInChildren<RTLTextMeshPro>();
            string want = open ? "اشترِ • " + NaaqaUI.Ar(b.price) : "تفتح بمستوى " + NaaqaUI.Ar(b.unlockLevel);
            if (bt != null && bt.text != want) bt.text = want;
        }
    }

    void Update()
    {
        if (Time.frameCount % 15 == 0) Refresh();
        float fit = Mathf.Min(1f, (canvasRect.rect.width - 40f) / 1640f, (canvasRect.rect.height - 40f) / 940f);
        card.localScale = Vector3.one * fit;
    }

    IEnumerator FadeIn()
    {
        group.alpha = 0f; group.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f) { group.alpha = t; yield return null; }
        group.alpha = 1f;
    }

    public void Close()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
