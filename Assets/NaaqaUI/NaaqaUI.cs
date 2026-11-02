using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;
using System.Collections.Generic;

// ===== أدوات بناء الواجهات بالكود =====
// الشاشات الجديدة (تسجيل الدخول، ملف اللاعب، شاشة البداية) تنبني من هنا،
// عشان ما نعدّل main.unity ويكون الشكل موحّد مثل CSS.
public static class NaaqaUI
{
    // ===== الألوان =====
    public static readonly Color Orange = new Color32(247, 148, 29, 255);
    public static readonly Color OrangeLight = new Color32(255, 178, 64, 255);
    public static readonly Color OrangeDark = new Color32(214, 94, 17, 255);
    public static readonly Color Cream = new Color32(255, 246, 232, 255);
    public static readonly Color Muted = new Color32(232, 208, 176, 190);
    public static readonly Color CardBg = new Color32(22, 14, 8, 222);
    public static readonly Color FieldBg = new Color32(255, 255, 255, 20);
    public static readonly Color FieldLine = new Color32(255, 214, 160, 46);
    public static readonly Color ErrorColor = new Color32(255, 128, 104, 255);
    public static readonly Color OkColor = new Color32(146, 226, 140, 255);

    // ===== الخط =====
    static TMP_FontAsset font;

    // الحروف العربية بالشكل المتصل (ألف، ميم، باء) اللي يستخدمها RTLTMPro
    static bool HasArabic(TMP_FontAsset f)
    {
        string probe = "\uFE8E\uFEE4\uFE91\u0627";
        foreach (char ch in probe)
            if (!f.HasCharacter(ch, true, true)) return false;
        return true;
    }

