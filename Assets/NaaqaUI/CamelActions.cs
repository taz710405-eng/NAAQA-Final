using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RTLTMPro;
using TMPro;

// ===== أزرار فوق الناقة =====
// لما تضغط على ناقة: يطلع فوقها زرين صغار، يمين «أطعم» ويسار «معلومات»، واسمها بينهم.
// يختفون لو ضغطت برا، أو بعد شوي.
public class CamelActions : MonoBehaviour
{
    static CamelActions instance;
    Canvas canvas;
    RectTransform root;
    RTLTextMeshPro nameText;
    Camel camel;
    float shownAt;
    GameObject datesBtn;

    public static void Show(Camel c)
    {
        if (c == null) return;
        if (instance == null)
        {
            instance = new GameObject("CamelActions").AddComponent<CamelActions>();
            instance.Build();
        }
        instance.camel = c;
        instance.shownAt = Time.unscaledTime;
        instance.nameText.text = CamelProfiles.Get(c).name;
        instance.root.gameObject.SetActive(true);
        // زر التمر: بس لو عنده تمر وما أكلت تمرة في هالدورة
        if (instance.datesBtn != null) instance.datesBtn.SetActive(c.CanUseDate);
        instance.Follow();
    }

    public static void Hide()
    {
        if (instance != null) instance.root.gameObject.SetActive(false);
    }

    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("CamelActionsCanvas", 60);
        canvas.transform.SetParent(transform, false);

        root = NaaqaUI.Rect("Root", canvas.transform);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(300, 170);

        // الاسم بالنص
        Image np = NaaqaUI.Img(root, "Name", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.88f), 12f);
        NaaqaUI.Place(np.rectTransform, new Vector2(0.5f, 0f), new Vector2(130, 34), new Vector2(0, 0));
        nameText = NaaqaUI.Text(np.transform, "T", "", 18, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(nameText.rectTransform, 4, 4, 0, 2);
        nameText.enableAutoSizing = true; nameText.fontSizeMin = 12; nameText.fontSizeMax = 18;

        // يمين: أطعم
        Button feed = NaaqaUI.IconButton(root, "feed", "أطعم", 64, () => { Camel c = camel; Hide(); if (c != null) c.Feed(); });
        NaaqaUI.Place((RectTransform)feed.transform, new Vector2(1, 0f), new Vector2(64, 64), new Vector2(0, 40));

        // فوق: تمر (يطلع بس لو عندك تمر)
        Button dates = NaaqaUI.IconButton(root, "dates_farm", "تمر", 56, () => { Camel c = camel; Hide(); if (c != null) c.FeedDates(); });
        NaaqaUI.Place((RectTransform)dates.transform, new Vector2(0.5f, 0f), new Vector2(56, 56), new Vector2(0, 74));
        datesBtn = dates.gameObject;

        // يسار: معلومات
        Button info = NaaqaUI.IconButton(root, "camel_market", "معلومات", 64, () => { Camel c = camel; Hide(); CamelInfoScreen.Show(c); });
        NaaqaUI.Place((RectTransform)info.transform, new Vector2(0, 0f), new Vector2(64, 64), new Vector2(0, 40));

        root.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!root.gameObject.activeSelf) return;
        if (camel == null || !camel.gameObject.activeInHierarchy || PlacementManager.IsPlacing || EditMode.Active
            || Time.unscaledTime - shownAt > 6f) { Hide(); return; }

        // ضغطة برا الأزرار وبرا الناقة تسكّرها
        Pointer p = Pointer.current;
        if (p != null && p.press.wasPressedThisFrame && Time.unscaledTime - shownAt > 0.15f)
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUi) { Hide(); return; }
        }
        Follow();
    }

    void Follow()
    {
        Camera cam = Camera.main;
        if (cam == null || camel == null) return;
        // فوق راس الناقة
        Bounds b = new Bounds(camel.transform.position, Vector3.zero);
        foreach (SpriteRenderer sr in camel.GetComponentsInChildren<SpriteRenderer>())
            if (sr.enabled && sr.gameObject.name != "MilkIcon") b.Encapsulate(sr.bounds);
        Vector3 top = new Vector3(b.center.x, b.center.y, b.max.z);
        Vector3 sp = cam.WorldToScreenPoint(top);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out local);
        root.anchoredPosition = local + new Vector2(0, 6);
    }
}
