using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ===== شكل الحوش حسب مستواه =====
// الصور في Assets/Resources/NaaqaPen/:
//   pen_1.png ... pen_5.png  = الحوش بكل مستوى (نفس المقاس ١٥٣٦×١١٨٠، والحوش في نص الصورة)
//   pen_trough.png           = حوض الماء (يطلع على الحوش لحاله لما ينشرى)
// لو صورة مستوى ما هي موجودة، يبقى شكل الحوش الحالي.
public class PenVisual : MonoBehaviour
{
    public const string Folder = "NaaqaPen";
    // مكان الحوض على صورة كل مستوى (٠..١ من اليسار ومن تحت): جنب السياج القدامي اليمين
    static readonly Vector2[] TroughSpot =
    {
        new Vector2(0.686f, 0.263f), new Vector2(0.692f, 0.238f), new Vector2(0.715f, 0.228f),
        new Vector2(0.694f, 0.235f), new Vector2(0.767f, 0.230f),
    };
    static readonly float[] TroughWidth = { 0.221f, 0.257f, 0.259f, 0.309f, 0.251f };   // من عرض الصورة

    SpriteRenderer sr;
    Sprite original;
    Vector2 size;           // مقاس الحوش بالوحدات
    SpriteRenderer trough;
    int shownLevel = -1;
    bool shownTrough;
    int shownCamels = -1;
    string shownSig = "";
    float sigTimer;

    // مكان الأحواش الإضافية ومستوياتها (لو تغير نعيد توزيع الإبل)
    static List<ExtraPen> ExtraPens()
    {
        if (PlacementManager.Instance == null || PlacementManager.IsPlacing) return new List<ExtraPen>();
        return ExtraPen.All();
    }

    static string ExtraSig()
    {
        if (PlacementManager.IsPlacing) return null;   // وقت التركيب ما نغيّر شي
        string s = "";
        foreach (ExtraPen g in ExtraPens()) s += Mathf.RoundToInt(g.transform.position.x * 10) + "," + Mathf.RoundToInt(g.transform.position.z * 10) + "," + g.Level + ";";
        return s;
    }

    // ترتيب ثابت للإبل: القديمة تبقى بمكانها، والجديدة تنضاف آخر شي
    readonly List<Camel> order = new List<Camel>();
    readonly Dictionary<Camel, string> homeKey = new Dictionary<Camel, string>();
    readonly Dictionary<Camel, Vector3> homePos = new Dictionary<Camel, Vector3>();
    readonly Dictionary<Camel, float> homeRange = new Dictionary<Camel, float>();
    readonly Dictionary<Camel, int> penOf = new Dictionary<Camel, int>();
    readonly Dictionary<Camel, GameObject> penGo = new Dictionary<Camel, GameObject>();
    readonly List<Renderer> hidden = new List<Renderer>();

    // رقم الحوش اللي فيه الناقة: -١ الرئيسي، ٠.. الإضافي (بنفس ترتيب ExtraPen.All)، -٩٩ ما ندري
    public static int PenIndexOf(Camel c)
    {
        int i;
        if (Instance == null || c == null || !Instance.penOf.TryGetValue(c, out i)) return -99;
        return i;
    }

    // نقطة قدام باب الحوش (للحلاب)
    public static bool PenFront(int idx, out Vector3 pos)
    {
        pos = Vector3.zero;
        if (Instance == null) return false;
        Bounds b;
        if (idx < 0) b = Instance.sr.bounds;
        else
        {
            List<ExtraPen> ex = ExtraPen.All();
            if (idx >= ex.Count) return false;
            SpriteRenderer er = ex[idx].GetComponentInChildren<SpriteRenderer>();
            if (er == null) return false;
            b = er.bounds;
        }
        pos = new Vector3(b.center.x + b.size.x * 0.2f, Instance.transform.position.y + 0.06f, b.min.z + b.size.z * 0.2f);
        return true;
    }

    // وقت تحريك حوش: إبله تختفي، وترجع معه لما ينحط
    void HideMoving()
    {
        GameObject moving = PlacementManager.MovingObject;
        if (moving == null)
        {
            if (hidden.Count > 0) { foreach (Renderer r in hidden) if (r != null) r.enabled = true; hidden.Clear(); }
            return;
        }
        if (hidden.Count > 0) return;
        foreach (Camel c in order)
        {
            GameObject g;
            if (c == null || !penGo.TryGetValue(c, out g) || g != moving) continue;
            foreach (Renderer r in c.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hidden.Add(r); }
        }
    }
    Vector3 lastPos;
    bool hadPos;

    public static PenVisual Instance;
    void OnDestroy() { if (Instance == this) Instance = null; }

