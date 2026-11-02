using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== طلب زبون البسطة =====
// لما تضغط على زبون واقف عند بسطة الحليب، تطلع هذي النافذة: شكله، ووش يبي، وكم بيعطيك.
// «سلّم الطلب» تبيع له، و«مو الحين» يمشي.
public class OrdersScreen : MonoBehaviour
{
    static OrdersScreen instance;
    NaaqaWindow win;
    RTLTextMeshPro say, need, have, reward;
    Image npc;
    Button deliver;
    int want, pay, wantDates;
    System.Func<bool> onDeliver;
    System.Action onRefuse, onClosed;
    bool answered;

    public static bool IsOpen { get { return instance != null && instance.win.IsOpen; } }

    // face: صورة الزبون، line: كلامه، want: كم حليب يبي، pay: كم بيعطيك
    public static void ShowCustomer(Sprite face, string line, int want, int pay, System.Func<bool> deliver, System.Action refuse, System.Action closed, int wantDates = 0)
    {
        if (instance == null)
        {
            instance = new GameObject("OrdersScreen").AddComponent<OrdersScreen>();
            instance.Build();
        }
        OrdersScreen s = instance;
        s.want = want; s.pay = pay; s.wantDates = wantDates;
        s.onDeliver = deliver; s.onRefuse = refuse; s.onClosed = closed;
        s.answered = false;
        s.npc.sprite = face;
        s.say.text = line;
        s.need.text = StallCustomers.WantText(want, wantDates);
        s.reward.text = "+" + NaaqaUI.Ar(pay);
        s.win.Open();
        s.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Orders", "طلب زبون", "orders", new Vector2(1100, 700));
        win.transform.SetParent(transform, false);
        win.subtitle.text = "زبون واقف عند كشكك";
        win.onTick = Refresh;
        win.onClose = () => { if (!answered && onClosed != null) onClosed(); };

        RectTransform op = NaaqaUI.Rect("Open", win.body);
        NaaqaUI.Stretch(op);

        npc = NaaqaUI.Img(op, "Npc", null, Color.white);
        npc.preserveAspect = true;
        NaaqaUI.Place(npc.rectTransform, new Vector2(1, 0.5f), new Vector2(230, 440), new Vector2(-10, 10));

        Image bubble = NaaqaUI.Img(op, "Bubble", NaaqaUI.Rounded(), NaaqaUI.BubbleBg, 28f);
        Image bl = NaaqaUI.Img(bubble.transform, "Line", NaaqaUI.RoundedOutline(), NaaqaUI.BubbleLine, 28f);
        NaaqaUI.Stretch(bl.rectTransform);
        bubble.rectTransform.anchorMin = new Vector2(0, 1); bubble.rectTransform.anchorMax = new Vector2(1, 1);
        bubble.rectTransform.pivot = new Vector2(0.5f, 1f);
        bubble.rectTransform.offsetMin = new Vector2(20, -130); bubble.rectTransform.offsetMax = new Vector2(-270, -10);
        say = NaaqaUI.Text(bubble.transform, "Say", "", 30, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        say.enableAutoSizing = true; say.fontSizeMin = 20; say.fontSizeMax = 30;
        NaaqaUI.Stretch(say.rectTransform, 24, 24, 8, 10);

        Image card = NaaqaUI.Tile(op, "Card");
        card.rectTransform.anchorMin = new Vector2(0, 0); card.rectTransform.anchorMax = new Vector2(1, 1);
        card.rectTransform.offsetMin = new Vector2(20, 110); card.rectTransform.offsetMax = new Vector2(-270, -150);

        Image mi = NaaqaUI.IconImg(card.transform, "milk", 120);
        NaaqaUI.Place(mi.rectTransform, new Vector2(1, 0.5f), new Vector2(120, 120), new Vector2(-24, 0));
        need = NaaqaUI.Text(card.transform, "Need", "", 36, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        NaaqaUI.Place(need.rectTransform, new Vector2(1, 0.5f), new Vector2(420, 54), new Vector2(-160, 34));
        have = NaaqaUI.Text(card.transform, "Have", "", 25, NaaqaUI.Muted, TextAlignmentOptions.MidlineRight);
        NaaqaUI.Place(have.rectTransform, new Vector2(1, 0.5f), new Vector2(420, 40), new Vector2(-160, -24));

        Image ci = NaaqaUI.IconImg(card.transform, "coin", 80);
        NaaqaUI.Place(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(80, 80), new Vector2(150, 0));
        reward = NaaqaUI.Text(card.transform, "Reward", "", 36, NaaqaUI.OrangeLight, TextAlignmentOptions.MidlineLeft, true);
        NaaqaUI.Place(reward.rectTransform, new Vector2(0, 0.5f), new Vector2(140, 60), new Vector2(18, 0));

        RTLTextMeshPro dt;
        deliver = NaaqaUI.MakeButton(op, "Deliver", "سلّم الطلب", NaaqaUI.ButtonStyle.Primary, 32, out dt);
        NaaqaUI.Place((RectTransform)deliver.transform, new Vector2(0.5f, 0), new Vector2(380, 86), new Vector2(-30, 10));
        deliver.onClick.AddListener(Deliver);

        RTLTextMeshPro rt;
        Button refuse = NaaqaUI.MakeButton(op, "Refuse", "مو الحين", NaaqaUI.ButtonStyle.Secondary, 28, out rt);
        NaaqaUI.Place((RectTransform)refuse.transform, new Vector2(0, 0), new Vector2(240, 86), new Vector2(20, 10));
        refuse.onClick.AddListener(Refuse);
    }

    void Refresh()
    {
        GameManager gm = GameManager.Instance;
        int h = gm != null ? gm.milkCount : 0;
        int d = FarmStore.Instance != null ? FarmStore.Instance.Dates : 0;
        bool ok = h >= want && d >= wantDates;
        string ht = want > 0 ? "عندك " + NaaqaUI.Ar(h) + " من " + NaaqaUI.Ar(want) + " حليب" : "";
        if (wantDates > 0) ht += (ht != "" ? "  •  " : "عندك ") + NaaqaUI.Ar(d) + " من " + NaaqaUI.Ar(wantDates) + " تمر";
        have.text = ht;
        have.color = ok ? NaaqaUI.OkColor : NaaqaUI.ErrorColor;
        deliver.interactable = ok;
    }

    void Deliver()
    {
        if (onDeliver == null) return;
        if (onDeliver())
        {
            answered = true;
            win.Close();
        }
        else win.SetStatus(wantDates > 0 ? "ما عندك تمر أو حليب كافي للطلب" : "الحليب ما يكفي، اجمع من إبلك", false);
    }

    void Refuse()
    {
        answered = true;
        if (onRefuse != null) onRefuse();
        win.Close();
    }
}
