using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// يحرك الكاميرا بالسحب ويقرب ويبعد (ماوس وعجلة، أو إصبع وقرصة بإصبعين)
public class CameraPanZoom : MonoBehaviour
{
    public float minSize = 3f;
    public float maxSize = 15f;
    public float panLimit = 10f;

    Camera cam;
    Vector3 startPos;
    List<Vector2> pts = new List<Vector2>();
    List<Vector2> prev = new List<Vector2>();
    bool blocked;

    void Start() { cam = GetComponent<Camera>(); startPos = transform.position; }

    void Update()
    {
        float scroll = ReadPointers();
        if (pts.Count == 0) blocked = false;
        else if (prev.Count == 0) blocked = OverUI();

        if (!blocked && pts.Count == prev.Count)
        {
            if (pts.Count == 1)
                transform.position += cam.ScreenToWorldPoint(prev[0]) - cam.ScreenToWorldPoint(pts[0]);
            else if (pts.Count == 2)
            {
                float a = Vector2.Distance(prev[0], prev[1]);
                float b = Vector2.Distance(pts[0], pts[1]);
                if (b > 1f) Zoom(a / b);
            }
        }
        if (scroll != 0f && !OverUI()) Zoom(scroll > 0f ? 0.9f : 1.1f);

        prev.Clear();
        prev.AddRange(pts);
        Clamp();
    }

    float ReadPointers()
    {
        pts.Clear();
        float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
            foreach (var t in Touchscreen.current.touches)
                if (t.press.isPressed) pts.Add(t.position.ReadValue());
        if (pts.Count == 0 && Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed) pts.Add(Mouse.current.position.ReadValue());
            scroll = Mouse.current.scroll.ReadValue().y;
        }
#else
        for (int i = 0; i < Input.touchCount; i++) pts.Add(Input.GetTouch(i).position);
        if (pts.Count == 0)
        {
            if (Input.GetMouseButton(0)) pts.Add(Input.mousePosition);
            scroll = Input.mouseScrollDelta.y;
        }
#endif
        return scroll;
    }

    bool OverUI()
    {
        if (EventSystem.current == null) return false;
#if !ENABLE_INPUT_SYSTEM
        if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
        return EventSystem.current.IsPointerOverGameObject();
    }

    void Zoom(float f)
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * f, minSize, maxSize);
    }

    void Clamp()
    {
        Vector3 d = transform.position - startPos;
        d.x = Mathf.Clamp(d.x, -panLimit, panLimit);
        d.y = Mathf.Clamp(d.y, -panLimit, panLimit);
        d.z = Mathf.Clamp(d.z, -panLimit, panLimit);
        transform.position = startPos + d;
    }
}