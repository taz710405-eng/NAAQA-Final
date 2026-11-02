using UnityEngine;
using UnityEngine.UI;

// ===== ظل الشرح =====
// يغمّق الشاشة كلها إلا دائرة حول الشي المطلوب، ويمسك أي ضغطة برّا (اللاعب ما يقدر يسوي غير المطلوب).
// يشتغل فوق كل النوافذ (حتى السوق الشعبي). GuideShade.Point(هدف) يأشر، و Point(null) يخفيه.
public class GuideShade : MonoBehaviour
{
    static GuideShade inst;
    Transform target;
    Canvas canvas;
    RectTransform root, layer, ring, hand;
    CanvasGroup group;
    Image[] parts = new Image[4];   // فوق، تحت، يسار، يمين
    Rect cur;
    bool hasCur;

    public static void Point(Transform t)
    {
        if (t == null && inst == null) return;
        if (inst == null) { inst = new GameObject("GuideShade").AddComponent<GuideShade>(); inst.Build(); }
        inst.target = t;
    }

    public static bool Showing { get { return inst != null && inst.target != null && inst.shown; } }

    void OnDestroy() { if (inst == this) inst = null; }

    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("GuideShadeCanvas", 950);
        canvas.transform.SetParent(transform, false);
        root = (RectTransform)canvas.transform;
        // الكانفس يبقى شغال دايم (عشان مقاسه يكون مقاس الشاشة)، ونخفي الطبقة اللي داخله بس
        layer = NaaqaUI.Rect("Layer", root);
        NaaqaUI.Stretch(layer);
        group = layer.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < 4; i++)
        {
            Image im = NaaqaUI.Img(layer, "Shade" + i, null, new Color(0f, 0f, 0f, 0.62f));
            im.raycastTarget = true;   // يمسك الضغطة برّا الدائرة
            RectTransform r = im.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = Vector2.zero;
            parts[i] = im;
        }
        Image rg = NaaqaUI.Img(layer, "Ring", NaaqaUI.RoundedOutline(), new Color(1f, 0.82f, 0.4f, 0.95f), 30f);
        rg.raycastTarget = false;
        ring = rg.rectTransform;
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        Image hd = NaaqaUI.IconImg(layer, "tap", 90);
        if (hd.sprite == null && TutorialSpotlight.Instance != null) hd.sprite = TutorialSpotlight.Instance.handSprite;   // نفس يد الشرح
        if (hd.sprite == null) hd.enabled = false;
        hd.raycastTarget = false;
        hand = hd.rectTransform;
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        SetShown(false);
    }

    bool shown;
    void SetShown(bool on)
    {
        shown = on;
        group.alpha = on ? 1f : 0f;
        group.blocksRaycasts = on;
        group.interactable = on;
    }

    void LateUpdate()
    {
        Rect sr = new Rect();
        bool show = target != null && target.gameObject.activeInHierarchy && ScreenRect(target, out sr) && ToLocal(sr, out sr);
        if (shown != show) SetShown(show);
        if (!show) { hasCur = false; return; }

        Rect want = sr;
        float pad = 18f;
        want = Rect.MinMaxRect(want.xMin - pad, want.yMin - pad, want.xMax + pad, want.yMax + pad);
        if (!hasCur) { cur = want; hasCur = true; }
        else
        {
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            cur = Rect.MinMaxRect(Mathf.Lerp(cur.xMin, want.xMin, k), Mathf.Lerp(cur.yMin, want.yMin, k),
                                  Mathf.Lerp(cur.xMax, want.xMax, k), Mathf.Lerp(cur.yMax, want.yMax, k));
        }

        Rect all = root.rect;
        Set(parts[0], Rect.MinMaxRect(all.xMin, cur.yMax, all.xMax, all.yMax));      // فوق
        Set(parts[1], Rect.MinMaxRect(all.xMin, all.yMin, all.xMax, cur.yMin));      // تحت
        Set(parts[2], Rect.MinMaxRect(all.xMin, cur.yMin, cur.xMin, cur.yMax));      // يسار
        Set(parts[3], Rect.MinMaxRect(cur.xMax, cur.yMin, all.xMax, cur.yMax));      // يمين

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f;
        ring.anchoredPosition = cur.center;
        ring.sizeDelta = cur.size * pulse;
        float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 14f;
        hand.anchoredPosition = new Vector2(cur.xMax + 20f, cur.yMin - 30f - bounce);
    }

    static void Set(Image im, Rect r)
    {
        RectTransform t = im.rectTransform;
        t.anchoredPosition = r.position;
        t.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
    }

    bool ToLocal(Rect screen, out Rect local)
    {
        Vector2 a, b;
        local = new Rect();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen.min, null, out a)) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen.max, null, out b)) return false;
        local = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        return local.width > 1f && local.height > 1f;
    }

    // مكان الهدف على الشاشة (زر أو شي في العالم)
    static bool ScreenRect(Transform t, out Rect r)
    {
        r = new Rect();
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        RectTransform rt = t as RectTransform;
        if (rt != null)
        {
            Canvas c = rt.GetComponentInParent<Canvas>();
            if (c == null) return false;
            Camera cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
            Vector3[] k = new Vector3[4];
            rt.GetWorldCorners(k);
            foreach (Vector3 v in k)
            {
                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, v);
                min = Vector2.Min(min, s); max = Vector2.Max(max, s);
            }
        }
        else
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return false;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) if (rs[i].enabled) b.Encapsulate(rs[i].bounds);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 s = cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                        if (s.z < 0f) return false;
                        min = Vector2.Min(min, s); max = Vector2.Max(max, s);
                    }
            // الصورة فيها فراغ حولها: نصغّر شوي
            Vector2 c2 = (min + max) * 0.5f, h = (max - min) * 0.36f;
            min = c2 - h; max = c2 + h;
        }
        r = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return r.width > 1f && r.height > 1f;
    }
}
