using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// ===== الطريق العام (خط السفر) =====
// طريق مزفلت رايح جاي ثابت في الخريطة (يسار تحت)، ولوحة «الدرعية» البنية على طرفه.
// ما يتحرك، وما ينركب عليه أي غرض (PlacementManager يرفض).
// الصور في Assets/Resources/NaaqaWorld/: road.png (قطعة تتكرر) و sign_diriyah.png
public class HighwayRoad : MonoBehaviour
{
    public static HighwayRoad Instance;

    // ===== مكان الطريق في الخريطة (بوحدات العالم، الحوش الرئيسي الأصلي في 0,0) =====
    // ثابت في العالم (ما يتأثر بتقريب الكاميرا)، يمر تحت يسار الخيمة والبسطة
    static readonly Vector2 RoadPoint = new Vector2(-7.1f, -15f);     // نقطة على نص الطريق (x, z)
    static readonly Vector2 SignPoint = new Vector2(-30f, -5.4f);     // قاعدة عمود اللوحة (تحت الطريق، واللوحة فوقه)
    const float RoadUnitsPerPx = 0.0107f;   // حجم الطريق
    const float SignUnitsPerPx = 0.005f;   // حجم اللوحة

    // مقاسات صورة الطريق (بالبكسل): الاتجاه الطويل (ميل ٢:١ نفس الأحواش)، والعرض، ونقطة البداية
    static readonly Vector2 ImgStep = new Vector2(1226f, 613f);
    static readonly Vector2 ImgCap = new Vector2(260f, -130f);
    static readonly Vector2 ImgA = new Vector2(20f, 150f);
    const float ImgWidth = 232.6f;   // عرض الطريق الحقيقي (عمودي عليه)
    static readonly Vector2 SignPivot = new Vector2(0.063f, 0.107f);

    Vector3 linePoint, lineDir, lineNormal;
    float halfWidth;
    bool ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (Instance != null) return;
        if (Resources.Load<Texture2D>("NaaqaWorld/road") == null) return;
        Instance = new GameObject("HighwayRoad").AddComponent<HighwayRoad>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    IEnumerator Start()
    {
        // ننتظر الحوش
        float t = 0f;
        while (PenVisual.Instance == null && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
        if (PenVisual.Instance == null) yield break;
        Build();
    }

    void Build()
    {
        SpriteRenderer pen = PenVisual.Instance.GetComponent<SpriteRenderer>();
        float groundY = PenVisual.Instance.transform.position.y;
        System.Func<Vector2, Vector3> world = v => new Vector3(v.x, groundY, v.y);

        // ===== الطريق =====
        Texture2D tex = Resources.Load<Texture2D>("NaaqaWorld/road");
        float w = RoadUnitsPerPx;                              // وحدات لكل بكسل من الصورة
        float ppu = 1f / w;
        Vector2 center = ImgA + ImgStep * 0.5f + ImgCap * 0.5f;
        Vector2 pivot = new Vector2(center.x / tex.width, 1f - center.y / tex.height);
        Sprite road = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);

        // الصورة: y لتحت، والعالم: z لفوق
        Vector3 step = new Vector3(ImgStep.x, 0f, -ImgStep.y) * w * 0.998f;
        linePoint = world(RoadPoint);
        lineDir = step.normalized;
        lineNormal = new Vector3(-lineDir.z, 0f, lineDir.x);
        halfWidth = ImgWidth * w * 0.5f;

        for (int i = -5; i <= 5; i++)
        {
            GameObject g = new GameObject("Road_" + i);
            g.transform.SetParent(transform, false);
            g.transform.position = linePoint + step * i + Vector3.up * 0.004f;
            g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = road;
            if (pen != null) { sr.sortingLayerID = pen.sortingLayerID; sr.sortingOrder = pen.sortingOrder - 2; }
        }

        // ===== لوحة الدرعية =====
        Texture2D st = Resources.Load<Texture2D>("NaaqaWorld/sign_diriyah");
        if (st != null)
        {
            Sprite sign = Sprite.Create(st, new Rect(0, 0, st.width, st.height), SignPivot, 1f / SignUnitsPerPx);
            GameObject g = new GameObject("DiriyahSign");
            g.transform.SetParent(transform, false);
            g.transform.position = world(SignPoint) + Vector3.up * 0.02f;
            g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = sign;
            if (pen != null) { sr.sortingLayerID = pen.sortingLayerID; sr.sortingOrder = pen.sortingOrder + 3; }
        }
        ready = true;
    }

    // خط الطريق (للوايت والسيارات)
    public static bool Line(out Vector3 point, out Vector3 dir, out Vector3 normal, out float half)
    {
        HighwayRoad r = Instance;
        point = dir = normal = Vector3.zero; half = 0f;
        if (r == null || !r.ready) return false;
        point = r.linePoint; dir = r.lineDir; normal = r.lineNormal; half = r.halfWidth;
        return true;
    }

    // هل الغرض قريب من الطريق (على جنبه)؟
    public static bool Near(Bounds b, float margin)
    {
        HighwayRoad r = Instance;
        if (r == null || !r.ready) return true;
        float min = float.MaxValue, max = float.MinValue;
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 p = new Vector3(b.center.x + b.extents.x * x, 0f, b.center.z + b.extents.z * z);
                float s = Vector3.Dot(p - new Vector3(r.linePoint.x, 0f, r.linePoint.z), r.lineNormal);
                min = Mathf.Min(min, s); max = Mathf.Max(max, s);
            }
        float h = r.halfWidth + margin;
        return min <= h && max >= -h;
    }

    // هل المستطيل (حدود غرض) يلمس الطريق؟
    public static bool Overlaps(Bounds b)
    {
        HighwayRoad r = Instance;
        if (r == null || !r.ready) return false;
        float min = float.MaxValue, max = float.MinValue;
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 p = new Vector3(b.center.x + b.extents.x * x, 0f, b.center.z + b.extents.z * z);
                Vector3 d = p - new Vector3(r.linePoint.x, 0f, r.linePoint.z);
                float s = Vector3.Dot(d, r.lineNormal);
                min = Mathf.Min(min, s); max = Mathf.Max(max, s);
            }
        return min <= r.halfWidth && max >= -r.halfWidth;
    }
}
