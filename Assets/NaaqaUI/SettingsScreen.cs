using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== الإعدادات =====
// صوت الصحراء، وصوت الأزرار، وحسابي.
public class SettingsScreen : MonoBehaviour
{
    static SettingsScreen instance;
    NaaqaWindow win;
    Slider ambient, sfx;

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("SettingsScreen").AddComponent<SettingsScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Load();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Settings", "الإعدادات", "settings", new Vector2(900, 860));
        win.transform.SetParent(transform, false);
        win.NoSubtitle();
        win.onClose = () => PlayerPrefs.Save();

        RectTransform list = NaaqaUI.Rect("List", win.body);
        NaaqaUI.Stretch(list, 10, 10, 10, 0);
        VerticalLayoutGroup v = list.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 18;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        ambient = SliderRow(list, "صوت الصحراء", "sound_desert", v2 => { if (SoundManager.Instance != null) SoundManager.Instance.SetAmbientVolume(v2); });
        sfx = SliderRow(list, "صوت الأزرار", "sound_sfx", v2 => { if (SoundManager.Instance != null) SoundManager.Instance.SetSfxVolume(v2); });

        RTLTextMeshPro bt;
        Button prof = NaaqaUI.MakeButton(list, "Profile", "حسابي", NaaqaUI.ButtonStyle.Secondary, 28, out bt);
        NaaqaUI.Height(prof, 84);
        Image pi = NaaqaUI.IconImg(prof.transform, "profile", 60);
        NaaqaUI.Place(pi.rectTransform, new Vector2(1, 0.5f), new Vector2(60, 60), new Vector2(-14, 0));
        prof.onClick.AddListener(() => { win.Close(); ProfileScreen.Show(); });

        // حذف الحساب (أحمر، ويطلب رمز تأكيد)
        RTLTextMeshPro dt;
        Button del = NaaqaUI.MakeButton(list, "Delete", "حذف الحساب", NaaqaUI.ButtonStyle.Danger, 28, out dt);
        NaaqaUI.Height(del, 84);
        Image dbg = del.GetComponent<Image>();
        if (dbg != null) dbg.color = new Color(0.75f, 0.16f, 0.12f, 0.9f);
        dt.color = Color.white;
        Image di = NaaqaUI.IconImg(del.transform, "delete", 60);
        NaaqaUI.Place(di.rectTransform, new Vector2(1, 0.5f), new Vector2(60, 60), new Vector2(-14, 0));
        del.onClick.AddListener(() => { win.Close(); DeleteAccountScreen.Show(); });

        RTLTextMeshPro note = NaaqaUI.Text(list, "Note", "تقدمك ينحفظ لحاله على حسابك", 22, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(note, 40);
    }

    Slider SliderRow(Transform parent, string label, string icon, UnityEngine.Events.UnityAction<float> onChange)
    {
        Image row = NaaqaUI.Tile(parent, "Row_" + label);
        NaaqaUI.Height(row, 120);
        SaduFit.Apply(row, "pill");   // نفس شكل عدادات القروش والحليب

        RTLTextMeshPro t = NaaqaUI.Text(row.transform, "Label", label, 28, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(t.rectTransform, new Vector2(1, 0.5f), new Vector2(200, 60), new Vector2(-104, 4));
        Image ic = NaaqaUI.IconImg(row.transform, icon, 76);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(76, 76), new Vector2(-18, 4));

        RectTransform sr = NaaqaUI.Rect("Slider", row.transform);
        sr.anchorMin = new Vector2(0, 0.5f); sr.anchorMax = new Vector2(1, 0.5f);
        sr.offsetMin = new Vector2(40, -16); sr.offsetMax = new Vector2(-320, 24);

        // المسار: غامق بإطار ذهبي، والمعبّى أحمر سدو، والمقبض دائرة سدو
        Image track = NaaqaUI.Img(sr, "Track", NaaqaUI.Rounded(), new Color(0.05f, 0.03f, 0.02f, 0.9f), 14f);
        track.rectTransform.anchorMin = new Vector2(0, 0.5f); track.rectTransform.anchorMax = new Vector2(1, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(0, 30);
        Image tl = NaaqaUI.Img(track.transform, "Line", NaaqaUI.RoundedOutline(), new Color(0.84f, 0.66f, 0.36f, 0.9f), 14f);
        NaaqaUI.Stretch(tl.rectTransform);

        RectTransform fillArea = NaaqaUI.Rect("FillArea", sr);
        fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
        fillArea.sizeDelta = new Vector2(0, 30);
        Image fill = NaaqaUI.Img(fillArea, "Fill", NaaqaUI.RoundedGradient(NaaqaUI.OrangeLight, NaaqaUI.OrangeDark, "btnPrimary"), Color.white, 14f);
        SaduFit.Apply(fill, "primary");
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
        fill.rectTransform.sizeDelta = Vector2.zero;

        RectTransform handleArea = NaaqaUI.Rect("HandleArea", sr);
        NaaqaUI.Stretch(handleArea, 26, 26, 0, 0);
        Sprite knob = NaaqaUI.Load("sadu_frame");
        Image handle = NaaqaUI.Img(handleArea, "Handle", knob != null ? knob : NaaqaUI.Circle(), knob != null ? Color.white : NaaqaUI.Cream);
        handle.preserveAspect = true;
        handle.rectTransform.sizeDelta = new Vector2(60, 60);
        handle.raycastTarget = true;

        Slider s = sr.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.onValueChanged.AddListener(onChange);

        // نخلي كل الصف يمسك السحب
        Image hit = sr.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f);
        return s;
    }

    void Load()
    {
        SoundManager sm = SoundManager.Instance;
        if (sm == null) return;
        ambient.SetValueWithoutNotify(sm.ambientVolume);
        sfx.SetValueWithoutNotify(sm.sfxVolume);
    }
}
