using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;
    public static bool IsPlacing = false;
    public static bool IsDragging = false;

    public static System.Action<string, GameObject> OnItemPlaced;

    [Header("أزرار ✓ ✕")]
    public RectTransform placeControls;
    public Button confirmButton;
    public Button cancelButton;
    public float buttonsBelow = 70f;

    [Header("الأسهم")]
    public bool showGreenArrow = true;
    public float arrowLength = 0.3f;
    public float arrowGap = 0.1f;
    public Material arrowMaterial;

    [Header("أماكن ممنوع التركيب فوقها")]
    public GameObject[] blockers;          // العزبة، الإبل، ...
    [Range(0.3f, 1f)]
    public float overlapTolerance = 0.8f;  // أصغر = يسمح بتقارب أكثر
    public Color invalidColor = new Color(1f, 0.35f, 0.35f, 0.85f);

    private Camera cam;
    private GameObject current;
    private string currentId;
    private float groundY;
    private Vector3 dragOffset;
    private GameObject arrowsRoot;
    private readonly List<LineRenderer> arrows = new List<LineRenderer>();
    private readonly List<Vector3> arrowDirs = new List<Vector3>();

    private SpriteRenderer[] itemSprites;
    private Color[] itemColors;
    private bool canPlace = true;

    // وضع التعديل: نحرك غرض متركب من قبل
    private bool moving = false;
    private Vector3 moveFrom;
    public static bool IsMoving { get { return Instance != null && Instance.moving && IsPlacing; } }
    // الحوش الرئيسي جزء من المشهد (مو من الشنطة): يتحرك بس، ما يرجع للشنطة ولا ينحفظ مع الأغراض
    public const string MainPenId = "__pen_main";
    public static GameObject MovingObject { get { return IsMoving ? Instance.current : null; } }
    public static bool IsMovingMainPen { get { return IsMoving && Instance.currentId == MainPenId; } }
    public static System.Action OnMainPenMoved;

    public bool BeginMoveMainPen(GameObject pen)
    {
        if (IsPlacing || pen == null) return false;
        currentId = MainPenId;
        current = pen;
        moveFrom = pen.transform.position;
        groundY = moveFrom.y;
        moving = true;
        IsPlacing = true;

        itemSprites = current.GetComponentsInChildren<SpriteRenderer>();
        itemColors = new Color[itemSprites.Length];
        for (int k = 0; k < itemSprites.Length; k++) itemColors[k] = itemSprites[k].color;

        BuildArrows();
        ShowGrid(true);
        if (placeControls != null) placeControls.gameObject.SetActive(true);
        return true;
    }

    // الأغراض اللي تركبت قبل (ما نركب فوقها بعد)
    private readonly List<GameObject> placedItems = new List<GameObject>();
    private readonly List<string> placedIds = new List<string>();

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        IsPlacing = false;
        IsDragging = false;
    }

    void OnEnable() { BagWindowView.OnPlaceRequested += StartPlacing; }
    void OnDisable() { BagWindowView.OnPlaceRequested -= StartPlacing; }

    void Start()
    {
        if (placeControls != null) placeControls.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    // ===== بداية التركيب =====
    public void StartPlacing(string id)
    {
        if (IsPlacing) return;
        if (InventoryManager.Instance == null) return;

        BagItemData data = InventoryManager.Instance.GetData(id);
        if (data == null || data.worldPrefab == null)
        {
            Warn("ما فيه شكل لهذا الغرض على الخريطة!");
            return;
        }

        if (cam == null) cam = Camera.main;
        groundY = data.worldPrefab.transform.position.y;

        Vector3 pos;
        if (!ScreenToGround(new Vector2(Screen.width / 2f, Screen.height / 2f), out pos))
            pos = data.worldPrefab.transform.position;
        pos.y = groundY;

        pos = Snap(pos); pos.y = groundY;
        if (id == DirtPaths.Id) pos = DirtPaths.SnapToNeighbor(pos, null);
        current = Instantiate(data.worldPrefab, pos, data.worldPrefab.transform.rotation);
        ApplyItemScale(id, current);
        currentId = id;
        IsPlacing = true;

        itemSprites = current.GetComponentsInChildren<SpriteRenderer>();
        itemColors = new Color[itemSprites.Length];
        for (int i = 0; i < itemSprites.Length; i++) itemColors[i] = itemSprites[i].color;

        BuildArrows();
        ShowGrid(true);
        if (placeControls != null) placeControls.gameObject.SetActive(true);
    }

    // ===== تحريك غرض متركب (وضع التعديل) =====
    public GameObject PlacedAt(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);
        float best = float.MaxValue;
        GameObject hit = null;
        foreach (RaycastHit h in Physics.RaycastAll(ray))
            for (int i = 0; i < placedItems.Count; i++)
                if (placedItems[i] != null && h.collider.transform.IsChildOf(placedItems[i].transform) && h.distance < best)
                { best = h.distance; hit = placedItems[i]; }
        return hit;
    }

    public bool BeginMove(GameObject go)
    {
        if (IsPlacing || go == null) return false;
        int i = placedItems.IndexOf(go);
        if (i < 0) return false;

        currentId = placedIds[i];
        placedItems.RemoveAt(i);
        placedIds.RemoveAt(i);
        current = go;
        moveFrom = go.transform.position;
        groundY = moveFrom.y;
        moving = true;
        IsPlacing = true;

        itemSprites = current.GetComponentsInChildren<SpriteRenderer>();
        itemColors = new Color[itemSprites.Length];
        for (int k = 0; k < itemSprites.Length; k++) itemColors[k] = itemSprites[k].color;

        BuildArrows();
        ShowGrid(true);
        if (placeControls != null) placeControls.gameObject.SetActive(true);
        return true;
    }

    // يرجّع الغرض اللي نحركه للشنطة
    public void StoreMoving()
    {
        if (!moving || current == null) return;
        if (currentId == MainPenId) { Warn("الحوش الرئيسي ما يرجع للشنطة"); return; }
        string id = currentId;
        GameObject go = current;
        moving = false;
        EndPlacing();
        Destroy(go);
        if (InventoryManager.Instance != null) InventoryManager.Instance.Add(id);
        if (FarmStore.Instance != null) StartCoroutine(RecalcNextFrame());
        Notify("رجع للشنطة");
    }

    System.Collections.IEnumerator RecalcNextFrame()
    {
        yield return null;
        if (FarmStore.Instance != null) FarmStore.Instance.RecalcWaterCap();
    }

    // ===== قلب السور (يمين / يسار) =====
    Button flipBtn;
    static bool CanFlip(string id) { return id == "fence"; }

    // يقلب الغرض اللي نركبه (صورة معكوسة)، والقلب ينحفظ مع الدوران
    public void FlipCurrent()
    {
        if (current == null || !CanFlip(currentId)) return;
        current.transform.rotation = Quaternion.AngleAxis(180f, Vector3.forward) * current.transform.rotation;
    }

    void UpdateFlipButton()
    {
        bool want = IsPlacing && current != null && CanFlip(currentId) && placeControls != null;
        if (want && flipBtn == null)
        {
            flipBtn = NaaqaUI.IconButton(placeControls, "fence", "اقلب", 96, FlipCurrent);
            RectTransform r = (RectTransform)flipBtn.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, -130f);
        }
        if (flipBtn != null && flipBtn.gameObject.activeSelf != want) flipBtn.gameObject.SetActive(want);
    }

    void Update()
    {
        UpdateFlipButton();
        if (!IsPlacing || current == null) return;
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !TypingNow()) FlipCurrent();

        Pointer p = Pointer.current;
        if (p == null) return;

        Vector2 pos = p.position.ReadValue();

        if (p.press.wasPressedThisFrame && !IsPointerOverUI())
        {
            Ray ray = cam.ScreenPointToRay(pos);
            bool onItem = false;
            foreach (RaycastHit h in Physics.RaycastAll(ray))
            {
                if (h.collider.transform.IsChildOf(current.transform)) { onItem = true; break; }
            }
            // احتياط (مثل الحوش الأساسي): لو ما انضرب له collider، نشيك على صورته على الشاشة
            if (!onItem) onItem = OnScreenShape(pos);

            Vector3 g;
            if (onItem && ScreenToGround(pos, out g))
            {
                dragOffset = current.transform.position - g;
                IsDragging = true;
            }
        }

        if (IsDragging && p.press.isPressed)
        {
            Vector3 g;
            if (ScreenToGround(pos, out g))
            {
                Vector3 np = Snap(g + dragOffset);
                if (currentId == DirtPaths.Id) np = DirtPaths.SnapToNeighbor(np, current);
                np.y = groundY;
                current.transform.position = np;
            }
        }

        if (p.press.wasReleasedThisFrame) IsDragging = false;
    }

    // هل الضغطة على صورة الغرض اللي نحركه؟ (وسط الصورة، لأن حولها فراغ)
    bool OnScreenShape(Vector2 sp)
    {
        if (current == null || cam == null) return false;
        Bounds b = GetBounds(current);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 w = new Vector3(b.center.x + b.extents.x * x, b.center.y, b.center.z + b.extents.z * z);
                Vector3 s = cam.WorldToScreenPoint(w);
                min = Vector2.Min(min, s); max = Vector2.Max(max, s);
            }
        Vector2 c = (min + max) * 0.5f, h = (max - min) * 0.38f;
        return sp.x > c.x - h.x && sp.x < c.x + h.x && sp.y > c.y - h.y && sp.y < c.y + h.y;
    }

    static bool TypingNow()
    {
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return sel != null && sel.GetComponent<TMPro.TMP_InputField>() != null;
    }

    // ===== المربعات =====
    // الشبكة مايلة بنفس زاوية الرسم (ميل ٢:١)، وكل غرض ينط مربع مربع
    public const float Cell = 1f;      // نص عرض المربع بالوحدات
    GameObject gridGo;

    public static Vector3 Snap(Vector3 p)
    {
        float i = Mathf.Round(p.x / (2f * Cell) + p.z / Cell);
        float j = Mathf.Round(p.x / (2f * Cell) - p.z / Cell);
        return new Vector3((i + j) * Cell, p.y, (i - j) * Cell * 0.5f);
    }

    void ShowGrid(bool on)
    {
        if (!on) { if (gridGo != null) gridGo.SetActive(false); return; }
        if (gridGo == null)
        {
            const int W = 128, H = 64;
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1, 1, 1, 0), line = new Color(1f, 0.96f, 0.85f, 0.55f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // خطوط المعين: |x/W*2 - 1| + |y/H*2 - 1| = 1
                    float d = Mathf.Abs(Mathf.Abs((x + 0.5f) / W * 2f - 1f) + Mathf.Abs((y + 0.5f) / H * 2f - 1f) - 1f);
                    t.SetPixel(x, y, d < 0.035f ? line : clear);
                }
            t.Apply();
            Sprite sp = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W / (2f * Cell), 0, SpriteMeshType.FullRect);
            gridGo = new GameObject("PlacementGrid");
            gridGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer sr = gridGo.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(2f * Cell * 14f, Cell * 14f);
            sr.sortingOrder = 400;
        }
        gridGo.SetActive(true);
    }

    void LateUpdate()
    {
        if (!IsPlacing || current == null) return;
        if (gridGo != null && gridGo.activeSelf)
        {
            // الشبكة تحت الغرض، وتنط معه (ما تتحرك بنعومة عشان تبان المربعات ثابتة)
            Bounds gb = GetBounds(current);
            Vector3 c = Snap(new Vector3(gb.center.x, groundY, gb.center.z));
            gridGo.transform.position = new Vector3(c.x, groundY + 0.03f, c.z);
            SpriteRenderer gsr = gridGo.GetComponent<SpriteRenderer>();
            // تحت الغرض اللي نحركه (عشان الخطوط ما تغطيه)
            if (itemSprites != null && itemSprites.Length > 0 && itemSprites[0] != null)
            {
                gsr.sortingLayerID = itemSprites[0].sortingLayerID;
                int lo = int.MaxValue;
                foreach (SpriteRenderer r in itemSprites) if (r != null) lo = Mathf.Min(lo, r.sortingOrder);
                gsr.sortingOrder = lo - 1;
            }
            float span = Mathf.Max(gb.size.x, gb.size.z * 2f) + 6f * Cell;
            gsr.size = new Vector2(Mathf.Ceil(span / (2f * Cell)) * 2f * Cell, Mathf.Ceil(span / (2f * Cell)) * Cell);
        }

        // الحوش الرئيسي يتحرك بحرية (صورته كبيرة وتلمس اللي حوله)
        canPlace = currentId == MainPenId ? !HighwayRoad.Overlaps(Shrink(GetBounds(current))) : !IsOverlapping();
        if (canPlace && currentId == Musalla.Id && !HighwayRoad.Near(GetBounds(current), 2.5f)) canPlace = false;   // المصلى لازم جنب الطريق
        ApplyTint(!canPlace);

        UpdateArrows();
        UpdateButtons();
    }

    // ===== فحص التداخل =====
    bool IsOverlapping()
    {
        Bounds mine = Shrink(GetBounds(current));
        if (HighwayRoad.Overlaps(mine)) return true;   // الطريق العام ما ينركب عليه شي

        if (blockers != null)
            foreach (GameObject b in blockers)
                if (b != null && b != current && !b.transform.IsChildOf(current.transform) && !current.transform.IsChildOf(b.transform)
                    && !(currentId == MainPenId && b.GetComponentInChildren<Camel>(true) != null)   // إبل الحوش تتحرك معه
                    && b.activeInHierarchy && OverlapXZ(mine, Shrink(GetBounds(b))))
                    return true;

        bool path = currentId == DirtPaths.Id;
        for (int i = 0; i < placedItems.Count; i++)
        {
            GameObject b = placedItems[i];
            if (b == null) continue;
            if (path && placedIds[i] == DirtPaths.Id)
            {
                // قطع الطريق تلتصق ببعض، بس ما تنحط وحدة فوق الثانية
                if (DirtPaths.TooClose(DirtPaths.Center(b), DirtPaths.Center(current))) return true;
                continue;
            }
            if (OverlapXZ(mine, Shrink(GetBounds(b)))) return true;
        }

        return false;
    }

    Bounds Shrink(Bounds b)
    {
        b.extents = new Vector3(b.extents.x * overlapTolerance, b.extents.y, b.extents.z * overlapTolerance);
        return b;
    }

    bool OverlapXZ(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x && a.max.x > b.min.x &&
               a.min.z < b.max.z && a.max.z > b.min.z;
    }

    void ApplyTint(bool invalid)
    {
        if (itemSprites == null) return;
        for (int i = 0; i < itemSprites.Length; i++)
            if (itemSprites[i] != null)
                itemSprites[i].color = invalid ? invalidColor : itemColors[i];
    }

    // ===== ✓ =====
    void Confirm()
    {
        if (current == null) return;

        if (!canPlace)
        {
            Warn(currentId == Musalla.Id ? "المصلى لازم يكون جنب الطريق العام" : "ما تقدر تحطها هنا! حركها لمكان فاضي");
            return;
        }

        ApplyTint(false);
        CamelSounds.Play(currentId == DirtPaths.Id ? "dirt" : "place", 0.05f);
        if (moving && currentId == MainPenId)
        {
            moving = false;
            EndPlacing();
            if (OnMainPenMoved != null) OnMainPenMoved();
            return;
        }
        GameObject placed = current;
        string id = currentId;
        placedItems.Add(placed);
        placedIds.Add(id);

        if (moving)
        {
            moving = false;
            EndPlacing();
            return;
        }

        InventoryManager.Instance.Remove(currentId);
        EndPlacing();
        Quests.Add("decor");

        if (OnItemPlaced != null) OnItemPlaced(id, placed);
        ContinueAfterPlace(id, placed);
    }

    // بعد ما يركّب: لو عنده من نفس الغرض في الشنطة يكمل يركّب اللي بعده،
    // ولو خلص يرجع لنافذة الشنطة (لو فيها شي ثاني)
    void ContinueAfterPlace(string id, GameObject last)
    {
        if (TutorialEnd.Active || WaterSystem.IntroActive || !WaterSystem.TutorialDone) return;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;
        if (inv.GetCount(id) > 0) { StartCoroutine(PlaceNext(id, last)); return; }
        foreach (KeyValuePair<string, int> kv in inv.GetAllCounts())
            if (kv.Value > 0) { BagScreen.Show(); return; }
    }

    System.Collections.IEnumerator PlaceNext(string id, GameObject last)
    {
        yield return null;
        if (IsPlacing || last == null) yield break;
        Bounds lb = GetBounds(last);
        Vector3 from = last.transform.position;
        StartPlacing(id);
        if (current == null || currentId != id) yield break;
        // القطعة الجاية جنب اللي قبلها
        Vector3 np;
        if (id == DirtPaths.Id) np = DirtPaths.SnapToNeighbor(from, current);
        else np = Snap(from + new Vector3(lb.size.x + Cell, 0f, 0f));
        np.y = groundY;
        current.transform.position = np;
    }

    // ===== ✕ =====
    void Cancel()
    {
        if (moving)
        {
            // يرجع لمكانه الأول
            if (current != null)
            {
                ApplyTint(false);
                current.transform.position = moveFrom;
                if (currentId != MainPenId)
                {
                    placedItems.Add(current);
                    placedIds.Add(currentId);
                }
            }
            moving = false;
            EndPlacing();
            return;
        }
        if (current != null) Destroy(current);
        EndPlacing();
        Notify("رجع للشنطة");
    }

    void EndPlacing()
    {
        current = null;
        currentId = null;
        IsPlacing = false;
        IsDragging = false;
        itemSprites = null;
        itemColors = null;

        ShowGrid(false);
        if (arrowsRoot != null) Destroy(arrowsRoot);
        arrows.Clear();
        arrowDirs.Clear();

        if (placeControls != null) placeControls.gameObject.SetActive(false);
    }

    // ===== الأسهم =====
    void BuildArrows()
    {
        arrowsRoot = new GameObject("PlacementArrows");

        Material mat = arrowMaterial;
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));

        AddArrow(Vector3.right, new Color(0.95f, 0.2f, 0.2f), mat);
        AddArrow(Vector3.left, new Color(0.95f, 0.2f, 0.2f), mat);
        AddArrow(Vector3.forward, new Color(0.2f, 0.45f, 1f), mat);
        AddArrow(Vector3.back, new Color(0.2f, 0.45f, 1f), mat);
        if (showGreenArrow)
            AddArrow(Vector3.up, new Color(0.2f, 0.85f, 0.3f), mat);
    }

    void AddArrow(Vector3 dir, Color color, Material mat)
    {
        GameObject go = new GameObject("Arrow");
        go.transform.SetParent(arrowsRoot.transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.positionCount = 3;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = 500;
        lr.numCapVertices = 0;

        Keyframe[] keys = new Keyframe[4];
        keys[0] = new Keyframe(0f, 1f, 0f, 0f);
        keys[1] = new Keyframe(0.649f, 1f, 0f, 0f);
        keys[2] = new Keyframe(0.65f, 3f, 0f, -8.57f);
        keys[3] = new Keyframe(1f, 0f, -8.57f, 0f);
        lr.widthCurve = new AnimationCurve(keys);

        arrows.Add(lr);
        arrowDirs.Add(dir);
    }

    void UpdateArrows()
    {
        Bounds b = GetBounds(current);
        float size = Mathf.Max(b.extents.x, b.extents.z);
        float len = size * arrowLength;
        float bounce = Mathf.Sin(Time.time * 6f) * len * 0.12f;

        for (int i = 0; i < arrows.Count; i++)
        {
            Vector3 d = arrowDirs[i];
            Vector3 start;

            if (d == Vector3.up)
                start = new Vector3(b.center.x, b.max.y + size * arrowGap, b.center.z);
            else
            {
                float edge = Mathf.Abs(d.x) > 0 ? b.extents.x : b.extents.z;
                start = b.center + d * (edge + size * arrowGap);
                start.y = b.center.y + 0.05f;
            }

            start += d * bounce;
            Vector3 end = start + d * len;

            arrows[i].widthMultiplier = len * 0.18f;
            arrows[i].SetPosition(0, start);
            arrows[i].SetPosition(1, Vector3.Lerp(start, end, 0.65f));
            arrows[i].SetPosition(2, end);
        }
    }

    void UpdateButtons()
    {
        if (placeControls == null) return;

        Bounds b = GetBounds(current);
        float minY = float.MaxValue;
        Vector3 c = b.center, e = b.extents;

        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 sp = cam.WorldToScreenPoint(c + new Vector3(e.x * x, e.y * y, e.z * z));
                    if (sp.y < minY) minY = sp.y;
                }

        Vector3 centerScreen = cam.WorldToScreenPoint(c);
        placeControls.position = new Vector3(centerScreen.x, minY - buttonsBelow, 0);
    }

    // ===== الحفظ =====
    public struct PlacedInfo
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public GameObject go;
    }

    // يشيل غرض متركب نهائيًا (مثل الحوض القديم: صار جزء من الحوش)
    public void RemovePlaced(GameObject go)
    {
        int i = placedItems.IndexOf(go);
        if (i < 0) return;
        placedItems.RemoveAt(i);
        placedIds.RemoveAt(i);
        Destroy(go);
    }

    public List<PlacedInfo> GetPlaced()
    {
        List<PlacedInfo> list = new List<PlacedInfo>();
        for (int i = 0; i < placedItems.Count; i++)
        {
            if (placedItems[i] == null) continue;
            PlacedInfo p = new PlacedInfo();
            p.id = placedIds[i];
            p.position = placedItems[i].transform.position;
            p.rotation = placedItems[i].transform.rotation;
            p.go = placedItems[i];
            list.Add(p);
        }
        return list;
    }

    public void ClearPlaced()
    {
        foreach (GameObject g in placedItems)
            if (g != null) Destroy(g);
        placedItems.Clear();
        placedIds.Clear();
    }

    // يرجّع غرض متركب من الحفظ (بدون ما ينقص من الشنطة)
    public GameObject RestorePlaced(string id, Vector3 position, Quaternion rotation)
    {
        if (InventoryManager.Instance == null) return null;

        BagItemData data = InventoryManager.Instance.GetData(id);
        if (data == null || data.worldPrefab == null)
        {
            Debug.LogWarning("ما قدرت أرجّع الغرض المتركب: " + id);
            return null;
        }

        GameObject go = Instantiate(data.worldPrefab, position, rotation);
        ApplyItemScale(id, go);
        placedItems.Add(go);
        placedIds.Add(id);

        if (OnItemPlaced != null) OnItemPlaced(id, go);
        return go;
    }

    // حجم بعض الأغراض (خيمة الراعي أصغر عشان ما تصير أكبر من الحوش)
    static void ApplyItemScale(string id, GameObject go)
    {
        float k = id == "tent" ? 0.6f : 1f;
        if (k != 1f && go != null) go.transform.localScale = go.transform.localScale * k;
    }

    // ===== أدوات =====
    bool ScreenToGround(Vector2 screenPos, out Vector3 point)
    {
        Plane ground = new Plane(Vector3.up, new Vector3(0, groundY, 0));
        Ray ray = cam.ScreenPointToRay(screenPos);
        float dist;
        if (ground.Raycast(ray, out dist))
        {
            point = ray.GetPoint(dist);
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // فوق زر أو نافذة؟ (الصور الصغيرة مثل عداد الناقة فوق الحوش ما تمنع السحب)
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    bool IsPointerOverUI()
    {
        EventSystem es = EventSystem.current;
        if (es == null || !es.IsPointerOverGameObject()) return false;
        Pointer p = Pointer.current;
        if (p == null) return true;
        PointerEventData ev = new PointerEventData(es) { position = p.position.ReadValue() };
        uiHits.Clear();
        es.RaycastAll(ev, uiHits);
        foreach (RaycastResult r in uiHits)
        {
            if (r.gameObject == null) continue;
            if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;      // زر أو نافذة
            if (r.gameObject.GetComponentInParent<GuideShade>() != null) return true;      // ظل الشرح
            if (r.gameObject.GetComponentInParent<TutorialSpotlight>() != null) return true;
        }
        return false;
    }

    void Notify(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
    }

    void Warn(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(msg);
    }
}