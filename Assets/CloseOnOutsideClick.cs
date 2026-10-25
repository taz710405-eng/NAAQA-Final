using UnityEngine;
using UnityEngine.InputSystem;

public class CloseOnOutsideClick : MonoBehaviour
{
    [Tooltip("حدود النافذة. اتركيه فاضي = النافذة نفسها")]
    public RectTransform panel;

    [Tooltip("يتسكر مع النافذة")]
    public GameObject dimBackground;

    private int openedFrame;

    void OnEnable()
    {
        // نتجاهل نفس الضغطة اللي فتحت النافذة
        openedFrame = Time.frameCount;
    }

    void Update()
    {
        if (Time.frameCount <= openedFrame + 1) return;

        Pointer p = Pointer.current;
        if (p == null || !p.press.wasPressedThisFrame) return;

        RectTransform area = panel != null ? panel : transform as RectTransform;
        if (area == null) return;

        Canvas canvas = area.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        Vector2 pos = p.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(area, pos, uiCam))
            Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
    }
}