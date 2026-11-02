using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// ===== تحريك الكاميرا =====
// الجوال: إصبع واحد يسحب الخريطة، وإصبعين للتقريب
// الكمبيوتر: اسحب بالماوس، وعجلة الماوس للتقريب
// الخريطة تمشي مع الإصبع بالضبط، وما تطلع برا الحدود
public class CameraController : MonoBehaviour
{
    public float moveSpeed = 0.02f;     // (قديم، ما عاد نحتاجه: السحب صار يتبع الإصبع)
    public float zoomSpeed = 0.01f;     // سرعة التقريب بالإصبعين
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("الكمبيوتر")]
    public float scrollZoomSpeed = 1f;
    public float dragThreshold = 12f;   // كم بكسل لازم يتحرك قبل ما نعتبره سحب (عشان الضغط العادي يشتغل)

    [Header("الحدود (من مكان الكاميرا في البداية)")]
    public float limitX = 12f;
    public float limitZ = 12f;
    public float groundY = 0f;

    private Camera cam;
    private Vector3 startPos;

    private bool pressing = false;
    private bool dragging = false;
    private bool blocked = false;       // الضغطة بدأت فوق نافذة
    private Vector2 pressPos;
    private Vector2 lastPos;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInParent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null) startPos = cam.transform.position;
    }

    void Update()
    {
        if (cam == null) return;

        // وقت سحب غرض في وضع التركيب، الكاميرا توقف
        if (PlacementManager.IsDragging)
        {
            pressing = false;
            dragging = false;
            return;
        }

        if (Touchscreen.current != null && ActiveTouches() > 0)
            HandleTouch();
        else if (Mouse.current != null)
            HandleMouse();
    }

    // ===== الجوال =====
    void HandleTouch()
    {
        var touches = Touchscreen.current.touches;
        int count = ActiveTouches();

        // إصبعين = تقريب
        if (count >= 2)
        {
            dragging = false;
            pressing = false;

            Vector2 c1 = touches[0].position.ReadValue();
            Vector2 c2 = touches[1].position.ReadValue();
            Vector2 p1 = c1 - touches[0].delta.ReadValue();
            Vector2 p2 = c2 - touches[1].delta.ReadValue();

            float diff = Vector2.Distance(c1, c2) - Vector2.Distance(p1, p2);
            Zoom(-diff * zoomSpeed);
            return;
        }

        Vector2 pos = touches[0].position.ReadValue();
        PointerStep(touches[0].press.isPressed, pos);
    }

    int ActiveTouches()
    {
        int n = 0;
        foreach (var t in Touchscreen.current.touches)
            if (t.press.isPressed) n++;
        return n;
    }

    // ===== الكمبيوتر =====
    void HandleMouse()
    {
        Mouse m = Mouse.current;

        float scroll = m.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(m.position.ReadValue()))
            Zoom(-Mathf.Sign(scroll) * scrollZoomSpeed);

        bool down = m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed;
        PointerStep(down, m.position.ReadValue());
    }

    // ===== السحب (مشترك) =====
    void PointerStep(bool down, Vector2 pos)
    {
        if (down && !pressing)
        {
            pressing = true;
            dragging = false;
            blocked = IsOverUI(pos);
            pressPos = pos;
            lastPos = pos;
            return;
        }

        if (!down)
        {
            pressing = false;
            dragging = false;
            return;
        }

        if (blocked) return;

        if (!dragging && (pos - pressPos).magnitude >= dragThreshold)
            dragging = true;

        if (dragging)
        {
            Vector3 a, b;
            if (ScreenToGround(lastPos, out a) && ScreenToGround(pos, out b))
            {
                Vector3 p = cam.transform.position + (a - b);
                cam.transform.position = Clamp(p);
            }
        }

        lastPos = pos;
    }

    void Zoom(float amount)
    {
        if (cam.orthographic)
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + amount, minZoom, maxZoom);
        else
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + amount * 3f, 20f, 70f);
    }

    Vector3 Clamp(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, startPos.x - limitX, startPos.x + limitX);
        p.z = Mathf.Clamp(p.z, startPos.z - limitZ, startPos.z + limitZ);
        p.y = startPos.y;
        return p;
    }

    bool ScreenToGround(Vector2 screen, out Vector3 point)
    {
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        Ray ray = cam.ScreenPointToRay(screen);
        float d;
        if (ground.Raycast(ray, out d))
        {
            point = ray.GetPoint(d);
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    // فوق نافذة أو زر؟ (ظل الشرح ما نحسبه)
    bool IsOverUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = pos;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (RaycastResult r in results)
        {
            if (r.gameObject.GetComponentInParent<TutorialSpotlight>() != null) continue;
            if (r.gameObject.GetComponentInParent<GuideShade>() != null) continue;   // الظل يمنع الأزرار بس، مو تحريك الخريطة
            return true;
        }
        return false;
    }
}
