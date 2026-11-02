using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== تطوير الأحواش =====
// كل أحواشك في نافذة وحدة: الحوش الرئيسي أول، وبعده الأحواش الإضافية.
// كل حوش من مستوى ١ لين ٥، وكل مستوى يزيد ناقة.
// أول تطوير للحوش الرئيسي يصير في الشرح، والباقي من هنا.
public class PenScreen : MonoBehaviour
{
    static PenScreen instance;
    NaaqaWindow win;
    RectTransform list;
    RTLTextMeshPro summary;
    ExtraPen focus;     // الحوش اللي ضغط عليه (يتلوّن)
    readonly List<GameObject> rows = new List<GameObject>();
    static readonly Dictionary<string, Sprite> pics = new Dictionary<string, Sprite>();

    public static void Show() { Open(null); }
    public static void ShowExtra(ExtraPen pen) { Open(pen); }

    static void Open(ExtraPen pen)
    {
        if (instance == null)
        {
            instance = new GameObject("PenScreen").AddComponent<PenScreen>();
            instance.Build();
        }
        instance.focus = pen;
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Pen", "تطوير الأحواش", "upgrade", new Vector2(1240, 800));
        win.transform.SetParent(transform, false);
        win.subtitle.text = "كل ما طوّرت حوش يشيل ناقة زيادة";

        summary = NaaqaUI.Text(win.body, "Summary", "", 28, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(summary.rectTransform, new Vector2(0.5f, 1), new Vector2(1100, 46), new Vector2(0, 0));

        RectTransform area = NaaqaUI.Rect("Area", win.body);
        NaaqaUI.Stretch(area, 0, 0, 56, 60);
        list = win.List(area, 12);

        RTLTextMeshPro hint = NaaqaUI.Text(win.body, "Hint", "تبي تغيّر مكان حوش؟ اضغط «تعديل» وحرّكه", 22, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(1100, 44), new Vector2(0, 6));
    }

    static string Camels(int n) { return NaaqaUI.Ar(n) + (n == 1 ? " ناقة" : " إبل"); }

    static string Dots(int lvl, int max)
    {
        return "(" + NaaqaUI.Ar(lvl) + " من " + NaaqaUI.Ar(max) + ")";   // الدوائر ما هي في الخط العربي
    }

    static Sprite Pic(string path)
    {
        Sprite s;
        if (pics.TryGetValue(path, out s) && s != null) return s;
        Texture2D t = Resources.Load<Texture2D>(path);
        s = t != null ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f) : null;
        pics[path] = s;
        return s;
    }

    void Refresh()
    {
        foreach (GameObject g in rows) if (g != null) Destroy(g);
        rows.Clear();

        summary.text = "كل إبلك " + NaaqaUI.Ar(CamelBreeds.CamelCount()) + " من " + NaaqaUI.Ar(CamelBreeds.Capacity) + " مكان";

        // الحوش الرئيسي
        FarmStore st = FarmStore.Instance;
        int main = st != null ? st.PenLevel : 1;
        AddRow("الحوش الرئيسي", main, GameRules.PenMaxLevel, GameRules.CamelsForPen(main), main < GameRules.PenMaxLevel ? GameRules.CamelsForPen(main + 1) : 0,
               main < GameRules.PenMaxLevel ? GameRules.PenUpgradeCost(main + 1) : 0,
               Pic(PenVisual.Folder + "/pen_" + main), focus == null, () => UpgradeMain());

        // الأحواش الإضافية
        List<ExtraPen> extras = ExtraPen.All();
        for (int i = 0; i < extras.Count; i++)
        {
            ExtraPen p = extras[i];
            int l = p.Level;
            Sprite pic = l >= 2 ? Pic(PenVisual.Folder + "/pen_" + l) : Pic(ShopExtraItems.Folder + "/" + ExtraPen.Id);
            bool top = l >= GameRules.ExtraPenMaxLevel;
            AddRow("حوش إضافي " + NaaqaUI.Ar(i + 1), l, GameRules.ExtraPenMaxLevel, ExtraPen.Capacity(l), top ? 0 : ExtraPen.Capacity(l + 1),
                   top ? 0 : GameRules.ExtraPenUpgradeCost(l + 1), pic, p == focus, () => UpgradeExtra(p));
        }

        if (extras.Count == 0)
        {
            int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
            RTLTextMeshPro t = NaaqaWindow.Header(list, lv >= GameRules.ExtraPen1Unlock
                ? "تبي إبل أكثر؟ اشترِ حوش إضافي من متجر السوق الشعبي"
                : "الحوش الإضافي يفتح في المستوى " + NaaqaUI.Ar(GameRules.ExtraPen1Unlock));
            t.alignment = TextAlignmentOptions.Center;
            t.color = NaaqaUI.Muted;
            t.fontSize = 22;
            rows.Add(t.gameObject);
        }
    }

    void AddRow(string name, int lvl, int maxLvl, int cap, int nextCap, int cost, Sprite pic, bool highlight, System.Action onUp)
    {
        bool max = lvl >= maxLvl;
        RTLTextMeshPro main, sub, bt;
        Button b;
        Image row = NaaqaWindow.Row(list, "upgrade", pic, 150, out main, out sub, out b, out bt,
                                    max ? "أعلى مستوى" : "طوّر • " + NaaqaUI.Ar(cost) + " قرش", 250);
        if (highlight) row.color = new Color(1f, 0.62f, 0.2f, 0.18f);
        main.text = name + "  •  مستوى " + NaaqaUI.Ar(lvl) + "  " + Dots(lvl, maxLvl);
        sub.text = "يشيل " + Camels(cap) + (max ? "  •  وصل أعلى مستوى، ما شاء الله" : "  •  المستوى الجاي يشيل " + Camels(nextCap));
        if (max)
        {
            b.interactable = false;
            if (b.targetGraphic != null) b.targetGraphic.color = new Color(1, 1, 1, 0.35f);
        }
        else b.onClick.AddListener(() => onUp());
        rows.Add(row.gameObject);
    }

    bool Pay(int cost)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        if (!gm.SpendMoney(cost))
        {
            win.SetStatus("القروش ما تكفي! التطوير يبي " + NaaqaUI.Ar(cost) + " قرش", false);
            return false;
        }
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        return true;
    }

    void UpgradeMain()
    {
        FarmStore st = FarmStore.Instance;
        if (st == null) return;
        int lvl = st.PenLevel;
        if (lvl >= GameRules.PenMaxLevel || !Pay(GameRules.PenUpgradeCost(lvl + 1))) return;
        st.SetPenLevel(lvl + 1);
        Done("الحوش الرئيسي", lvl + 1, GameRules.CamelsForPen(lvl + 1));
    }

    void UpgradeExtra(ExtraPen p)
    {
        if (p == null) return;
        int lvl = p.Level;
        if (lvl >= GameRules.ExtraPenMaxLevel || !Pay(GameRules.ExtraPenUpgradeCost(lvl + 1))) return;
        p.SetLevel(lvl + 1);
        Done("الحوش الإضافي", lvl + 1, ExtraPen.Capacity(lvl + 1));
    }

    void Done(string name, int lvl, int cap)
    {
        LevelManager.AddXp(LevelManager.XpPerPurchase);
        win.SetStatus("تم تطوير " + name + " بنجاح! صار يشيل " + Camels(cap), true);
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(name + " صار مستوى " + NaaqaUI.Ar(lvl) + "!");
        Refresh();
    }
}
