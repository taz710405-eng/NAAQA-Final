using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ===== الواجهات تناسب كل الشاشات =====
// كل واجهة مصممة على ١٩٢٠×١٠٨٠. قبل كانت تتقص على الآيباد (أضيق) وعلى الجوالات العريضة (أطول).
// هنا نخلي كل CanvasScaler بوضع Expand: التصميم كله يدخل في الشاشة، والزيادة تكون مساحة فاضية على الأطراف،
// والأشياء المثبتة على الزوايا (العدادات والأزرار) تروح لزوايا الشاشة.
// يشتغل لحاله على واجهات المشهد (ما نعدّل main.unity) وعلى أي واجهة تنبني بعدين.
// ولما يتغير مقاس الشاشة (تدوير الجوال، تكبير المتصفح) نعيد رسم كل النصوص، عشان حدّة الخط تنحسب
// على المقاس الجديد (بدونها ممكن يطلع الخط باهت أو مربعات شفافة).
public class ScreenFit : MonoBehaviour
{
    float timer;
    int lastW, lastH;
    float refreshIn = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        GameObject g = new GameObject("ScreenFit");
        DontDestroyOnLoad(g);
        g.AddComponent<ScreenFit>();
        Fix();
        FontUpgrade.SwapAll();
        SceneManager.sceneLoaded += (s, m) => { Fix(); FontUpgrade.SwapAll(); };
    }

    // كل ثانية نشيك على الواجهات الجديدة (النوافذ تنبني وقت ما تنفتح)
    void Update()
    {
        // تغيّر مقاس الشاشة: نعيد رسم النصوص بعد ما يستقر المقاس
        if (Screen.width != lastW || Screen.height != lastH)
        {
            bool first = lastW == 0;
            lastW = Screen.width; lastH = Screen.height;
            if (!first) refreshIn = 0.25f;
        }
        if (refreshIn > 0f)
        {
            refreshIn -= Time.unscaledDeltaTime;
            if (refreshIn <= 0f) RefreshTexts();
        }

        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 1f;
        FontUpgrade.SwapAll();   // النصوص الجديدة تاخذ الخط الواضح
        if (Fix()) refreshIn = 0.1f;
    }

    // يرجع true لو غيّر شي
    static bool Fix()
    {
        bool changed = false;
        foreach (CanvasScaler sc in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
        {
            if (sc.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            if (sc.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
            {
                sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                changed = true;
            }
        }
        return changed;
    }

    static void RefreshTexts()
    {
        Canvas.ForceUpdateCanvases();
        foreach (TMPro.TMP_Text t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude))
            t.SetAllDirty();
    }
}
