using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== نافذة تعارض الحفظ =====
// تطلع بس لو فيه تعارض حقيقي: تقدم على هالجهاز ما ارتفع، والسحابة تقدمت من جهاز ثاني، والمحتوى مختلف.
// اللاعب يختار وش يبقى، والثاني ينحفظ احتياط على الجهاز (SaveManager يسوي هالشي).
public class SaveConflictScreen : MonoBehaviour
{
    public static void Show(SaveManager.SaveData local, SaveManager.SaveData cloud, System.Action<bool> choose, System.Action later)
    {
        SaveConflictScreen s = new GameObject("SaveConflictScreen").AddComponent<SaveConflictScreen>();
        s.Build(local, cloud, choose, later);
    }

    NaaqaWindow win;
    bool done;

    void Build(SaveManager.SaveData local, SaveManager.SaveData cloud, System.Action<bool> choose, System.Action later)
    {
        win = NaaqaWindow.Create("SaveConflict", "وش التقدم اللي تبيه؟", "mail", new Vector2(1300, 700), 900);
        win.transform.SetParent(transform, false);
        win.subtitle.text = "لعبت على جهاز ثاني وعلى هالجهاز. اختار واحد، والثاني ينحفظ احتياط";

        Card(local, "تقدم هالجهاز", 0.75f, () => Pick(true, choose));
        Card(cloud, "التقدم في السحابة", 0.25f, () => Pick(false, choose));

        win.onClose = () =>
        {
            if (!done && later != null) later();
            Destroy(gameObject, 0.1f);
        };
        win.Open();
    }

    void Pick(bool chooseLocal, System.Action<bool> choose)
    {
        if (done) return;
        done = true;
        if (choose != null) choose(chooseLocal);
        win.Close();
    }

    void Card(SaveManager.SaveData d, string title, float x, UnityEngine.Events.UnityAction onPick)
    {
        Image tile = NaaqaUI.Tile(win.body, "Card");
        RectTransform r = tile.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(x, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(580, 470);
        r.anchoredPosition = Vector2.zero;

        RTLTextMeshPro t = NaaqaUI.Text(tile.transform, "Title", title, 34, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(540, 56), new Vector2(0f, -40f));

        RTLTextMeshPro info = NaaqaUI.Text(tile.transform, "Info", Summary(d), 28, NaaqaUI.Cream, TextAlignmentOptions.TopRight);
        NaaqaUI.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(500, 260), new Vector2(0f, -210f));

        RTLTextMeshPro bt;
        Button b = NaaqaUI.MakeButton(tile.transform, "Pick", "خذ هذا", NaaqaUI.ButtonStyle.Primary, 30, out bt);
        NaaqaUI.Place((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(300, 84), new Vector2(0f, 60f));
        b.onClick.AddListener(onPick);
    }

    static string Summary(SaveManager.SaveData d)
    {
        if (d == null) return "";
        int camels = d.camels != null ? d.camels.Count : 0;
        string when;
        if (d.savedAt <= 0) when = "الحين";
        else if (!d.timeOk) when = "وقته غير مؤكد";
        else
        {
            long ago = System.Math.Max(0L, ServerTime.UnixLong - d.savedAt);
            when = ago < 3600 ? "قبل " + NaaqaUI.Ar((int)(ago / 60)) + " دقيقة"
                 : ago < 86400 ? "قبل " + NaaqaUI.Ar((int)(ago / 3600)) + " ساعة"
                 : "قبل " + NaaqaUI.Ar((int)(ago / 86400)) + " يوم";
        }
        return "المستوى: " + NaaqaUI.Ar(Mathf.Max(1, d.level))
             + "\nالقروش: " + NaaqaUI.Ar(d.money)
             + "\nالإبل: " + NaaqaUI.Ar(camels)
             + "\nالحليب: " + NaaqaUI.Ar(d.milk) + "  •  العلف: " + NaaqaUI.Ar(d.feed)
             + "\nآخر حفظ: " + when;
    }
}
