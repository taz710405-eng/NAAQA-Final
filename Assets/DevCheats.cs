#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

// ===== أزرار التجربة (داخل Unity بس، ما تطلع في اللعبة المنشورة على المتصفح) =====
// وقت Play، اضغط Shift (أو Ctrl) مع الحرف:
//   L = ترفع مستواك ١   (Ctrl+Shift+L = ترفع ٥)
//   M = +١٠٠٠ قرش
//   N = +١٠٠٠ حليب
//   F = +١٠٠٠ علف
//   D = +١٠٠٠ تمر
//   K = +٢٠ حليب وتمر وترنج (القديم)
// التقدم ينحفظ على الحساب اللي داخل فيه، فجرّبها على حساب تجربة.
public class DevCheats : MonoBehaviour
{
    const int Amount = 1000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        GameObject g = new GameObject("DevCheats");
        DontDestroyOnLoad(g);
        g.AddComponent<DevCheats>();
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        // وقت الكتابة في خانة (مثل الإيميل) ما تشتغل الاختصارات
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        GameObject sel = es != null ? es.currentSelectedGameObject : null;
        if (sel != null && (sel.GetComponent<TMPro.TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null)) return;
        bool shift = k.shiftKey.isPressed, ctrl = k.ctrlKey.isPressed;
        if (!shift && !ctrl) return;
        GameManager gm = GameManager.Instance;

        if (k.lKey.wasPressedThisFrame && LevelManager.Instance != null)
        {
            int add = shift && ctrl ? 5 : 1;
            LevelManager.Instance.Restore(LevelManager.Instance.Level + add, 0);
            Say("تجربة: صرت المستوى " + NaaqaUI.Ar(LevelManager.Instance.Level));
        }

        if (k.mKey.wasPressedThisFrame && gm != null)
        {
            gm.AddMoney(Amount);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
            Say("تجربة: +" + NaaqaUI.Ar(Amount) + " قرش");
        }

        if (k.nKey.wasPressedThisFrame && gm != null)
        {
            gm.AddMilk(Amount);
            if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
            Say("تجربة: +" + NaaqaUI.Ar(Amount) + " حليب");
        }

        if (k.fKey.wasPressedThisFrame && gm != null)
        {
            gm.AddFeed(Amount);
            if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
            Say("تجربة: +" + NaaqaUI.Ar(Amount) + " علف");
        }

        if (k.dKey.wasPressedThisFrame && FarmStore.Instance != null)
        {
            FarmStore.Instance.AddDates(Amount);
            Say("تجربة: +" + NaaqaUI.Ar(Amount) + " تمر");
        }

        if (k.kKey.wasPressedThisFrame && gm != null)
        {
            gm.AddMilk(20);
            if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
            if (FarmStore.Instance != null) { FarmStore.Instance.AddDates(20); FarmStore.Instance.AddCitron(20); }
            Say("تجربة: +٢٠ حليب وتمر وترنج");
        }
    }

    static void Say(string msg)
    {
        Debug.Log(msg);
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(msg);
    }
}
#endif
