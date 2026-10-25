using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TutorialSpotlight : MonoBehaviour
{
    public static TutorialSpotlight Instance;

    [System.Serializable]
    public class Step
    {
        public string note;
        public Transform[] targets;
        public float radius = 0f;

        [Header("Optional: needs milk first")]
        public int minMilk = 0;
        public Transform[] notEnoughTargets;
    }

    [Header("Steps (same order as tasks)")]
    public Step[] steps =
    {
        new Step { note = "اضغط على الإبل" },
        new Step { note = "أطعم الإبل" },
        new Step { note = "اجمع الحليب" },
        new Step { note = "سلّم الطلب", minMilk = 2 },
        new Step { note = "طوّر العزبة" }
    };

    [Header("Windows (hide when one is open, unless target is inside)")]
    public GameObject[] windows;

    [Header("Look")]
    public Color shadeColor = new Color(0f, 0f, 0f, 0.65f);
    public float worldRadius = 140f;
    public float uiPadding = 30f;
    public float minRadius = 70f;
    public float moveSpeed = 12f;
    public float fadeSpeed = 6f;
    public float pulse = 0.06f;
    public bool blockClicks = true;

    [Header("World Targets (camels etc.)")]
    public bool useRendererBounds = true;
    public float worldPadding = 15f;
    public Vector2 worldOffset = Vector2.zero;

    [Header("Hand (optional)")]
    public Sprite handSprite;
    public Vector2 handSize = new Vector2(90f, 90f);
    public Vector2 handOffset = new Vector2(70f, -70f);
    public float handBounce = 15f;

    private RectTransform root;
    private CanvasGroup group;
    private RectTransform hole, top, bottom, left, right, hand;
    private int currentStep = -1;
    private Vector2 curPos;
    private float curRadius;
    private bool hasPos = false;

    // آخر قياس صحيح لكل هدف (عشان لو الصورة اختفت لحظة ما تقفز الدائرة)
    private class Memory { public Vector2 offset; public float radius; }
    private readonly Dictionary<Transform, Memory> memory = new Dictionary<Transform, Memory>();

    void Awake()
    {
        Instance = this;
        root = GetComponent<RectTransform>();

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        hole = MakePart("Hole", MakeHoleSprite());
        Image holeImage = hole.GetComponent<Image>();
        holeImage.alphaHitTestMinimumThreshold = 0.5f;

        top = MakePart("Top", null);
        bottom = MakePart("Bottom", null);
        left = MakePart("Left", null);
        right = MakePart("Right", null);

        if (handSprite != null)
        {
            hand = MakePart("Hand", handSprite);
            Image h = hand.GetComponent<Image>();
            h.color = Color.white;
            h.raycastTarget = false;
            h.preserveAspect = true;
            hand.sizeDelta = handSize;
        }
    }

    public void ShowStep(int index)
    {
        if (currentStep != index) hasPos = false;
        currentStep = index;
    }

    public void Hide()
    {
        currentStep = -1;
    }

    void LateUpdate()
    {
        Vector2 targetPos = Vector2.zero;
        float targetRadius = 0f;
        bool show = false;

        if (currentStep >= 0)
            show = FindTarget(out targetPos, out targetRadius);

        float goal = show ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, goal, Time.unscaledDeltaTime * fadeSpeed);
        group.blocksRaycasts = show && blockClicks && group.alpha > 0.5f;

        if (!show)
        {
            hasPos = false;
            return;
        }

        if (!hasPos)
        {
            curPos = targetPos;
            curRadius = targetRadius;
            hasPos = true;
        }
        else
        {
            float k = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
            curPos = Vector2.Lerp(curPos, targetPos, k);
            curRadius = Mathf.Lerp(curRadius, targetRadius, k);
        }

        float r = curRadius * (1f + Mathf.Sin(Time.unscaledTime * 3f) * pulse);
        Layout(curPos, r);

        if (hand != null)
        {
            float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * handBounce;
            hand.anchoredPosition = curPos + handOffset + new Vector2(0f, bounce);
        }
    }

    bool FindTarget(out Vector2 pos, out float radius)
    {
        pos = Vector2.zero;
        radius = minRadius;

        if (steps == null || currentStep >= steps.Length) return false;
        Step step = steps[currentStep];
        if (step == null) return false;

        Transform[] list = step.targets;

        if (step.minMilk > 0 && GameManager.Instance != null &&
            GameManager.Instance.milkCount < step.minMilk &&
            step.notEnoughTargets != null && step.notEnoughTargets.Length > 0)
        {
            list = step.notEnoughTargets;
        }

        if (list == null) return false;

        GameObject openWindow = null;
        if (windows != null)
        {
            foreach (GameObject w in windows)
            {
                if (w != null && w.activeInHierarchy)
                {
                    openWindow = w;
                    break;
                }
            }
        }

        for (int i = list.Length - 1; i >= 0; i--)
        {
            Transform t = list[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (openWindow != null && !t.IsChildOf(openWindow.transform)) continue;

            if (Measure(t, out pos, out radius))
            {
                if (step.radius > 0f) radius = step.radius;
                radius = Mathf.Max(radius, minRadius);
                return true;
            }
        }

        return false;
    }

    bool Measure(Transform t, out Vector2 localPos, out float radius)
    {
        localPos = Vector2.zero;
        radius = worldRadius;

        Canvas myCanvas = GetComponentInParent<Canvas>();
        Camera myCam = (myCanvas == null || myCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : myCanvas.worldCamera;

        RectTransform rt = t as RectTransform;
        if (rt != null && rt.GetComponentInParent<Canvas>() != null)
        {
            Canvas tCanvas = rt.GetComponentInParent<Canvas>();
            Camera tCam = tCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tCanvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(tCam, corners[i]);
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, myCam, out local)) return false;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            localPos = (min + max) * 0.5f;
            Vector2 size = max - min;
            radius = Mathf.Max(size.x, size.y) * 0.5f + uiPadding;
            return true;
        }

        Camera cam = Camera.main;
        if (cam == null) return false;

        // مكان مركز الأوبجكت على الشاشة
        Vector3 sp = cam.WorldToScreenPoint(t.position);
        if (sp.z < 0f) return false;
        Vector2 pivotLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, myCam, out pivotLocal)) return false;

        if (useRendererBounds)
        {
            Vector2 rPos;
            float rRad;
            if (MeasureRenderers(t, cam, myCam, out rPos, out rRad))
            {
                // نحفظ القياس الصحيح
                Memory m;
                if (!memory.TryGetValue(t, out m)) { m = new Memory(); memory[t] = m; }
                m.offset = rPos - pivotLocal;
                m.radius = rRad;

                localPos = rPos + worldOffset;
                radius = rRad;
                return true;
            }

            // الصورة اختفت لحظة: نستخدم آخر قياس محفوظ بدل ما نقفز
            Memory saved;
            if (memory.TryGetValue(t, out saved))
            {
                localPos = pivotLocal + saved.offset + worldOffset;
                radius = saved.radius;
                return true;
            }
        }

        localPos = pivotLocal + worldOffset;
        radius = worldRadius;
        return true;
    }

    bool MeasureRenderers(Transform t, Camera cam, Camera uiCam, out Vector2 localPos, out float radius)
    {
        localPos = Vector2.zero;
        radius = worldRadius;

        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds b = new Bounds();

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }

        if (!found) return false;

        Vector3 c = b.center;
        Vector3 e = b.extents;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = c + new Vector3(e.x * x, e.y * y, e.z * z);
            Vector3 sp = cam.WorldToScreenPoint(corner);
            if (sp.z < 0f) return false;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, uiCam, out local)) return false;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        localPos = (min + max) * 0.5f;
        Vector2 size = max - min;
        radius = Mathf.Max(size.x, size.y) * 0.5f + worldPadding;
        return true;
    }

    void Layout(Vector2 p, float r)
    {
        Rect area = root.rect;
        float m = 50f;
        float xMin = area.xMin - m, xMax = area.xMax + m;
        float yMin = area.yMin - m, yMax = area.yMax + m;

        Place(hole, p, new Vector2(r * 2f, r * 2f));

        float topH = Mathf.Max(0f, yMax - (p.y + r));
        Place(top, new Vector2((xMin + xMax) * 0.5f, p.y + r + topH * 0.5f), new Vector2(xMax - xMin, topH));

        float botH = Mathf.Max(0f, (p.y - r) - yMin);
        Place(bottom, new Vector2((xMin + xMax) * 0.5f, yMin + botH * 0.5f), new Vector2(xMax - xMin, botH));

        float leftW = Mathf.Max(0f, (p.x - r) - xMin);
        Place(left, new Vector2(xMin + leftW * 0.5f, p.y), new Vector2(leftW, r * 2f));

        float rightW = Mathf.Max(0f, xMax - (p.x + r));
        Place(right, new Vector2(p.x + r + rightW * 0.5f, p.y), new Vector2(rightW, r * 2f));
    }

    void Place(RectTransform rt, Vector2 center, Vector2 size)
    {
        rt.anchoredPosition = center;
        rt.sizeDelta = size;
    }

    RectTransform MakePart(string name, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = shadeColor;
        img.raycastTarget = true;

        return rt;
    }

    Sprite MakeHoleSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float r = size * 0.5f;
        float edge = size * 0.06f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = Mathf.Clamp01((d - (r - edge)) / edge);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
}