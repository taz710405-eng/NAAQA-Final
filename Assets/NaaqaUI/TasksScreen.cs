using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== المهام (بعد الشرح) =====
// مهام منوعة لها مراحل: اجمع حليب، أطعم إبلك، سلّم طلبات، بع في السوق، طوّر الحوش...
// كل ما خلصت مرحلة تستلم جائزتها وتطلع المرحلة الأصعب.
public class TasksScreen : MonoBehaviour
{
    static TasksScreen instance;
    NaaqaWindow win;
    RectTransform list;
    readonly List<GameObject> rows = new List<GameObject>();

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("TasksScreen").AddComponent<TasksScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Rebuild();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Tasks", "المهام", "tasks", new Vector2(1100, 860));
        win.transform.SetParent(transform, false);
        list = win.List(win.body, 12);
    }

    void Rebuild()
    {
        foreach (GameObject g in rows) if (g != null) Destroy(g);
        rows.Clear();

        // الجاهزة للاستلام أول، والمخلصة آخر
        List<Quests.Quest> order = new List<Quests.Quest>(Quests.All);
        order.Sort((a, b) => Rank(a).CompareTo(Rank(b)));

        int ready = Quests.ReadyCount;
        win.subtitle.text = ready > 0 ? "عندك " + NaaqaUI.Ar(ready) + " جائزة تنتظرك!" : "خلّص المهام واستلم جوائزها";

        foreach (Quests.Quest q in order)
        {
            Quests.Quest quest = q;
            bool done = Quests.Finished(q);
            bool canClaim = Quests.Ready(q);
            int goal = Quests.Goal(q);
            int prog = Mathf.Min(Quests.Progress(q), goal);

            RTLTextMeshPro main, sub, bt;
            Button btn;
            Image row = NaaqaWindow.Row(list, q.icon, null, 112, out main, out sub, out btn, out bt,
                done ? "خلصت" : canClaim ? "استلم" : NaaqaUI.Ar(prog) + " / " + NaaqaUI.Ar(goal), 190);
            main.text = done ? q.title.Replace("{0}", NaaqaUI.Ar(q.goals[q.goals.Length - 1])) : q.title.Replace("{0}", NaaqaUI.Ar(goal));
            main.fontSize = 26;
            sub.text = done ? "خلصت كل المراحل، ما شاء الله"
                : "المرحلة " + NaaqaUI.Ar(Quests.Stage(q.id) + 1) + " من " + NaaqaUI.Ar(q.goals.Length) + "  •  الجائزة " + NaaqaUI.Ar(Quests.Reward(q)) + " قرش";

            // شريط التقدم تحت النص
            Image track = NaaqaUI.Img(row.transform, "Track", NaaqaUI.Rounded(), new Color(1, 1, 1, 0.1f), 6f);
            track.rectTransform.anchorMin = new Vector2(0, 0); track.rectTransform.anchorMax = new Vector2(1, 0);
            track.rectTransform.offsetMin = new Vector2(216, 6); track.rectTransform.offsetMax = new Vector2(-116, 13);
            Image fill = NaaqaUI.Img(track.transform, "Fill", NaaqaUI.RoundedGradient(NaaqaUI.OrangeLight, NaaqaUI.OrangeDark, "btnPrimary"), Color.white, 6f);
            float k = done ? 1f : goal > 0 ? prog / (float)goal : 0f;
            fill.rectTransform.anchorMin = new Vector2(1f - k, 0); fill.rectTransform.anchorMax = new Vector2(1, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            if (canClaim) row.color = new Color(1f, 0.75f, 0.35f, 0.16f);
            btn.interactable = canClaim;
            btn.onClick.AddListener(() =>
            {
                int reward = Quests.Reward(quest);
                if (Quests.Claim(quest))
                {
                    win.SetStatus("+" + NaaqaUI.Ar(reward) + " قرش! مرحلة جديدة انفتحت", true);
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
                }
                Rebuild();
            });
            rows.Add(row.gameObject);
        }
    }

    static int Rank(Quests.Quest q)
    {
        if (Quests.Ready(q)) return 0;
        if (Quests.Finished(q)) return 2;
        return 1;
    }
}
