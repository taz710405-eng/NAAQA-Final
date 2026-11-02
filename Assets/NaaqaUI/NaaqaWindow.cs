using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== نافذة بنفس شكل «سوق الإبل» =====
// كل النوافذ الجديدة (البريد، الإعدادات، الشنطة، الذهب، العلف، تطوير الحوش، الطلبات...)
// تنبني من هنا عشان يكون الشكل موحّد.
public class NaaqaWindow : MonoBehaviour
{
    public Canvas canvas;
    public RectTransform card;      // البطاقة كلها
    public RectTransform body;      // المساحة اللي تحت العنوان
    public RTLTextMeshPro title, subtitle, status;
    public System.Action onClose;
    public System.Action onTick;    // يتنادى كل فريم والنافذة مفتوحة

    CanvasGroup group;
    RectTransform canvasRect;
    Vector2 size;

    public static NaaqaWindow Create(string name, string titleText, string icon, Vector2 size, int order = 760)
    {
        NaaqaWindow w = new GameObject(name).AddComponent<NaaqaWindow>();
        w.Build(titleText, icon, size, order);
        return w;
    }

    void Build(string titleText, string icon, Vector2 sz, int order)
    {
        size = sz;
        canvas = NaaqaUI.MakeCanvas(name + "Canvas", order);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasRect = (RectTransform)canvas.transform;
        Transform root = canvas.transform;

        Image dim = NaaqaUI.Img(root, "Dim", null, new Color(0.04f, 0.02f, 0f, 0.7f));
        NaaqaUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(Close);

        card = NaaqaUI.Rect("Card", root);
        NaaqaUI.Center(card, size, Vector2.zero);
        Image shadow = NaaqaUI.Img(card, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.6f), 36f);
        NaaqaUI.Stretch(shadow.rectTransform, -40, -40, -30, -50);
        Image bg = NaaqaUI.Img(card, "Bg", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 248), new Color32(22, 13, 8, 250), "marketBg"), Color.white, 36f);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Image border = NaaqaUI.Img(card, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.3f), 36f);
        NaaqaUI.Stretch(border.rectTransform);

        // الأيقونة والعنوان (يمين)
        float titleRight = 30f;
        if (!string.IsNullOrEmpty(icon))
        {
            Image ic = NaaqaUI.IconImg(card, icon, 78);
            NaaqaUI.Place(ic.rectTransform, new Vector2(1, 1), new Vector2(78, 78), new Vector2(-26, -22));
            titleRight = 116f;
        }
        title = NaaqaUI.Text(card, "Title", titleText, 44, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(120, -78);
        title.rectTransform.offsetMax = new Vector2(-titleRight, -24);

        subtitle = NaaqaUI.Text(card, "Sub", "", 24, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        subtitle.rectTransform.anchorMin = new Vector2(0, 1); subtitle.rectTransform.anchorMax = new Vector2(1, 1);
        subtitle.rectTransform.pivot = new Vector2(1f, 1f);
        subtitle.rectTransform.offsetMin = new Vector2(120, -120);
        subtitle.rectTransform.offsetMax = new Vector2(-titleRight, -80);

        // زر الإغلاق (يسار)
        Button close = NaaqaUI.IconButton(card, "close", null, 64, Close);
        NaaqaUI.Place((RectTransform)close.transform, new Vector2(0, 1), new Vector2(64, 64), new Vector2(28, -26));

        body = NaaqaUI.Rect("Body", card);
        NaaqaUI.Stretch(body, 30, 30, 136, 66);

        status = NaaqaUI.Text(card, "Status", "", 24, NaaqaUI.Muted, TextAlignmentOptions.Center);
        status.rectTransform.anchorMin = new Vector2(0, 0); status.rectTransform.anchorMax = new Vector2(1, 0);
        status.rectTransform.pivot = new Vector2(0.5f, 0f);
        status.rectTransform.sizeDelta = new Vector2(-60, 50);
        status.rectTransform.anchoredPosition = new Vector2(0, 12);

        gameObject.SetActive(false);
    }

    // بدون عنوان فرعي: الجسم يطلع لفوق شوي
    public void NoSubtitle()
    {
        subtitle.gameObject.SetActive(false);
        body.offsetMax = new Vector2(body.offsetMax.x, -104);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        status.text = "";
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Close()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        if (onClose != null) onClose();
    }

    public bool IsOpen { get { return gameObject.activeSelf; } }

    public void SetStatus(string msg, bool ok)
    {
        status.color = ok ? NaaqaUI.OkColor : NaaqaUI.ErrorColor;
        status.text = msg;
    }

    public void Info(string msg)
    {
        status.color = NaaqaUI.Muted;
        status.text = msg;
    }

    void Update()
    {
        float fit = Mathf.Min(1f, (canvasRect.rect.width - 40f) / size.x, (canvasRect.rect.height - 40f) / size.y);
        card.localScale = Vector3.one * fit;
        if (onTick != null) onTick();
    }

    IEnumerator FadeIn()
    {
        group.alpha = 0f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.2f)
        {
            group.alpha = t;
            yield return null;
        }
        group.alpha = 1f;
    }

    // ===== أدوات المحتوى =====

    // شبكة كروت تنسحب لتحت
    public RectTransform Grid(RectTransform parent, Vector2 cell, int columns, float spacing = 16f)
    {
        RectTransform content = Scroll(parent);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cell;
        grid.spacing = new Vector2(spacing, spacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperRight;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        return content;
    }

    // قائمة صفوف تنسحب لتحت
    public RectTransform List(RectTransform parent, float spacing = 12f)
    {
        RectTransform content = Scroll(parent);
        VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = new RectOffset(4, 4, 4, 4);
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        return content;
    }

    static RectTransform Scroll(RectTransform parent)
    {
        RectTransform view = NaaqaUI.Rect("Viewport", parent);
        NaaqaUI.Stretch(view);
        view.gameObject.AddComponent<RectMask2D>();
        Image hit = view.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);

        RectTransform content = NaaqaUI.Rect("Content", view);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = view.gameObject.AddComponent<ScrollRect>();
        sr.viewport = view;
        sr.content = content;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 30f;
        return content;
    }

    // سطر نص داخل كرت (من فوق لتحت)
    public static RTLTextMeshPro Line(Transform parent, string text, float size, Color c, bool bold, ref float y, float h)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Line", text, size, c, TextAlignmentOptions.Center, bold);
        t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        t.rectTransform.sizeDelta = new Vector2(-20, h);
        t.rectTransform.anchoredPosition = new Vector2(0, y);
        y -= h;
        return t;
    }

    // زر تحت الكرت
    public static Button BottomButton(Transform parent, string label, NaaqaUI.ButtonStyle style, out RTLTextMeshPro text, float height = 56f)
    {
        Button b = NaaqaUI.MakeButton(parent, "Btn", label, style, 24, out text);
        RectTransform brt = (RectTransform)b.transform;
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(-28, height);
        brt.anchoredPosition = new Vector2(0, 14);
        return b;
    }

    // صف أفقي: أيقونة يمين، نصين بالنص، وزر يسار
    public static Image Row(Transform parent, string icon, Sprite picture, float height, out RTLTextMeshPro main, out RTLTextMeshPro sub, out Button btn, out RTLTextMeshPro btnText, string btnLabel, float btnWidth = 190f)
    {
        Image row = NaaqaUI.Tile(parent, "Row");
        NaaqaUI.Height(row, height);

        Image ic;
        if (picture != null) { ic = NaaqaUI.Img(row.transform, "Pic", picture, Color.white); ic.preserveAspect = true; }
        else ic = NaaqaUI.IconImg(row.transform, icon, height - 18);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(height - 18, height - 18), new Vector2(-10, 0));

        float textRight = height + 4;
        float textLeft = btnLabel != null ? btnWidth + 26 : 20;
        main = NaaqaUI.Text(row.transform, "Main", "", 27, NaaqaUI.Cream, TextAlignmentOptions.BottomRight, true);
        main.rectTransform.anchorMin = new Vector2(0, 0.5f); main.rectTransform.anchorMax = new Vector2(1, 1);
        main.rectTransform.offsetMin = new Vector2(textLeft, 0); main.rectTransform.offsetMax = new Vector2(-textRight, -8);
        sub = NaaqaUI.Text(row.transform, "Sub", "", 21, NaaqaUI.Muted, TextAlignmentOptions.TopRight);
        sub.rectTransform.anchorMin = new Vector2(0, 0); sub.rectTransform.anchorMax = new Vector2(1, 0.5f);
        sub.rectTransform.offsetMin = new Vector2(textLeft, 6); sub.rectTransform.offsetMax = new Vector2(-textRight, -2);

        btn = null; btnText = null;
        if (btnLabel != null)
        {
            btn = NaaqaUI.MakeButton(row.transform, "Btn", btnLabel, NaaqaUI.ButtonStyle.Primary, 23, out btnText);
            NaaqaUI.Place((RectTransform)btn.transform, new Vector2(0, 0.5f), new Vector2(btnWidth, Mathf.Min(62, height - 22)), new Vector2(14, 0));
        }
        return row;
    }

    // عنوان قسم داخل القائمة
    public static RTLTextMeshPro Header(Transform parent, string text)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Header", text, 28, NaaqaUI.OrangeLight, TextAlignmentOptions.BottomRight, true);
        NaaqaUI.Height(t, 46);
        return t;
    }
}