    // لما يتحرك الحوش (وضع التعديل) إبله تتحرك معه
    void FollowMove()
    {
        Vector3 now = transform.position;
        if (!hadPos) { hadPos = true; lastPos = now; return; }
        Vector3 d = now - lastPos;
        if (d.sqrMagnitude < 1e-8f) return;
        lastPos = now;
        foreach (Camel c in order)
        {
            int pn;
            if (c == null || !penOf.TryGetValue(c, out pn) || pn != -1) continue;
            Vector3 h = homePos[c] + d;
            homePos[c] = h;
            CamelMovement m = c.GetComponent<CamelMovement>();
            if (m != null) m.SetHome(h, homeRange[c], true);
            else c.transform.position += d;
        }
    }
    Vector3 camelBase;          // حجم الناقة الأصلي (قبل التصغير)
    float cw, ch;               // مقاس صورة الحوش الحالية بالوحدات

    // حجم الإبل مع صور الحوش الجديدة (أصغر عشان تتناسب معها)
    public const float CamelSize = 0.5f;
    // نص ساحة الحوش (من نص الصورة) ومسافة مشي الإبل
    static readonly Vector2 YardCenter = new Vector2(-0.05f, -0.06f);
    // أماكن الإبل المرتبة داخل الساحة (من عرض/طول صورة الحوش): صفين، ٣ ورا و٢ قدام
    static readonly Vector2[] Slots =
    {
        new Vector2(-0.03f, 0.04f), new Vector2(-0.10f, -0.10f), new Vector2(0.05f, -0.10f),
        new Vector2(-0.15f, 0.05f), new Vector2(0.09f, 0.03f),
    };
    static Vector3 SlotOffset(int slot, float w, float h, float k)
    {
        Vector2 o = Slots[Mathf.Clamp(slot, 0, Slots.Length - 1)];
        // لو زادت الإبل عن الأماكن (حفظ قديم) نزحزحها شوي عشان ما تركب فوق بعض
        float extra = slot >= Slots.Length ? 0.03f * (slot - Slots.Length + 1) : 0f;
        return new Vector3((YardCenter.x + o.x + extra) * w * k, 0f, (YardCenter.y + o.y - extra) * h * k);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        foreach (SpriteRenderer r in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            string n = r.gameObject.name;
            if (n != "العزبه" && n != "العزبة") continue;
            if (r.GetComponent<PenVisual>() == null) r.gameObject.AddComponent<PenVisual>();
            return;
        }
    }

