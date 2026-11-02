using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== معلومات الناقة =====
// صورتها، واسمها (تقدر تغيّره)، وسلالتها، وجنسها، ووش هي شاطرة فيه.
public class CamelInfoScreen : MonoBehaviour
{
    static CamelInfoScreen instance;
    NaaqaWindow win;
    RawImage pic;
    TMP_InputField nameField;
    RTLTextMeshPro breedText, rarityText, genderText, bestText, statsText, stateText;
    Camel camel;

    public static void Show(Camel c)
    {
        if (c == null) return;
        if (instance == null)
        {
            instance = new GameObject("CamelInfoScreen").AddComponent<CamelInfoScreen>();
            instance.Build();
        }
        instance.camel = c;
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("CamelInfo", "معلومات الناقة", "camel_market", new Vector2(1200, 780));
        win.transform.SetParent(transform, false);
        win.NoSubtitle();

        // الصورة (يسار)
        Image frame = NaaqaUI.Tile(win.body, "PicFrame", 0.06f);
        NaaqaUI.Place(frame.rectTransform, new Vector2(0, 1), new Vector2(460, 460), new Vector2(0, -10));
        RectTransform pa = NaaqaUI.Rect("Pic", frame.transform);
        NaaqaUI.Stretch(pa, 14, 14, 14, 14);
        pic = pa.gameObject.AddComponent<RawImage>();
        pic.uvRect = new Rect(0f, 0.03f, 1f, 0.87f);
        pic.raycastTarget = false;

        // يمين: الاسم والتفاصيل
        RectTransform info = NaaqaUI.Rect("Info", win.body);
        info.anchorMin = new Vector2(0, 0); info.anchorMax = new Vector2(1, 1);
        info.offsetMin = new Vector2(490, 0); info.offsetMax = Vector2.zero;

        RTLTextMeshPro nl = NaaqaUI.Text(info, "NameLabel", "اسمها", 22, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        NaaqaUI.Place(nl.rectTransform, new Vector2(1, 1), new Vector2(600, 34), new Vector2(0, -10));
        nameField = NaaqaUI.Field(info, "Name", "اكتب اسمها", true, TMP_InputField.ContentType.Standard, 74f);
        NaaqaUI.Place((RectTransform)nameField.transform, new Vector2(1, 1), new Vector2(430, 74), new Vector2(0, -48));
        nameField.characterLimit = 16;
        RTLTextMeshPro st;
        Button save = NaaqaUI.MakeButton(info, "Save", "احفظ الاسم", NaaqaUI.ButtonStyle.Primary, 24, out st);
        NaaqaUI.Place((RectTransform)save.transform, new Vector2(1, 1), new Vector2(210, 74), new Vector2(-446, -48));
        save.onClick.AddListener(SaveName);

        float y = -150;
        breedText = Row(info, ref y, 34, true);
        rarityText = Row(info, ref y, 26, true);
        genderText = Row(info, ref y, 26, false);
        bestText = Row(info, ref y, 26, false);
        statsText = Row(info, ref y, 24, false);
        stateText = Row(info, ref y, 24, false);
        stateText.color = NaaqaUI.Muted;
    }

    RTLTextMeshPro Row(Transform parent, ref float y, float size, bool bold)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Row", "", size, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, bold);
        t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        t.rectTransform.sizeDelta = new Vector2(0, size + 26);
        t.rectTransform.anchoredPosition = new Vector2(0, y);
        y -= size + 30;
        return t;
    }

    void Refresh()
    {
        if (camel == null) { win.Close(); return; }
        string id = CamelBreeds.BreedOf(camel);
        CamelBreeds.Breed b = CamelBreeds.Get(id);
        CamelProfiles.Entry e = CamelProfiles.Get(camel);

        pic.texture = Resources.Load<Texture2D>("NaaqaCamels/camel_" + id);
        nameField.SetTextWithoutNotify(e.name);
        win.title.text = e.name;

        if (b == null) return;
        breedText.text = "السلالة: " + b.name;
        rarityText.text = "الندرة: " + b.RarityName;
        rarityText.color = b.RarityColor;
        // (نظام ذكر وأنثى انشال: نوريه وقت الحليب بداله)
        genderText.text = "وقت الحليب: " + NaaqaUI.Ar(Mathf.RoundToInt(b.MilkProductionTime)) + " ثانية لكل حليبة";
        bestText.text = "شاطرة في: " + b.Best + "  •  " + b.use;
        statsText.text = "الحليب " + NaaqaUI.Ar(b.MilkStars) + "/٣"
                       + "  •  " + (b.RacingEligible ? "السباق " + NaaqaUI.Ar(b.RaceStars) + "/٣" : "غير مؤهلة للسباق")
                       + "  •  المزاين " + NaaqaUI.Ar(b.beauty) + "/٣";
        stateText.text = camel.IsMilkReady ? "حليبها جاهز! اضغط عليها وتجمعه"
                       : camel.IsProducing ? "تنتج حليب الحين"
                       : "جوعانة، أطعمها عشان تنتج حليب";
    }

    void SaveName()
    {
        if (camel == null) return;
        CamelProfiles.Rename(camel, nameField.text);
        win.SetStatus("انحفظ اسمها: " + CamelProfiles.Get(camel).name, true);
        Refresh();
    }
}
