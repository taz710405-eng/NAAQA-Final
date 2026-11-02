using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ===== نوافذ الشرح القديمة بالشكل الجديد =====
// نافذة الطلب، ونافذة المهام، ونافذة التطوير (اللي في الشرح)، وشريط المهمة فوق،
// وأزرار الصح والإكس وقت التركيب. كلها تصير غامقة شفافة بحد ذهبي وأزرار سدو.
public class OldUiSkin : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Run();
        SceneManager.sceneLoaded += (s, m) => Run();
    }

    static void Run()
    {
        Safe(() =>
        {
            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null) return;
            if (ui.ordersWindow != null) Window(ui.ordersWindow.transform);
            if (ui.tasksWindow != null) Window(ui.tasksWindow.transform);
            if (ui.upgradeWindow != null)
            {
                Window(ui.upgradeWindow.transform);
                Transform req = ui.upgradeWindow.transform.Find("RequiredLabel");
                TMP_Text rt = req != null ? req.GetComponent<TMP_Text>() : null;
                if (rt != null) rt.text = "التكلفة: مجانًا";
            }
        });
        Safe(() =>
        {
            TaskBanner tb = Object.FindAnyObjectByType<TaskBanner>(FindObjectsInactive.Include);
            if (tb != null) Banner(tb);
        });
        Safe(() =>
        {
            PlacementManager pm = Object.FindAnyObjectByType<PlacementManager>(FindObjectsInactive.Include);
            if (pm == null) return;
            RoundIcon(pm.confirmButton, "ok");
            RoundIcon(pm.cancelButton, "cancel");
        });
    }

    static void Safe(System.Action a)
    {
        try { a(); } catch (System.Exception e) { Debug.LogWarning("شكل النوافذ القديمة: " + e.Message); }
    }

    static void Window(Transform w)
    {
        if (w.GetComponent<OldUiSkinDone>() != null) return;
        w.gameObject.AddComponent<OldUiSkinDone>();

        Image bg = w.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = NaaqaUI.Rounded();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.09f, 0.06f, 0.035f, 0.94f);
            bg.pixelsPerUnitMultiplier = (bg.sprite.border.x - 1f) / 34f;
            Image line = NaaqaUI.Img(w, "GoldLine", NaaqaUI.RoundedOutline(), new Color(0.84f, 0.66f, 0.36f, 0.9f), 34f);
            NaaqaUI.Stretch(line.rectTransform);
            line.transform.SetAsFirstSibling();
        }

        foreach (Transform c in w.GetComponentsInChildren<Transform>(true))
        {
            if (c == w) continue;
            Button b = c.GetComponent<Button>();
            string n = c.name;
            if (b != null && n.Contains("Close")) { RoundIcon(b, "close"); continue; }
            if (b != null)
            {
                Image bi = c.GetComponent<Image>();
                SaduFit.Apply(bi, "primary", b);
                ColorBlock cb = b.colors; cb.disabledColor = Color.white; b.colors = cb;
                continue;
            }
            Image img = c.GetComponent<Image>();
            if (img != null && (n.StartsWith("check") || n == "CheckIcon"))
            {
                img.sprite = NaaqaUI.Icon("ok"); img.preserveAspect = true; img.color = Color.white;
            }
        }

        // كل الكلام كريمي واضح
        foreach (TMP_Text t in w.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = NaaqaUI.Cream;
            if (t.transform.parent != null && t.transform.parent.GetComponent<Button>() != null)
            {
                t.fontStyle = FontStyles.Bold;
                t.enableAutoSizing = true; t.fontSizeMax = Mathf.Max(24f, t.fontSize); t.fontSizeMin = 14f;
                RectTransform rt = t.rectTransform;
                rt.anchorMin = new Vector2(0, 0.2f); rt.anchorMax = new Vector2(1, 0.8f);
                rt.offsetMin = new Vector2(16, 0); rt.offsetMax = new Vector2(-16, 0);
                // كان مايل لطرف: نخليه بالنص وبحجمه الطبيعي
                rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
                rt.pivot = new Vector2(0.5f, 0.5f);
                t.alignment = TextAlignmentOptions.Center;
                t.margin = Vector4.zero;
            }
        }
    }

    static void Banner(TaskBanner tb)
    {
        if (tb.GetComponent<OldUiSkinDone>() != null) return;
        tb.gameObject.AddComponent<OldUiSkinDone>();
        Image bg = tb.GetComponent<Image>();
        if (bg != null) NaaqaUI.DarkPill(bg, 36f);
        foreach (TMP_Text t in tb.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = NaaqaUI.Cream;
            t.fontStyle = FontStyles.Bold;
        }
        // كلام المهمة كان صغير (مصغّر ٠٫٦ في المشهد): نرجعه لحجمه ونوسّع الشريط
        RectTransform br = (RectTransform)tb.transform;
        br.sizeDelta = new Vector2(780f, Mathf.Max(br.sizeDelta.y, 100f));
        if (tb.taskText != null) NaaqaUI.BigText(tb.taskText, 140f, 30f, 44f);
        if (tb.checkIcon != null)
        {
            Image ci = tb.checkIcon.GetComponent<Image>();
            if (ci != null) { ci.sprite = NaaqaUI.Icon("ok"); ci.preserveAspect = true; ci.color = Color.white; }
        }
    }

    // زر دايري بأيقونة سدو (بدون كلام أو رسمة قديمة جواه)
    static void RoundIcon(Button b, string icon)
    {
        if (b == null) return;
        Sprite s = NaaqaUI.Icon(icon);
        if (s == null) return;
        Image img = b.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = s; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
            b.targetGraphic = img;
        }
        RectTransform rt = (RectTransform)b.transform;
        float side = Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);
        if (side > 0f) rt.sizeDelta = new Vector2(side, side);
        foreach (Transform c in b.transform)
        {
            if (c.GetComponent<TMP_Text>() != null || c.GetComponent<Image>() != null) c.gameObject.SetActive(false);
        }
    }
}

// علامة إن النافذة اتعدلت (ما نعدلها مرتين)
public class OldUiSkinDone : MonoBehaviour { }
