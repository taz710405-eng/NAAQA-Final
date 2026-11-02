using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RTLTMPro;
using TMPro;

// ===== كشك البيع: بيع مباشر =====
// اضغط على الكشك وبع حليبك وتمرك على طول بدون ما تنتظر زبون.
// الحليب بـ٦ قروش، والتمرة بـ٤ (GameRules). كل بيعة تنخصم مرة وحدة من المخزون.
public class KioskScreen : MonoBehaviour
{
    static KioskScreen instance;
    NaaqaWindow win;
    RTLTextMeshPro milkHave, datesHave;
    Button milkOne, milkAll, dateOne, dateAll;
    RTLTextMeshPro milkAllText, dateAllText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PlacementManager.OnItemPlaced -= OnPlaced;
        PlacementManager.OnItemPlaced += OnPlaced;
    }

    // الكشك المتركب (جديد أو من الحفظ) ينضغط
    static void OnPlaced(string id, GameObject go)
    {
        if (id != StallCustomers.StallId || go == null) return;
        BoxCollider box = go.GetComponentInChildren<BoxCollider>(true);
        GameObject target = box != null ? box.gameObject : go;
        if (target.GetComponent<KioskTap>() == null) target.AddComponent<KioskTap>();
    }

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("KioskScreen").AddComponent<KioskScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Kiosk", "كشك البيع", "milk", new Vector2(1100, 600));
        win.transform.SetParent(transform, false);
        win.subtitle.text = "بع حليبك وتمرك على طول، أو انتظر الزباين يجونك";
        win.onTick = Refresh;

        Row(-20f, "milk", "الحليب • " + NaaqaUI.Ar(GameRules.MilkPrice) + " قروش للحبة", out milkHave, out milkOne, out milkAll, out milkAllText,
            () => Sell(false, 1), () => Sell(false, -1));
        Row(-230f, "dates_farm", "التمر • " + NaaqaUI.Ar(GameRules.DatePrice) + " قروش للحبة", out datesHave, out dateOne, out dateAll, out dateAllText,
            () => Sell(true, 1), () => Sell(true, -1));
    }

    void Row(float y, string icon, string title, out RTLTextMeshPro have, out Button one, out Button all, out RTLTextMeshPro allText,
             UnityEngine.Events.UnityAction onOne, UnityEngine.Events.UnityAction onAll)
    {
        Image tile = NaaqaUI.Tile(win.body, "Row_" + icon);
        RectTransform r = tile.rectTransform;
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 1f);
        r.offsetMin = new Vector2(30, y - 190); r.offsetMax = new Vector2(-30, y);

        Image ic = NaaqaUI.IconImg(tile.transform, icon, 110);
        NaaqaUI.Place(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(110, 110), new Vector2(-24, 0));
        RTLTextMeshPro t = NaaqaUI.Text(tile.transform, "Title", title, 30, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(t.rectTransform, new Vector2(1, 1), new Vector2(560, 50), new Vector2(-150, -30));
        have = NaaqaUI.Text(tile.transform, "Have", "", 26, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        NaaqaUI.Place(have.rectTransform, new Vector2(1, 0), new Vector2(560, 46), new Vector2(-150, 40));

        RTLTextMeshPro ot;
        one = NaaqaUI.MakeButton(tile.transform, "One", "بِع ١", NaaqaUI.ButtonStyle.Secondary, 26, out ot);
        NaaqaUI.Place((RectTransform)one.transform, new Vector2(0, 0.5f), new Vector2(150, 76), new Vector2(250, 0));
        one.onClick.AddListener(onOne);
        all = NaaqaUI.MakeButton(tile.transform, "All", "", NaaqaUI.ButtonStyle.Primary, 24, out allText);
        NaaqaUI.Place((RectTransform)all.transform, new Vector2(0, 0.5f), new Vector2(210, 76), new Vector2(24, 0));
        all.onClick.AddListener(onAll);
    }

    static int MilkNow { get { return GameManager.Instance != null ? GameManager.Instance.milkCount : 0; } }
    static int DatesNow { get { return FarmStore.Instance != null ? FarmStore.Instance.Dates : 0; } }

    void Refresh()
    {
        int m = MilkNow, d = DatesNow;
        milkHave.text = "عندك " + NaaqaUI.Ar(m) + " حليب";
        datesHave.text = "عندك " + NaaqaUI.Ar(d) + " تمر";
        milkOne.interactable = m > 0; milkAll.interactable = m > 0;
        dateOne.interactable = d > 0; dateAll.interactable = d > 0;
        milkAllText.text = m > 0 ? "بِع الكل • " + NaaqaUI.Ar(m * GameRules.MilkPrice) : "ما عندك حليب";
        dateAllText.text = d > 0 ? "بِع الكل • " + NaaqaUI.Ar(d * GameRules.DatePrice) : "ما عندك تمر";
    }

    // n = -1 يعني الكل. يتحقق من المخزون قبل البيع، ويخصم مرة وحدة
    void Sell(bool dates, int n)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        int have = dates ? DatesNow : MilkNow;
        int count = n < 0 ? have : n;
        if (count <= 0 || have < count) { win.SetStatus(dates ? "ما عندك تمر كافي" : "ما عندك حليب كافي", false); return; }

        if (dates) FarmStore.Instance.AddDates(-count);
        else
        {
            gm.milkCount -= count;
            if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
            Quests.Add("sell", count);
        }
        int coins = count * (dates ? GameRules.DatePrice : GameRules.MilkPrice);
        gm.AddMoney(coins);
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        CamelSounds.Play("pour");
        win.SetStatus((dates ? "تم بيع التمر" : "تم بيع الحليب") + " +" + NaaqaUI.Ar(coins) + " قرش", true);
        Refresh();
    }
}

// الضغط على الكشك (برا التركيب والتعديل)
public class KioskTap : MonoBehaviour
{
    void OnMouseUpAsButton()
    {
        if (PlacementManager.IsPlacing || EditMode.Active) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TutorialEnd.Active) return;   // وقت الشرح يبيع للزبون الأول
        KioskScreen.Show();
    }
}