    void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
        original = sr.sprite;
        size = original != null ? (Vector2)original.bounds.size : new Vector2(15.36f, 10.24f);
        cw = size.x; ch = size.y;
        foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Include))
            if (!c.name.StartsWith("Breed_")) { Vector3 s = c.transform.localScale; camelBase = new Vector3(Mathf.Abs(s.x), s.y, s.z); break; }
    }

    void Update()
    {
        HideMoving();
        FollowMove();
        FarmStore st = FarmStore.Instance;
        int lvl = st != null ? st.PenLevel : 1;
        bool hasTrough = false;   // حوض المويه انشال (صار فيه بير)
        int camels = CamelBreeds.CamelCount();
        sigTimer -= Time.deltaTime;
        string sig = shownSig;
        if (sigTimer <= 0f) { sigTimer = 1f; string now = ExtraSig(); if (now != null) sig = now; }
        if (PlacementManager.IsPlacing) return;
        if (lvl == shownLevel && hasTrough == shownTrough && camels == shownCamels && sig == shownSig) return;
        bool layout = lvl != shownLevel || camels != shownCamels || sig != shownSig;
        shownSig = sig;
        shownLevel = lvl;
        shownTrough = hasTrough;
        shownCamels = camels;
        Apply(lvl, hasTrough);
        if (layout) PlaceCamels(lvl);
    }

    // الإبل: حجم يتناسب مع الحوش، وكل وحدة لها مكان ثابت داخل الساحة (ما تتكوّم فوق بعض)
    // الحوش الكبير يتعبى أول، والزايد يروح للأحواش الإضافية
    void PlaceCamels(int lvl)
    {
        if (Resources.Load<Texture2D>(Folder + "/pen_" + lvl) == null || camelBase == Vector3.zero) return;

        order.RemoveAll(c => c == null || !c.gameObject.activeInHierarchy);
        List<Camel> found = new List<Camel>(FindObjectsByType<Camel>(FindObjectsInactive.Exclude));
        // إبل الحوش الأصلية أول، بعدها اللي انشرت
        found.Sort((a, b) =>
        {
            int ba = a.name.StartsWith("Breed_") ? 1 : 0, bb = b.name.StartsWith("Breed_") ? 1 : 0;
            return ba != bb ? ba - bb : string.CompareOrdinal(a.name, b.name);
        });
        foreach (Camel c in found) if (!order.Contains(c)) order.Add(c);

        int mainCap = GameRules.CamelsForPen(lvl);
        List<ExtraPen> extras = ExtraPens();

        int pen = -1, slot = 0, inPen = mainCap;
        for (int i = 0; i < order.Count; i++)
        {
            Camel c = order[i];
            // ننتقل للحوش اللي بعده لما يمتلي اللي قبله
            if (slot >= inPen && pen + 1 < extras.Count) { pen++; slot = 0; inPen = extras[pen].Cap; }

            Vector3 home;
            float range;
            if (pen < 0)
            {
                // أماكن ثابتة ومرتبة (الجديدة ما تحرك القديمة)
                float k = (0.9f + 0.05f * lvl) / 1.15f;   // ساحة الحوش الأصغر أضيق
                home = transform.position + SlotOffset(slot, cw, ch, k);
                range = cw * 0.006f;
            }
            else
            {
                SpriteRenderer er = extras[pen].GetComponentInChildren<SpriteRenderer>();
                Bounds eb = er != null ? er.bounds : new Bounds(extras[pen].transform.position, Vector3.one);
                float k = extras[pen].Level >= 2 ? (0.9f + 0.05f * extras[pen].Level) / 1.15f : 0.85f;
                home = new Vector3(eb.center.x, c.transform.position.y, eb.center.z) + SlotOffset(slot, eb.size.x, eb.size.z, k);
                range = eb.size.x * 0.006f;
            }
            slot++;

            CamelAnimator anim = c.GetComponent<CamelAnimator>();
            if (anim != null) anim.SetSize(camelBase * CamelSize);
            else c.transform.localScale = camelBase * CamelSize;

            // ما نحرك الناقة إلا لو مكانها تغير، ونقفزها بس لو انتقلت لحوش ثاني أو جديدة
            string key = pen + ":" + Mathf.RoundToInt(home.x * 20) + "," + Mathf.RoundToInt(home.z * 20);
            string old;
            bool had = homeKey.TryGetValue(c, out old);
            penGo[c] = pen < 0 ? gameObject : extras[pen].gameObject;
            penOf[c] = pen;
            if (had && old == key) continue;
            homeKey[c] = key;
            homePos[c] = home;
            homeRange[c] = range;
            penOf[c] = pen;
            CamelMovement m = c.GetComponent<CamelMovement>();
            if (m != null) m.SetHome(home, range, true);   // تنط لمكانها على طول (ما تمشي له)
        }
    }

    // الحوش الكبير بنفس حجم الأحواش الإضافية (نفس المقياس حق صورها)
    float MatchExtraPpu(float fallback)
    {
        InventoryManager inv = InventoryManager.Instance;
        BagItemData d = inv != null ? inv.GetData(ExtraPen.Id) : null;
        SpriteRenderer er = d != null && d.worldPrefab != null ? d.worldPrefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (er == null || er.sprite == null) return fallback;
        float extraScale = Mathf.Abs(er.transform.lossyScale.x);
        float myScale = Mathf.Abs(transform.lossyScale.x);
        if (extraScale <= 0f || myScale <= 0f) return fallback;
        return er.sprite.pixelsPerUnit * myScale / extraScale;
    }

    void Apply(int lvl, bool hasTrough)
    {
        // شكل المستوى
        Texture2D tex = Resources.Load<Texture2D>(Folder + "/pen_" + lvl);
        bool custom = tex != null;
        cw = size.x; ch = size.y;   // مقاس الصورة بالوحدات
        if (custom)
        {
            float ppu = MatchExtraPpu(tex.width / size.x);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            cw = tex.width / ppu;
            ch = tex.height / ppu;
        }
        else sr.sprite = original;
        int i = Mathf.Clamp(lvl, 1, 5) - 1;

        // مكان الضغط على قد الصورة ورقيق (عشان ما يغطي الإبل فوقه وتقدر تجمع حليبها)
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && sr.sprite != null)
        {
            Bounds lb = sr.sprite.bounds;
            box.center = new Vector3(lb.center.x, lb.center.y, 0f);
            box.size = new Vector3(lb.size.x * 0.8f, lb.size.y * 0.8f, 0.02f);
        }

        // الحوض: يطلع بس مع صور الحوش الجديدة (الصورة القديمة فيها حوض مرسوم)
        Texture2D tt = Resources.Load<Texture2D>(Folder + "/pen_trough");
        bool show = hasTrough && custom && tt != null;
        if (show && trough == null)
        {
            GameObject g = new GameObject("PenTrough");
            g.transform.SetParent(transform, false);
            trough = g.AddComponent<SpriteRenderer>();
            trough.sprite = Sprite.Create(tt, new Rect(0, 0, tt.width, tt.height), new Vector2(0.5f, 0.5f), 100f);
            trough.sortingLayerID = sr.sortingLayerID;
            trough.sortingOrder = sr.sortingOrder + 1;
        }
        if (trough != null)
        {
            trough.gameObject.SetActive(show);
            trough.transform.localPosition = new Vector3((TroughSpot[i].x - 0.5f) * cw, (TroughSpot[i].y - 0.5f) * ch, -0.02f);
            // الحجم حسب المستوى
            float want = TroughWidth[i] * cw;
            float now = trough.sprite.bounds.size.x;
            if (now > 0f) trough.transform.localScale = Vector3.one * (want / now);
        }
    }
}
