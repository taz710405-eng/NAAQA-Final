using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== العمال =====
// كل حلابينك، وكل واحد يمسك أي حوشين (تغيّرها بزر «غيّر الأحواش»)، وزر توظيف حلاب جديد.
public class WorkerScreen : MonoBehaviour
{
    static WorkerScreen instance;
    NaaqaWindow win;
    RectTransform list;
    RTLTextMeshPro hireText;
    Button hire;
    readonly List<GameObject> rows = new List<GameObject>();

    public static void Show()
    {
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv < GameRules.WorkersUnlock)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowWarning("التوظيف يفتح في المستوى " + NaaqaUI.Ar(GameRules.WorkersUnlock) + " (أنت في " + NaaqaUI.Ar(lv) + ")");
            return;
        }
        if (instance == null)
        {
            instance = new GameObject("WorkerScreen").AddComponent<WorkerScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Rebuild();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Workers", "العمال", "worker", new Vector2(1200, 800));
        win.transform.SetParent(transform, false);

        RectTransform area = NaaqaUI.Rect("Area", win.body);
        NaaqaUI.Stretch(area, 0, 0, 0, 110);
        list = win.List(area, 12);

        hire = NaaqaUI.MakeButton(win.body, "Hire", "", NaaqaUI.ButtonStyle.Primary, 28, out hireText);
        NaaqaUI.Place((RectTransform)hire.transform, new Vector2(0.5f, 0), new Vector2(560, 96), new Vector2(0, 6));
        hire.onClick.AddListener(() =>
        {
            string err = Milker.Hire();
            if (err != null) win.SetStatus(err, false); else win.SetStatus("توظف حلاب جديد!", true);
            Rebuild();
        });
    }

    void Rebuild()
    {
        foreach (GameObject g in rows) if (g != null) Destroy(g);
        rows.Clear();

        win.subtitle.text = "كل حلاب يمسك حوشين، يطعم إبلها ويحلبها  •  ياخذ " + NaaqaUI.Ar(Milker.FeePerMilk) + " قرش من كل حليب";

        for (int i = 0; i < Milker.Workers.Count; i++)
        {
            int idx = i;
            Milker.Worker w = Milker.Workers[i];
            RTLTextMeshPro main, sub, bt;
            Button b;
            Image row = NaaqaWindow.Row(list, "worker", NaaqaUI.Load("npc_milker"), 150, out main, out sub, out b, out bt, "غيّر الأحواش", 230);
            main.text = "حلاب " + NaaqaUI.Ar(i + 1);
            sub.text = "يمسك: " + Milker.PensText(w);
            b.onClick.AddListener(() => { Milker.NextPens(idx); Rebuild(); });
            if (Milker.Options().Count <= 1) b.interactable = false;

            // استغنِ عنه
            RTLTextMeshPro ft;
            Button fire = NaaqaUI.MakeButton(row.transform, "Fire", "استغنِ", NaaqaUI.ButtonStyle.Secondary, 22, out ft);
            NaaqaUI.Place((RectTransform)fire.transform, new Vector2(0, 0.5f), new Vector2(150, 62), new Vector2(256, 0));
            fire.onClick.AddListener(() => { Milker.Fire(idx); Rebuild(); });
            main.rectTransform.offsetMin = new Vector2(420, main.rectTransform.offsetMin.y);
            sub.rectTransform.offsetMin = new Vector2(420, sub.rectTransform.offsetMin.y);
            rows.Add(row.gameObject);
        }

        if (Milker.Workers.Count == 0)
        {
            RTLTextMeshPro t = NaaqaWindow.Header(list, "ما عندك عمال للحين. وظّف حلاب يشتغل عنك");
            t.alignment = TextAlignmentOptions.Center;
            t.color = NaaqaUI.Muted;
            rows.Add(t.gameObject);
        }

        bool can = Milker.Workers.Count < Milker.MaxWorkers;
        hireText.text = can ? "وظّف حلاب • " + NaaqaUI.Ar(Milker.HireCost) + " قرش"
                            : "عندك حلاب لكل حوشين";
        hire.interactable = can;
    }
}
