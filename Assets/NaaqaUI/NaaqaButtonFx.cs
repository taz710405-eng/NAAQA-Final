using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// حركة خفيفة للأزرار: تكبر شوي لما يمر الماوس، وتصغر لما تنضغط
public class NaaqaButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.03f;
    public float pressScale = 0.96f;
    public float speed = 14f;

    private Vector3 baseScale;
    private float target = 1f;
    private float current = 1f;
    private bool hover, down;
    private Selectable sel;

    void Awake()
    {
        baseScale = transform.localScale;
        sel = GetComponent<Selectable>();
    }

    void OnDisable()
    {
        hover = down = false;
        current = target = 1f;
        transform.localScale = baseScale;
    }

    void Update()
    {
        bool on = sel == null || sel.interactable;
        target = !on ? 1f : down ? pressScale : hover ? hoverScale : 1f;
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        transform.localScale = baseScale * current;
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e) { down = true; }
    public void OnPointerUp(PointerEventData e) { down = false; }
}