    public static TMP_FontAsset Font
    {
        get
        {
            // النسخة الواضحة من الخط (FontUpgrade) لو انبنت
            TMP_FontAsset sharp = FontUpgrade.HQ;
            if (sharp != null) return sharp;
            if (font != null) return font;
            // ناخذ خط اللعبة العربي من نصوص المشهد، بس لازم يكون فيه الحروف العربية فعلاً
            // (قبل كنا ناخذ أول خط نلقاه، وأحيانًا يطلع خط بدون عربي فتصير الحروف مربعات)
            TMP_FontAsset any = null;
            foreach (RTLTextMeshPro t in Object.FindObjectsByType<RTLTextMeshPro>(FindObjectsInactive.Include))
            {
                if (t.font == null) continue;
                if (any == null) any = t.font;
                if (HasArabic(t.font)) { font = t.font; break; }
            }
            if (font == null)
                foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                    if (f != null && HasArabic(f)) { font = f; break; }
            if (font == null) font = any != null ? any : TMP_Settings.defaultFontAsset;
            return font;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        font = null;
        sprites.Clear();
    }

    // ===== أشكال (تنرسم مرة وحدة وتنحفظ) =====
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    const int R = 32;   // نصف قطر الزوايا في الصورة نفسها

    // مستطيل زواياه دائرية (يتمدد بدون ما تخرب الزوايا)
    public static Sprite Rounded()
    {
        return RoundedGradient(Color.white, Color.white, "round");
    }

    public static Sprite RoundedGradient(Color top, Color bottom, string key = null)
    {
        key = key ?? ("grad" + top + bottom);
        Sprite s;
        if (sprites.TryGetValue(key, out s) && s != null) return s;

        int n = R * 2 + 4;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = n / 2f;
        for (int y = 0; y < n; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(n - 1));
            for (int x = 0; x < n; x++)
            {
                float d = RoundedDistance(x + 0.5f - half, y + 0.5f - half, half, R);
                c.a = Mathf.Clamp01(0.5f - d) * Mathf.Lerp(bottom.a, top.a, y / (float)(n - 1));
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(R + 1, R + 1, R + 1, R + 1));
        sprites[key] = s;
        return s;
    }

    // إطار فقط (بدون تعبئة)
    public static Sprite RoundedOutline()
    {
        Sprite s;
        if (sprites.TryGetValue("outline", out s) && s != null) return s;

        int n = R * 2 + 4;
        float t = 3f;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = RoundedDistance(x + 0.5f - half, y + 0.5f - half, half, R);
                float a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + t));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
            }
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(R + 1, R + 1, R + 1, R + 1));
        sprites["outline"] = s;
        return s;
    }

    // ظل ناعم تحت البطاقات
    public static Sprite SoftShadow()
    {
        Sprite s;
        if (sprites.TryGetValue("shadow", out s) && s != null) return s;

        int blur = 28;
        int n = (R + blur) * 2 + 4;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = RoundedDistance(x + 0.5f - half, y + 0.5f - half, half - blur, R);
                float a = 1f - Mathf.SmoothStep(0f, 1f, (d + blur * 0.3f) / blur);
                tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.Clamp01(a)));
            }
        tex.Apply();
        int b = R + blur + 1;
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        sprites["shadow"] = s;
        return s;
    }

    public static Sprite Circle()
    {
        Sprite s;
        if (sprites.TryGetValue("circle", out s) && s != null) return s;
        int n = 256;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) - (c - 1f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        sprites["circle"] = s;
        return s;
    }

    // تدرج من فوق لتحت (للتظليل فوق الخلفيات)
    public static Sprite VerticalFade(Color top, Color bottom, string key)
    {
        Sprite s;
        if (sprites.TryGetValue(key, out s) && s != null) return s;
        Texture2D tex = new Texture2D(4, 128, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 128; y++)
        {
            float t = y / 127f;
            t = t * t * (3f - 2f * t);
            Color c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f));
        sprites[key] = s;
        return s;
    }

    // تدرج من اليسار لليمين
    public static Sprite HorizontalFade(Color left, Color right, string key)
    {
        Sprite s;
        if (sprites.TryGetValue(key, out s) && s != null) return s;
        Texture2D tex = new Texture2D(128, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < 128; x++)
        {
            float t = x / 127f;
            t = t * t * (3f - 2f * t);
            Color c = Color.Lerp(left, right, t);
            for (int y = 0; y < 4; y++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, 128, 4), new Vector2(0.5f, 0.5f));
        sprites[key] = s;
        return s;
    }

    static float RoundedDistance(float px, float py, float half, float r)
    {
        float qx = Mathf.Abs(px) - (half - r);
        float qy = Mathf.Abs(py) - (half - r);
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    // صورة من Assets/Resources/NaaqaUI
    public static Sprite Load(string name)
    {
        string key = "res_" + name;
        Sprite s;
        if (sprites.TryGetValue(key, out s) && s != null) return s;

        Texture2D tex = Resources.Load<Texture2D>("NaaqaUI/" + name);
        if (tex != null)
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        else
            s = Resources.Load<Sprite>("NaaqaUI/" + name);

        if (s == null) Debug.LogWarning("ما لقيت الصورة: Resources/NaaqaUI/" + name);
        else sprites[key] = s;
        return s;
    }

    // صورة عنصر من Assets/Resources/NaaqaItems
    public static Sprite LoadItem(string name)
    {
        string key = "item_" + name;
        Sprite s;
        if (sprites.TryGetValue(key, out s) && s != null) return s;
        Texture2D tex = Resources.Load<Texture2D>("NaaqaItems/" + name);
        if (tex == null) return null;
        s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        sprites[key] = s;
        return s;
    }

    public static Texture2D LoadTexture(string name)
    {
        return Resources.Load<Texture2D>("NaaqaUI/" + name);
    }

    // ===== عناصر =====
    public static Canvas MakeCanvas(string name, int order)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = g.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;

        CanvasScaler sc = g.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        // Expand: الواجهة كلها (١٩٢٠×١٠٨٠) تدخل في الشاشة بأي مقاس، بدون قص (جوال عريض، آيباد، كمبيوتر)
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return c;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.layer = 5; // UI
        g.transform.SetParent(parent, false);
        return (RectTransform)g.transform;
    }

    // نص قديم من المشهد كان مصغّر (scale ٠٫٦) فيطلع صغير: نرجعه لحجمه، وياخذ عرض اللي فوقه ناقص pad
    public static void BigText(TMP_Text t, float pad, float min, float max)
    {
        if (t == null) return;
        RectTransform r = t.rectTransform;
        r.localScale = Vector3.one;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.sizeDelta = new Vector2(-pad, -12f);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMin = min;
        t.fontSizeMax = max;
        t.fontSize = max;
    }

    public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void Center(RectTransform rt, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, float radius = 0f)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
            float r = radius > 0 ? radius : R;
            img.pixelsPerUnitMultiplier = (sprite.border.x - 1f) / r;
        }
        return img;
    }

    // نص عربي (يتصل ويتعدل اتجاهه)
    public static RTLTextMeshPro Text(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Right, bool bold = false)
    {
        RectTransform rt = Rect(name, parent);
        RTLTextMeshPro t = rt.gameObject.AddComponent<RTLTextMeshPro>();
        t.font = Font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        t.text = text ?? "";
        return t;
    }

    // نص إنجليزي/أرقام (مثل الإيميل)
    public static TextMeshProUGUI Latin(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = Rect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        t.text = text ?? "";
        return t;
    }

    public static LayoutElement Height(Component c, float h)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
        return le;
    }

    // ===== زر =====
    public enum ButtonStyle { Primary, Secondary, Ghost, Danger }

    public static Button MakeButton(Transform parent, string name, string label, ButtonStyle style, float fontSize, out RTLTextMeshPro text)
    {
        Sprite sprite;
        Color color;
        Color txt;
        switch (style)
        {
            case ButtonStyle.Primary:
                sprite = RoundedGradient(OrangeLight, OrangeDark, "btnPrimary"); color = Color.white; txt = new Color32(46, 22, 6, 255); break;
            case ButtonStyle.Danger:
                sprite = Rounded(); color = new Color(1f, 0.45f, 0.35f, 0.14f); txt = ErrorColor; break;
            case ButtonStyle.Ghost:
                sprite = null; color = new Color(0, 0, 0, 0); txt = OrangeLight; break;
            default:
                sprite = Rounded(); color = new Color(1f, 1f, 1f, 0.08f); txt = Cream; break;
        }

        Image bg = Img(parent, name, sprite, color, 22f);
        bg.raycastTarget = true;
        if (sprite == null) bg.color = new Color(0, 0, 0, 0.001f);

        Button b = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        // شكل السدو للأزرار الأساسية والثانوية (لو صوره موجودة)
        bool sadu = (style == ButtonStyle.Primary || style == ButtonStyle.Secondary)
                    && SaduFit.Apply(bg, style == ButtonStyle.Primary ? "primary" : "secondary", b);
        if (sadu)
        {
            txt = Cream;
            cb.disabledColor = Color.white;   // المقفل يصير رمادي من الصورة نفسها
            b.colors = cb;
        }

        text = Text(bg.transform, "Label", label, fontSize, txt, TextAlignmentOptions.Center, style == ButtonStyle.Primary || sadu);
        Stretch(text.rectTransform, 12, 12, 0, 4);
        if (sadu)
        {
            // الكلام بين شريطين السدو (ما يطلع عليهم)، ويصغر لحاله لو ما وسع
            text.rectTransform.anchorMin = new Vector2(0, 0.2f);
            text.rectTransform.anchorMax = new Vector2(1, 0.8f);
            text.rectTransform.offsetMin = new Vector2(16, 0);
            text.rectTransform.offsetMax = new Vector2(-16, 0);
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Min(14f, fontSize);
            text.lineSpacing = -12f;
        }

        bg.gameObject.AddComponent<NaaqaButtonFx>();
        b.onClick.AddListener(() => { if (SoundManager.Instance != null) SoundManager.Instance.PlayClick(); });
        return b;
    }

    // ===== خانة كتابة =====
    public static TMP_InputField Field(Transform parent, string name, string placeholder, bool arabic,
        TMP_InputField.ContentType type, float height = 74f)
    {
        Image bg = Img(parent, name, Rounded(), FieldBg, 18f);
        bg.raycastTarget = true;
        Height(bg, height);

        // خط خفيف حول الخانة
        Image line = Img(bg.transform, "Border", RoundedOutline(), FieldLine, 18f);
        Stretch(line.rectTransform);

        bool wasActive = bg.gameObject.activeSelf;
        bg.gameObject.SetActive(false);   // نجهّز كل شي قبل ما تشتغل الخانة

        TMP_InputField f = bg.gameObject.AddComponent<TMP_InputField>();

        RectTransform area = Rect("TextArea", bg.transform);
        Stretch(area, 22, 22, 6, 6);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI txt = Rect("Text", area).gameObject.AddComponent<TextMeshProUGUI>();
        Stretch(txt.rectTransform);
        txt.font = Font;
        txt.fontSize = 30;
        txt.color = Cream;
        txt.alignment = arabic ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.richText = false;

        RTLTextMeshPro ph = Rect("Placeholder", area).gameObject.AddComponent<RTLTextMeshPro>();
        Stretch(ph.rectTransform);
        ph.font = Font;
        ph.fontSize = 27;
        ph.color = new Color(Muted.r, Muted.g, Muted.b, 0.55f);
        ph.alignment = TextAlignmentOptions.MidlineRight;
        ph.textWrappingMode = TextWrappingModes.NoWrap;
        ph.raycastTarget = false;
        ph.text = placeholder;

        f.textViewport = area;
        f.textComponent = txt;
        f.placeholder = ph;
        f.fontAsset = Font;
        f.pointSize = 30;
        f.contentType = type;
        f.lineType = TMP_InputField.LineType.SingleLine;
        f.richText = false;
        f.customCaretColor = true;
        f.caretColor = OrangeLight;
        f.caretWidth = 3;
        f.selectionColor = new Color(Orange.r, Orange.g, Orange.b, 0.35f);
        f.targetGraphic = bg;

        // الاسم العربي: نعرضه موصول فوق الكتابة الأصلية
        if (arabic)
        {
            RTLTextMeshPro display = Rect("RtlDisplay", area).gameObject.AddComponent<RTLTextMeshPro>();
            Stretch(display.rectTransform);
            display.font = Font;
            display.fontSize = 30;
            display.color = Cream;
            display.alignment = TextAlignmentOptions.MidlineRight;
            display.textWrappingMode = TextWrappingModes.NoWrap;
            display.raycastTarget = false;
            RtlInputMirror mirror = bg.gameObject.AddComponent<RtlInputMirror>();
            mirror.display = display;
        }

        // يتلون الإطار لما تكون الخانة مختارة
        NaaqaFieldFx fx = bg.gameObject.AddComponent<NaaqaFieldFx>();
        fx.border = line;

        bg.gameObject.SetActive(wasActive);
        return f;
    }

    // ===== أيقونات موحّدة (دايرة غامقة شفافة وفيها رسمة كريمية) =====
    // الصور في Resources/NaaqaUI/ic_<name>.png
    public static Sprite Icon(string name)
    {
        return Load("ic_" + name);
    }

    public static Image IconImg(Transform parent, string icon, float size)
    {
        Image img = Img(parent, "Icon_" + icon, Icon(icon), Color.white);
        img.preserveAspect = true;
        img.rectTransform.sizeDelta = new Vector2(size, size);
        return img;
    }

    // زر أيقونة دايري (مع اسم تحته لو تبين)
    public static Button IconButton(Transform parent, string icon, string label, float size, UnityEngine.Events.UnityAction onClick)
    {
        Image img = IconImg(parent, icon, size);
        img.raycastTarget = true;
        img.gameObject.name = "Btn_" + icon;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        b.colors = cb;
        if (onClick != null) b.onClick.AddListener(onClick);
        b.onClick.AddListener(() => { if (SoundManager.Instance != null) SoundManager.Instance.PlayClick(); });
        img.gameObject.AddComponent<NaaqaButtonFx>();
        if (!string.IsNullOrEmpty(label)) Caption(img.transform, label, 17f);
        return b;
    }

    // اسم أبيض واضح تحت الأيقونة
    public static RTLTextMeshPro Caption(Transform icon, string label, float size)
    {
        // الاسم داخل شريط غامق صغير تحت الأيقونة. مقاسه ثابت (بدون Layout) عشان ما يكبر ويتلخبط
        Image pill = Img(icon, "Caption", Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.88f), 12f);
        RectTransform prt = pill.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0, 2);
        prt.sizeDelta = new Vector2(label.Length * size * 0.62f + 22f, size + 14f);

        RTLTextMeshPro t = Text(pill.transform, "Text", label, size, Cream, TextAlignmentOptions.Center, true);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        Stretch(t.rectTransform, 4, 4, 0, 2);
        pill.gameObject.AddComponent<CaptionFit>().text = t;
        return t;
    }

    // مربع فاتح داخل النوافذ (نفس كروت سوق الإبل)
    public static Image Tile(Transform parent, string name, float alpha = 0.06f)
    {
        return Img(parent, name, Rounded(), new Color(1, 1, 1, alpha), 22f);
    }

    // يربط عنصر بحافة أبوه: anchor = نقطة الربط (٠..١)، pos = الإزاحة
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    // ===== فقاعة كلام موحّدة: خلفية سوداء شفافة، حد رملي ذهبي، وخط كريمي واضح =====
    public static readonly Color BubbleBg = new Color(0.06f, 0.04f, 0.02f, 0.9f);
    public static readonly Color BubbleLine = new Color32(232, 196, 128, 255);

    public static Image SpeechBubble(Transform parent, string name, float fontSize, out RTLTextMeshPro text)
    {
        Image b = Img(parent, name, Rounded(), BubbleBg, 18f);
        Image line = Img(b.transform, "Line", RoundedOutline(), BubbleLine, 18f);
        Stretch(line.rectTransform);
        text = Text(b.transform, "Text", "", fontSize, Cream, TextAlignmentOptions.Center, true);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        Stretch(text.rectTransform, 16, 16, 8, 10);
        return b;
    }

    // يكبّر الفقاعة على قد الكلام (ما يطلع الكلام برّا)
    public static void FitBubble(RectTransform bubble, RTLTextMeshPro t, string msg, float maxWidth, float extraRight = 0f)
    {
        t.text = msg;
        t.UpdateText();
        float padX = 32f, padY = 22f;
        Vector2 pref = t.GetPreferredValues(maxWidth - padX - extraRight, 0f);
        float w = Mathf.Clamp(pref.x + padX + extraRight, 120f, maxWidth);
        Vector2 pref2 = t.GetPreferredValues(w - padX - extraRight, 0f);
        bubble.sizeDelta = new Vector2(w, Mathf.Max(56f, pref2.y + padY));
        t.rectTransform.offsetMin = new Vector2(16, 10);
        t.rectTransform.offsetMax = new Vector2(-16 - extraRight, -8);
    }

    // يحوّل صورة قديمة (خشب) لنفس ستايل اللعبة: مستطيل دايري غامق شفاف بحد خفيف
    public static void DarkPill(Image img, float radius = 30f)
    {
        if (img == null) return;
        img.sprite = Rounded();
        img.color = new Color(0.07f, 0.045f, 0.025f, 0.86f);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = (img.sprite.border.x - 1f) / radius;
        if (img.transform.Find("PillLine") == null)
        {
            Image line = Img(img.transform, "PillLine", RoundedOutline(), new Color(1f, 0.8f, 0.5f, 0.35f), radius);
            Stretch(line.rectTransform);
            line.transform.SetAsFirstSibling();
        }
    }

    // علامة + أو − مرسومة (بدون خط، عشان تطلع واضحة دايم)
    public static void PlusSign(Transform parent, float size, bool minus, Color c)
    {
        float th = Mathf.Max(4f, size * 0.2f);
        Image h = Img(parent, "H", Rounded(), c, th * 0.5f);
        Place(h.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, th), Vector2.zero);
        if (minus) return;
        Image v = Img(parent, "V", Rounded(), c, th * 0.5f);
        Place(v.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(th, size), Vector2.zero);
    }

    // ===== أرقام عربية =====
    public static string Ar(int number)
    {
        char[] chars = number.ToString().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('٠' + (chars[i] - '0'));
        return new string(chars);
    }
}
