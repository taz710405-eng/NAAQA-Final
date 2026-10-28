using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
// يشغل حركة المشي لما الجمل يتحرك، ويرجع للوقوف لما يوقف، ويلف وجهه لجهة المشي
// ولما اللاعب يضغط على الجمل (وقت "أكلت الناقة") يشغل حركة الأكل (Trigger اسمه Eat في الـ Animator)
public class CamelAnimSwitch : MonoBehaviour
{
    public GameObject eatIcon;      // أيقونة العلف فوق الجمل (تطلع وقت الأكل بس)
    public float eatIconTime = 1.5f;

    Animator anim; Vector3 lastPos; float stillTime; float baseScaleX;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (eatIcon != null) eatIcon.SetActive(false);
        lastPos = transform.position;
        baseScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPos; lastPos = transform.position;
        if (delta.sqrMagnitude > 0.0000001f) stillTime = 0f; else stillTime += Time.deltaTime;
        bool moving = stillTime < 0.15f;
        if (anim != null) anim.SetBool("IsWalking", moving);

        if (Mathf.Abs(delta.x) > 0.0001f)
        {
            Vector3 s = transform.localScale;
            s.x = delta.x > 0 ? baseScaleX : -baseScaleX;
            transform.localScale = s;
        }

        Vector2 pos;
        if (PressedThisFrame(out pos) && ClickedOnMe(pos)) PlayEat();
    }

    // تقدر البنات تناديها من سكربتاتهم بعد: GetComponent<CamelAnimSwitch>().PlayEat();
    public void PlayEat()
    {
        if (anim != null) anim.SetTrigger("Eat");
        if (eatIcon != null)
        {
            eatIcon.SetActive(true);
            CancelInvoke(nameof(HideEatIcon));
            Invoke(nameof(HideEatIcon), eatIconTime);
        }
    }

    void HideEatIcon()
    {
        if (eatIcon != null) eatIcon.SetActive(false);
    }

    bool PressedThisFrame(out Vector2 pos)
    {
        pos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pos = Mouse.current.position.ReadValue(); return true; }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        { pos = Input.GetTouch(0).position; return true; }
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
        return false;
    }

    bool ClickedOnMe(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(screenPos);
        Transform hit = null;
        RaycastHit h3;
        if (Physics.Raycast(ray, out h3)) hit = h3.transform;
        else
        {
            RaycastHit2D h2 = Physics2D.GetRayIntersection(ray);
            if (h2.collider != null) hit = h2.transform;
        }
        if (hit == null) return false;
        return hit == transform || hit.IsChildOf(transform) || transform.IsChildOf(hit);
    }
}