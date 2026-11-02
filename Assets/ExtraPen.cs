using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// ===== الحوش الإضافي =====
// يتركب من الشنطة. يبدأ بناقتين، ويتطور ٣ مرات لين ٥ نوق (٢٠٠ / ٣٥٠ / ٥٥٠).
// الأول يفتح مستوى ٨ (٦٠٠)، والثاني مستوى ١٤ (٩٠٠)، وأقصى شي ٣ أحواش كلها.
// مستوى ١ = صورة الحوش الإضافي، ومن ٢ وفوق = نفس صور الحوش الكبير بس أصغر.
// اضغط عليه (برا وضع التعديل) عشان تطوره.
public class ExtraPen : MonoBehaviour
{
    public const string Id = "pen_extra";
    public int Level { get; private set; } = 1;

    SpriteRenderer sr;
    float ppu;          // نفس مقاس صورة الحوش الإضافي الأصلية
    Vector2 pivot;
    int shown = -1;

    public static int Capacity(int level) { return Mathf.Min(MaxCamels, GameRules.ExtraPenCapacity(level)); }

    // كم حوش إضافي عنده (متركب أو في الشنطة)
    public static int Owned()
    {
        int n = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(Id) : 0;
        if (PlacementManager.Instance != null)
            foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced()) if (p.id == Id) n++;
        if (PlacementManager.IsPlacing && PlacementManager.MovingObject != null && PlacementManager.MovingObject.GetComponent<ExtraPen>() != null) n++;
        return n;
    }
    public const int MaxCamels = 5;   // كل حوش أعلى شي ٥ نوق
    public int Cap { get { return Capacity(Level); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PlacementManager.OnItemPlaced -= OnPlaced;
        PlacementManager.OnItemPlaced += OnPlaced;
    }

    static void OnPlaced(string id, GameObject go)
    {
        if (id != Id || go == null) return;
        if (go.GetComponent<ExtraPen>() == null) go.AddComponent<ExtraPen>();
    }

    // الأحواش الإضافية المتركبة (بنفس ترتيب الحفظ)
    public static List<ExtraPen> All()
    {
        List<ExtraPen> l = new List<ExtraPen>();
        if (PlacementManager.Instance == null) return l;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
        {
            if (p.id != Id || p.go == null) continue;
            ExtraPen e = p.go.GetComponent<ExtraPen>();
            if (e == null) e = p.go.AddComponent<ExtraPen>();
            l.Add(e);
        }
        return l;
    }

    public static int TotalCap()
    {
        int n = 0;
        foreach (ExtraPen e in All()) n += e.Cap;
        return n;
    }

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            Sprite s = sr.sprite;
            ppu = s.pixelsPerUnit;
            pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
        }
        // نفس طبقة الحوش الكبير عشان الإبل تطلع فوقه
        foreach (PenVisual pv in FindObjectsByType<PenVisual>(FindObjectsInactive.Exclude))
        {
            SpriteRenderer main = pv.GetComponent<SpriteRenderer>();
            if (main != null && sr != null) { sr.sortingLayerID = main.sortingLayerID; sr.sortingOrder = main.sortingOrder; }
            break;
        }
        // الضغط ينقرأ من الـ collider، فنحط عليه مستقبل للضغطة
        BoxCollider box = GetComponentInChildren<BoxCollider>(true);
        if (box != null && box.GetComponent<ExtraPenTap>() == null) box.gameObject.AddComponent<ExtraPenTap>().pen = this;
        Apply();
    }

    public void SetLevel(int lvl)
    {
        Level = Mathf.Clamp(lvl, 1, GameRules.ExtraPenMaxLevel);
        Apply();
    }

    // للحفظ: نخزنه في خانة «extra» حق الغرض المتركب
    public string SaveState() { return "pen:" + Level; }
    public void LoadState(string s)
    {
        if (string.IsNullOrEmpty(s) || !s.StartsWith("pen:")) return;
        int l;
        if (int.TryParse(s.Substring(4), out l)) SetLevel(l);
    }

    void Apply()
    {
        if (sr == null || shown == Level) return;
        shown = Level;
        if (Level >= 2)
        {
            Texture2D tex = Resources.Load<Texture2D>(PenVisual.Folder + "/pen_" + Level);
            if (tex == null) return;
            // بنفس المقياس: حوش مستوى ٢ يطلع بنفس عرض الحوش الإضافي، والأعلى أكبر شوي
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
        }
        else
        {
            Texture2D tex = Resources.Load<Texture2D>(ShopExtraItems.Folder + "/" + Id);
            if (tex != null) sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
        }
        BoxCollider box = GetComponentInChildren<BoxCollider>(true);
        if (box != null && sr.sprite != null)
        {
            // رقيق وعلى قد الحوش (عشان ما يغطي الإبل وتقدر تجمع حليبها)
            Vector3 size = sr.sprite.bounds.size;
            box.size = new Vector3(size.x * 0.8f, size.y * 0.8f, 0.02f);
            Vector3 ctr = sr.sprite.bounds.center;
            box.center = new Vector3(ctr.x, ctr.y, 0f);
        }
    }

}

// الضغط على الحوش الإضافي يفتح التطوير
public class ExtraPenTap : MonoBehaviour
{
    public ExtraPen pen;

    void OnMouseUpAsButton()
    {
        if (pen == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (PlacementManager.IsPlacing || EditMode.Active) return;
        PenScreen.ShowExtra(pen);
    }
}
