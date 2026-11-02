using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RTLTMPro;
using TMPro;

// ===== وضع التعديل =====
// تضغط زر «تعديل»، وبعدها تضغط على أي غرض متركب في الحوش (خيمة، نخلة، مزرعة...)
// وتحركه بالأسهم مثل التركيب، أو ترجّعه للشنطة.
public class EditMode : MonoBehaviour
{
    static EditMode instance;
    public static bool Active { get { return instance != null && instance.on; } }

    bool on;
    Canvas canvas;
    RTLTextMeshPro hint;
    GameObject storeBtn;

    public static void Toggle()
    {
        if (Active) Exit(); else Enter();
    }

    public static void Enter()
    {
        if (PlacementManager.Instance == null) return;
        if (instance == null)
        {
            instance = new GameObject("EditMode").AddComponent<EditMode>();
            instance.Build();
        }
        instance.on = true;
        instance.canvas.gameObject.SetActive(true);
    }

    public static void Exit()
    {
        if (instance == null) return;
        if (PlacementManager.IsMoving) return;   // خلّص التحريك أول (✓ أو ✕)
        instance.on = false;
        instance.canvas.gameObject.SetActive(false);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("EditModeCanvas", 640);
        canvas.transform.SetParent(transform, false);

        Image bar = NaaqaUI.Img(canvas.transform, "Bar", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 30f);
        bar.raycastTarget = true;
        NaaqaUI.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(1100, 110), new Vector2(0, 26));
        Image line = NaaqaUI.Img(bar.transform, "Line", NaaqaUI.RoundedOutline(), new Color(1f, 0.8f, 0.5f, 0.4f), 30f);
        NaaqaUI.Stretch(line.rectTransform);

        Image ic = NaaqaUI.IconImg(bar.transform, "edit", 80);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(80, 80), new Vector2(-16, 0));

        hint = NaaqaUI.Text(bar.transform, "Hint", "", 28, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        hint.rectTransform.anchorMin = Vector2.zero; hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(460, 0); hint.rectTransform.offsetMax = new Vector2(-110, 0);

        RTLTextMeshPro t;
        Button done = NaaqaUI.MakeButton(bar.transform, "Done", "خلصت", NaaqaUI.ButtonStyle.Primary, 28, out t);
        NaaqaUI.Place((RectTransform)done.transform, new Vector2(0, 0.5f), new Vector2(200, 74), new Vector2(18, 0));
        done.onClick.AddListener(Exit);

        Button store = NaaqaUI.MakeButton(bar.transform, "Store", "رجّعه للشنطة", NaaqaUI.ButtonStyle.Secondary, 25, out t);
        NaaqaUI.Place((RectTransform)store.transform, new Vector2(0, 0.5f), new Vector2(220, 74), new Vector2(230, 0));
        store.onClick.AddListener(() => { if (PlacementManager.Instance != null) PlacementManager.Instance.StoreMoving(); });
        storeBtn = store.gameObject;
    }

    void Update()
    {
        if (!on) return;
        bool moving = PlacementManager.IsMoving;
        bool canStore = moving && !PlacementManager.IsMovingMainPen;
        if (storeBtn.activeSelf != canStore) storeBtn.SetActive(canStore);
        hint.text = moving ? "حرّكه للمكان اللي تبيه واضغط زر الصح" : "اضغط على أي غرض أو حوش عشان تحركه";

        if (PlacementManager.IsPlacing || PlacementManager.Instance == null) return;
        Pointer p = Pointer.current;
        if (p == null || !p.press.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 sp = p.position.ReadValue();
        Camera cam = Camera.main;
        GameObject go = PlacementManager.Instance.PlacedAt(sp);

        // الحوش الرئيسي: لو الضغطة عليه (صورته كبيرة، فنختار الأقرب لنص الضغطة)
        PenVisual main = PenVisual.Instance;
        bool onMain = false;
        if (main != null && cam != null)
        {
            foreach (RaycastHit h in Physics.RaycastAll(cam.ScreenPointToRay(sp)))
                if (h.collider.GetComponentInParent<PenVisual>() == main) { onMain = true; break; }
            if (!onMain)
            {
                // احتياط: لو ما له collider نشيك على حدود صورته
                SpriteRenderer msr = main.GetComponent<SpriteRenderer>();
                if (msr != null)
                {
                    Bounds bb = msr.bounds;
                    Vector3 a = cam.WorldToScreenPoint(bb.min), c = cam.WorldToScreenPoint(bb.max);
                    Rect r = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
                    float sx = r.width * 0.3f, sy = r.height * 0.3f;   // الوسط بس (الصورة فيها فراغ)
                    onMain = Rect.MinMaxRect(r.xMin + sx, r.yMin + sy, r.xMax - sx, r.yMax - sy).Contains(sp);
                }
            }
        }

        if (onMain && go != null)
        {
            float dMain = Vector2.Distance(sp, cam.WorldToScreenPoint(main.transform.position));
            float dGo = Vector2.Distance(sp, cam.WorldToScreenPoint(go.transform.position));
            if (dMain < dGo) go = null;
        }
        if (go != null) { PlacementManager.Instance.BeginMove(go); return; }
        if (onMain)
        {
            bool ok = PlacementManager.Instance.BeginMoveMainPen(main.gameObject);
            Debug.Log("تحريك الحوش الرئيسي: " + (ok ? "بدأ" : "ما بدأ"));
        }
    }
}
