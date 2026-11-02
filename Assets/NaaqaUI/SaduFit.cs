using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ===== أزرار السدو =====
// صور الأزرار في Assets/Resources/NaaqaUI/sadu_*.png
// الشريط (المثلثات) يتكرر بالعرض بدون ما يتمطط، والارتفاع يتناسب مع الزر.
public class SaduFit : MonoBehaviour
{
    // حدود كل صورة: يسار، تحت، يمين، فوق (بالبكسل). اللي بينها يتكرر.
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "primary",   new Vector4(76, 57, 80, 60) },
        { "secondary", new Vector4(85, 57, 80, 60) },
        { "disabled",  new Vector4(70, 55, 95, 58) },
        { "pill",      new Vector4(84, 44, 87, 22) },
    };
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s) && s != null) return s;
        Texture2D t = Resources.Load<Texture2D>("NaaqaUI/sadu_" + name);
        if (t == null) return null;
        Vector4 b;
        if (!Borders.TryGetValue(name, out b)) b = Vector4.zero;
        t.wrapMode = TextureWrapMode.Clamp;
        s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        cache[name] = s;
        return s;
    }

    // يحوّل صورة لشكل السدو. لو الصورة ما لقيناها يرجع false ويبقى الشكل القديم.
    public static bool Apply(Image img, string name, Button button = null)
    {
        Sprite s = Get(name);
        if (img == null || s == null) return false;
        img.sprite = s;
        img.color = Color.white;
        img.type = Image.Type.Tiled;
        img.fillCenter = true;
        SaduFit f = img.GetComponent<SaduFit>();
        if (f == null) f = img.gameObject.AddComponent<SaduFit>();
        f.img = img;
        f.normal = s;
        f.button = button;
        f.Fit();
        return true;
    }

    Image img;
    Sprite normal;
    Button button;
    float lastH = -1f;
    bool lastOn = true;

    void OnEnable() { lastH = -1f; Fit(); }
    void OnRectTransformDimensionsChange() { Fit(); }

    void LateUpdate()
    {
        // الزر المقفل يصير رمادي
        if (button != null && button.interactable != lastOn)
        {
            lastOn = button.interactable;
            Sprite d = Get("disabled");
            img.sprite = lastOn || d == null ? normal : d;
            lastH = -1f;
        }
        Fit();
    }

    void Fit()
    {
        if (img == null || img.sprite == null) return;
        float h = ((RectTransform)transform).rect.height;
        if (h <= 1f || Mathf.Abs(h - lastH) < 0.01f) return;
        lastH = h;
        // صورة الزر كاملة تطلع بارتفاع الزر بالضبط (فالشريط ما يتكرر لتحت)
        img.pixelsPerUnitMultiplier = img.sprite.rect.height / h;
    }
}
