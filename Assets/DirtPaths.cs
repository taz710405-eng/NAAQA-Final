using UnityEngine;
using System.Collections.Generic;

// ===== الطريق الترابي =====
// كل قطعة = مربعين (معين ٤×٢). لما تحط قطعة جنب قطعة ثانية تلتصق فيها وتصير طريق واحد.
// والشخصيات (الزباين والحلاب) تمشي على الطرق المتصلة.
public static class DirtPaths
{
    public const string Id = "dirt_path";
    // خطوة القطعة اللي جنبها (بالعالم x,z) على اتجاهين المعين
    // الخطوة تنحسب من مقاس القطعة الحقيقي (عشان تلتصق صح مهما كان حجمها)، مع تداخل خفيف يخفي الحد
    static Vector3 StepA = new Vector3(2f, 0f, 1f);
    static Vector3 StepB = new Vector3(2f, 0f, -1f);
    const float Overlap = 0.9f;
    static float LinkTol = 0.35f;

    static void Measure(GameObject g)
    {
        if (g == null) return;
        Renderer r = g.GetComponentInChildren<Renderer>();
        if (r == null) return;
        Vector3 sz = r.bounds.size;
        if (sz.x < 0.1f || sz.z < 0.05f) return;
        float hx = sz.x * 0.5f * Overlap, hz = sz.z * 0.5f * Overlap;
        StepA = new Vector3(hx, 0f, hz);
        StepB = new Vector3(hx, 0f, -hz);
        LinkTol = Mathf.Max(0.2f, hx * 0.25f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PlacementManager.OnItemPlaced -= OnPlaced;
        PlacementManager.OnItemPlaced += OnPlaced;
    }

    // الطريق تحت كل شي (الإبل والناس يمشون فوقه)
    static void OnPlaced(string id, GameObject go)
    {
        if (id != Id || go == null) return;
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || PenVisual.Instance == null) return;
        SpriteRenderer pen = PenVisual.Instance.GetComponent<SpriteRenderer>();
        if (pen != null) { sr.sortingLayerID = pen.sortingLayerID; sr.sortingOrder = pen.sortingOrder - 1; }
    }

    public static Vector3 Center(GameObject g)
    {
        if (g == null) return Vector3.zero;
        Renderer r = g.GetComponentInChildren<Renderer>();
        Vector3 c = r != null ? r.bounds.center : g.transform.position;
        c.y = 0f;
        return c;
    }

    public static List<Vector3> Tiles(GameObject except = null)
    {
        List<Vector3> l = new List<Vector3>();
        if (PlacementManager.Instance == null) return l;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == Id && p.go != null && p.go != except) l.Add(Center(p.go));
        return l;
    }

    // تلصق القطعة اللي نحركها بأقرب قطعة جنبها (لو قريبة)
    public static Vector3 SnapToNeighbor(Vector3 pos, GameObject current)
    {
        if (current == null) return pos;
        Measure(current);
        Vector3 offset = Center(current) - new Vector3(current.transform.position.x, 0f, current.transform.position.z);
        Vector3 want = new Vector3(pos.x, 0f, pos.z) + offset;
        float best = StepA.magnitude * 1.1f;   // يلتصق لو قريب
        Vector3 bestC = Vector3.zero;
        bool found = false;
        List<Vector3> tiles = Tiles(current);
        foreach (Vector3 t in tiles)
            foreach (Vector3 st in new[] { StepA, -StepA, StepB, -StepB })
            {
                Vector3 c = t + st;
                // ما نلصقها فوق قطعة موجودة
                bool taken = false;
                foreach (Vector3 o in tiles) if ((o - c).sqrMagnitude < 0.5f) { taken = true; break; }
                if (taken) continue;
                float d = Vector3.Distance(c, want);
                if (d < best) { best = d; bestC = c; found = true; }
            }
        if (!found) return pos;
        Vector3 np = bestC - offset;
        return new Vector3(np.x, pos.y, np.z);
    }

    // قطعتين فوق بعض (مو جنب بعض)
    public static bool TooClose(Vector3 a, Vector3 b)
    {
        return (a - b).magnitude < StepA.magnitude * 0.55f;
    }

    static bool Linked(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        return (d - StepA).magnitude < LinkTol || (d + StepA).magnitude < LinkTol
            || (d - StepB).magnitude < LinkTol || (d + StepB).magnitude < LinkTol;
    }

    // طريق من «from» لين «to» على القطع المتصلة. يرجع null لو ما فيه.
    // fromFar = نبدأ من أبعد قطعة متصلة (للزباين اللي جايين من بعيد)
    public static List<Vector3> Route(Vector3 from, Vector3 to, bool fromFar, float reach = 3.5f)
    {
        List<Vector3> t = Tiles();
        if (t.Count == 0) return null;
        if (PlacementManager.Instance != null)
            foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
                if (p.id == Id && p.go != null) { Measure(p.go); break; }
        to.y = 0f; from.y = 0f;
        int goal = Nearest(t, to, reach);
        if (goal < 0) return null;

        // BFS من الهدف
        int[] prev = new int[t.Count];
        int[] dist = new int[t.Count];
        for (int i = 0; i < t.Count; i++) { prev[i] = -1; dist[i] = -1; }
        Queue<int> q = new Queue<int>();
        q.Enqueue(goal); dist[goal] = 0;
        while (q.Count > 0)
        {
            int a = q.Dequeue();
            for (int b = 0; b < t.Count; b++)
                if (dist[b] < 0 && Linked(t[a], t[b])) { dist[b] = dist[a] + 1; prev[b] = a; q.Enqueue(b); }
        }

        int start = -1;
        if (fromFar)
        {
            // أبعد قطعة متصلة، ولو فيه أكثر من وحدة ناخذ الأقرب لـ from
            int far = 0;
            for (int i = 0; i < t.Count; i++) if (dist[i] > far) far = dist[i];
            if (far == 0) return null;
            float bd = float.MaxValue;
            for (int i = 0; i < t.Count; i++)
                if (dist[i] == far) { float d = (t[i] - from).sqrMagnitude; if (d < bd) { bd = d; start = i; } }
        }
        else
        {
            start = Nearest(t, from, reach);
            if (start < 0 || dist[start] < 0) return null;
        }

        List<Vector3> path = new List<Vector3>();
        for (int i = start; i >= 0; i = prev[i]) { path.Add(t[i]); if (i == goal) break; }
        return path;
    }

    static int Nearest(List<Vector3> t, Vector3 p, float reach)
    {
        int best = -1; float bd = reach * reach;
        for (int i = 0; i < t.Count; i++) { float d = (t[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
        return best;
    }
}
