using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 0.02f;
    public float zoomSpeed = 0.01f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;

        // إصبع واحد = تحريك الخريطة
        if (touches.Count > 0 && touches[0].press.isPressed)
        {
            Vector2 delta = touches[0].delta.ReadValue();

            if (delta != Vector2.zero)
            {
                transform.position -= new Vector3(
                    delta.x * moveSpeed,
                    0,
                    delta.y * moveSpeed
                );
            }
        }

        // إصبعين = Zoom
        if (touches.Count > 1 &&
            touches[0].press.isPressed &&
            touches[1].press.isPressed)
        {
            Vector2 current1 = touches[0].position.ReadValue();
            Vector2 current2 = touches[1].position.ReadValue();

            Vector2 previous1 = current1 - touches[0].delta.ReadValue();
            Vector2 previous2 = current2 - touches[1].delta.ReadValue();

            float currentDistance = Vector2.Distance(current1, current2);
            float previousDistance = Vector2.Distance(previous1, previous2);

            float difference = currentDistance - previousDistance;

            cam.orthographicSize -= difference * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize,
                minZoom,
                maxZoom
            );
        }
    }
}