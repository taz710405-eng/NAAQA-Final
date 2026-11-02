using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== نافذة المزرعة =====
// ازرع (تشرب مويه)، احصد، وطوّر (٣ مستويات: ٥ ثم ٧ ثم ١٠).
public class FarmScreen : MonoBehaviour
{
    static FarmScreen instance;
    NaaqaWindow win;
    Image pic;
    RTLTextMeshPro levelText, stateText, infoText, actText, upText;
    Button act, up;
    FarmPlot farm;

    public static void Show(FarmPlot f)
    {
        if (f == null) return;
        if (instance == null)
        {
            instance = new GameObject("FarmScreen").AddComponent<FarmScreen>();
            instance.Build();
        }
        instance.farm = f;
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Farm", "المزرعة", "feed_farm", new Vector2(1100, 700));
        win.transform.SetParent(transform, false);
        win.onTick = Refresh;

        Image frame = NaaqaUI.Tile(win.body, "PicFrame", 0.06f);
        NaaqaUI.Place(frame.rectTransform, new Vector2(1, 1), new Vector2(400, 330), new Vector2(0, -10));
        pic = NaaqaUI.Img(frame.transform, "Pic", null, Color.white);
        pic.preserveAspect = true;
        NaaqaUI.Stretch(pic.rectTransform, 14, 14, 14, 14);

        RectTransform info = NaaqaUI.Rect("Info", win.body);
        info.anchorMin = new Vector2(0, 0); info.anchorMax = new Vector2(1, 1);
        info.offsetMin = new Vector2(0, 120); info.offsetMax = new Vector2(-430, -10);
        float y = 0;
        levelText = Line(info, ref y, 32, true);
        // سطرين: المستوى، وتحته كم تطلع وكل كم (كان سطر واحد طويل يطلع برا ويركب على اللي تحته)
        levelText.rectTransform.sizeDelta = new Vector2(0, 32 * 2 + 30);
        y -= 40;
        stateText = Line(info, ref y, 28, true);
        infoText = Line(info, ref y, 22, false);
        infoText.color = NaaqaUI.Muted;
        infoText.rectTransform.sizeDelta = new Vector2(0, 120);

        act = NaaqaUI.MakeButton(win.body, "Act", "", NaaqaUI.ButtonStyle.Primary, 28, out actText);
        NaaqaUI.Place((RectTransform)act.transform, new Vector2(1, 0), new Vector2(420, 96), new Vector2(0, 10));
        act.onClick.AddListener(Act);
        up = NaaqaUI.MakeButton(win.body, "Up", "", NaaqaUI.ButtonStyle.Secondary, 26, out upText);
        NaaqaUI.Place((RectTransform)up.transform, new Vector2(0, 0), new Vector2(420, 96), new Vector2(0, 10));
        up.onClick.AddListener(Upgrade);
    }

    RTLTextMeshPro Line(Transform p, ref float y, float size, bool bold)
    {
        RTLTextMeshPro t = NaaqaUI.Text(p, "Line", "", size, NaaqaUI.Cream, TextAlignmentOptions.TopRight, bold);
        t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        t.rectTransform.sizeDelta = new Vector2(0, size + 24);
        t.rectTransform.anchoredPosition = new Vector2(0, y);
        y -= size + 30;
        return t;
    }

    void Refresh()
    {
        if (farm == null) { win.Close(); return; }
        win.title.text = farm.Title;
        win.subtitle.text = "ازرع، استنَّ لين تجهز، واحصد";
        pic.sprite = NaaqaUI.LoadItem(farm.id + (farm.Level >= 2 ? "_2" : ""));
        levelText.text = "المستوى " + NaaqaUI.Ar(farm.Level) + " من " + NaaqaUI.Ar(FarmPlot.MaxLevel)
                       + "\nتطلع " + NaaqaUI.Ar(farm.Yield) + " " + farm.ProductName + " كل " + Dur(farm.GrowTime);

        FarmStore st = FarmStore.Instance;
        int water = st != null ? st.Water : 0;
        switch (farm.state)
        {
            case FarmPlot.State.Empty:
                stateText.text = "فاضية";
                stateText.color = NaaqaUI.Cream;
                actText.text = "ازرع • " + NaaqaUI.Ar(GameRules.FarmWaterPerCycle) + " مويه";
                act.interactable = true;
                break;
            case FarmPlot.State.Growing:
                int s = Mathf.CeilToInt(farm.left);
                stateText.text = "تنمو... باقي " + NaaqaUI.Ar(s / 60) + ":" + NaaqaUI.Ar(s % 60 / 10) + NaaqaUI.Ar(s % 10);
                stateText.color = NaaqaUI.OrangeLight;
                actText.text = "تنمو";
                act.interactable = false;
                break;
            default:
                stateText.text = "جاهزة للحصاد!";
                stateText.color = NaaqaUI.OkColor;
                actText.text = "احصد " + NaaqaUI.Ar(farm.Yield) + " " + farm.ProductName;
                act.interactable = true;
                break;
        }
        infoText.text = "كل حبة " + farm.ProductName + " تشرب حبة مويه  •  عندك " + NaaqaUI.Ar(water) + " مويه"
            + (farm.IsFeed ? "\nالعلف تاكله إبلك" : "\nالتمر تعطيه إبلك (تنتج أسرع) وتبيعه للزباين");

        if (farm.Level >= FarmPlot.MaxLevel) { upText.text = "أعلى مستوى"; up.interactable = false; }
        else
        {
            upText.text = "طوّر • " + NaaqaUI.Ar(FarmPlot.UpgradeCost(farm.Level + 1)) + " قرش • " + NaaqaUI.Ar(FarmPlot.YieldAt(farm.Level + 1)) + " " + farm.ProductName;
            up.interactable = true;
        }
    }

    void Act()
    {
        if (farm == null) return;
        string err = farm.state == FarmPlot.State.Ready ? farm.Harvest() : farm.Plant();
        if (err != null) win.SetStatus(err, false);
        else win.SetStatus(farm.state == FarmPlot.State.Growing ? "انزرعت!" : "حصدت!", true);
        Refresh();
    }

    // ١٨٠ ث = «٣ دقايق»، ١٥٠ ث = «٢:٣٠ دقيقة»
    static string Dur(float sec)
    {
        int t = Mathf.RoundToInt(sec), m = t / 60, r = t % 60;
        return r == 0 ? NaaqaUI.Ar(m) + " دقايق" : NaaqaUI.Ar(m) + ":" + NaaqaUI.Ar(r / 10) + NaaqaUI.Ar(r % 10) + " دقيقة";
    }

    void Upgrade()
    {
        if (farm == null) return;
        string err = farm.Upgrade();
        if (err != null) win.SetStatus(err, false);
        else win.SetStatus("تطورت المزرعة! صارت تطلع " + NaaqaUI.Ar(farm.Yield), true);
        Refresh();
    }
}
