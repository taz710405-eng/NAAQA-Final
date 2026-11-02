using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

// ===== خط واضح على كل الشاشات =====
// خط اللعبة القديم (NotoSansArabic-FULL SDF) انبنى بحجم صغير (٣٣) وهامش ١ بكسل بس،
// فلما تصغر الواجهة (الآيباد، الجوال، نافذة صغيرة) تطلع الحروف مسننة وحولها حواف بيضا.
// هنا نبني نسخة من نفس الخط وقت التشغيل بجودة عالية (حجم ٩٠، هامش ٩)، ونبدّل كل النصوص لها.
// نفس الخط ونفس الشكل، بس أحدّ. لو ما انبنت لأي سبب يبقى الخط القديم مثل ما هو.
public static class FontUpgrade
{
    static TMP_FontAsset hq;
    static bool tried;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { hq = null; tried = false; }

    public static TMP_FontAsset HQ
    {
        get
        {
            if (tried) return hq;
            tried = true;
            try
            {
                Font src = Resources.Load<Font>("NaaqaFont/naaqa_arabic");
                if (src != null)
                    hq = TMP_FontAsset.CreateFontAsset(src, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (hq != null) hq.name = "Naaqa Arabic HQ";
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("ما انبنى الخط الواضح، نكمل بالخط القديم: " + e.Message);
                hq = null;
            }
            return hq;
        }
    }

    static bool IsOld(TMP_FontAsset f)
    {
        return f != null && f != hq && f.name.StartsWith("NotoSansArabic");
    }

    public static void Swap(TMP_Text t)
    {
        if (t == null || !IsOld(t.font) || HQ == null) return;
        float ow = t.outlineWidth;
        Color32 oc = t.outlineColor;
        t.font = HQ;
        if (ow > 0f) { t.outlineWidth = ow; t.outlineColor = oc; }
    }

    public static void SwapAll()
    {
        if (HQ == null) return;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            Swap(t);
    }
}
