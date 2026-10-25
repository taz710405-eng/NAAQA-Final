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

    // الأغراض اللي تركبت قبل (ما نركب فوقها بعد)
    private readonly List<GameObject> placedItems = new List<GameObject>();

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

        current = Instantiate(data.worldPrefab, pos, data.worldPrefab.transform.rotation);
        currentId = id;
        IsPlacing = true;

        itemSprites = current.GetComponentsInChildren<SpriteRenderer>();
        itemColors = new Color[itemSprites.Length];
        for (int i = 0; i < itemSprites.Length; i++) itemColors[i] = itemSprites[i].color;

        BuildArrows();
        if (placeControls != null) placeControls.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsPlacing || current == null) return;

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
                Vector3 np = g + dragOffset;
                np.y = groundY;
                current.transform.position = np;
            }
        }

        if (p.press.wasReleasedThisFrame) IsDragging = false;
    }

    void LateUpdate()
    {
        if (!IsPlacing || current == null) return;

        canPlace = !IsOverlapping();
        ApplyTint(!canPlace);

        UpdateArrows();
        UpdateButtons();
    }

    // ===== فحص التداخل =====
    bool IsOverlapping()
    {
        Bounds mine = Shrink(GetBounds(current));

        if (blockers != null)
            foreach (GameObject b in blockers)
                if (b != null && b.activeInHierarchy && OverlapXZ(mine, Shrink(GetBounds(b))))
                    return true;

        foreach (GameObject b in placedItems)
            if (b != null && OverlapXZ(mine, Shrink(GetBounds(b))))
                return true;

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
            Warn("ما تقدر تحطها هنا! حركها لمكان فاضي");
            return;
        }

        ApplyTint(false);
        InventoryManager.Instance.Remove(currentId);
        GameObject placed = current;
        string id = currentId;
        placedItems.Add(placed);

        EndPlacing();
        Notify("تم التركيب!");

        if (OnItemPlaced != null) OnItemPlaced(id, placed);
    }

    // ===== ✕ =====
    void Cancel()
    {
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

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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