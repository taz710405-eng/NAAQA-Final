using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== اطلب وايت مويه =====
public class WaterTruckScreen : MonoBehaviour
{
    static WaterTruckScreen instance;
    NaaqaWindow win;
    readonly List<Button> buttons = new List<Button>();
    readonly List<RTLTextMeshPro> labels = new List<RTLTextMeshPro>();

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("WaterTruckScreen").AddComponent<WaterTruckScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Refresh();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("WaterTruck", "اطلب وايت مويه", "water_truck", new Vector2(1100, 720));
        win.transform.SetParent(transform, false);
        win.onTick = Refresh;
        RectTransform area = NaaqaUI.Rect("Area", win.body);
        NaaqaUI.Stretch(area, 0, 0, 0, 40);
        RectTransform list = win.List(area, 14);

        for (int i = 0; i < WaterSystem.TruckTons.Length; i++)
        {
            int idx = i;
            RTLTextMeshPro main, sub, bt;
            Button b;
            NaaqaWindow.Row(list, "water_truck", null, 150, out main, out sub, out b, out bt,
                "اطلب • " + NaaqaUI.Ar(WaterSystem.TruckPrices[i]) + " قرش", 270);
            main.text = "وايت " + NaaqaUI.Ar(WaterSystem.TruckTons[i]) + " طن";
            sub.text = "يعبي " + NaaqaUI.Ar(WaterSystem.TruckTons[i]) + " مويه في آبارك";
            b.onClick.AddListener(() => Order(idx));
            buttons.Add(b);
            labels.Add(bt);
        }
    }

    void Order(int i)
    {
        string err = WaterSystem.Order(i);
        if (err != null) { win.SetStatus(err, false); return; }
        win.Close();
    }

    void Refresh()
    {
        FarmStore st = FarmStore.Instance;
        int water = st != null ? st.Water : 0, cap = st != null ? st.WaterCap : 0;
        int money = GameManager.Instance != null ? GameManager.Instance.money : 0;
        win.subtitle.text = cap <= 0 ? "ما عندك بير! ركّب بير أول"
            : WaterSystem.TruckOnWay ? "الوايت جاي بالطريق..."
            : "عندك " + NaaqaUI.Ar(water) + " من " + NaaqaUI.Ar(cap) + " مويه  •  البير يتعبى لحاله حبة كل دقيقة ونص، والوايت أسرع";
        bool intro = WaterSystem.IntroActive;
        for (int i = 0; i < buttons.Count; i++)
        {
            bool free = intro && i == WaterSystem.IntroTruck;
            buttons[i].interactable = cap > 0 && !WaterSystem.TruckOnWay && cap - water >= WaterSystem.TruckTons[i]
                                      && (free || (!intro && money >= WaterSystem.TruckPrices[i]));
            string want = free ? "اطلب • مجانًا" : "اطلب • " + NaaqaUI.Ar(WaterSystem.TruckPrices[i]) + " قرش";
            if (labels[i].text != want) labels[i].text = want;
        }
        if (intro && cap > 0) win.subtitle.text = "أول وايت عليك هدية! اطلب وايت ٢٥ طن";
    }
}
