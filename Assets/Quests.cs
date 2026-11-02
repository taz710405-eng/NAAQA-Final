using UnityEngine;
using System.Collections.Generic;

// ===== المهام (بعد الشرح) =====
// مهام منوعة تتجدد: كل مهمة لها مراحل، وكل ما خلصت مرحلة تستلم جائزتها وتطلع لك المرحلة الأصعب.
// السكربتات الثانية تنادي Quests.Add("milk") مثلًا، والعدادات تنحفظ مع التقدم.
public static class Quests
{
    public class Quest
    {
        public string id, title, icon;
        public int[] goals;        // المراحل
        public int[] rewards;      // جائزة كل مرحلة (قروش)
        public bool absolute;      // يقيس قيمة حالية (مثل مستوى العزبة) بدل عداد
        public Quest(string id, string title, string icon, int[] goals, int[] rewards, bool absolute = false)
        { this.id = id; this.title = title; this.icon = icon; this.goals = goals; this.rewards = rewards; this.absolute = absolute; }
    }

    public static readonly Quest[] All =
    {
        new Quest("milk",   "اجمع {0} حليب من إبلك",       "milk",         new[] { 10, 30, 80, 200, 500 },  new[] { 10, 20, 35, 60, 100 }),
        new Quest("feed",   "أطعم إبلك {0} مرة",           "feed",         new[] { 10, 30, 80, 200, 500 },  new[] { 8, 15, 25, 45, 75 }),
        new Quest("orders", "سلّم {0} طلبات",               "orders",       new[] { 3, 10, 25, 60, 150 },    new[] { 10, 20, 35, 60, 100 }),
        new Quest("sell",   "بِع {0} حليب من كشك البيع", "sell",         new[] { 10, 40, 100, 250, 600 }, new[] { 8, 15, 25, 45, 75 }),
        new Quest("camel",  "اشترِ {0} ناقة من سوق الإبل",  "camel_market", new[] { 1, 2, 3 },               new[] { 20, 40, 70 }),
        new Quest("pen",    "طوّر الحوش لمستوى {0}",        "upgrade",      new[] { 3, 4, 5 },               new[] { 10, 20, 35 }, true),
        new Quest("decor",  "ركّب {0} أغراض في حوشك",       "decor",        new[] { 1, 3, 6, 10 },           new[] { 5, 10, 20, 35 }),
        new Quest("race",   "فز بـ {0} سباق هجن",           "race",         new[] { 1, 3, 10 },              new[] { 20, 45, 100 }),
        new Quest("mazayen","فز بـ {0} مزاين",              "mazayen",      new[] { 1, 3, 10 },              new[] { 25, 55, 120 }),
    };

    static readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    static readonly Dictionary<string, int> stage = new Dictionary<string, int>();   // كم مرحلة استلم
    public static System.Action OnChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { counts.Clear(); stage.Clear(); }

    public static void Add(string id, int n = 1)
    {
        if (n <= 0) return;
        counts[id] = Count(id) + n;
        if (OnChanged != null) OnChanged();
    }

    public static int Count(string id) { int v; return counts.TryGetValue(id, out v) ? v : 0; }
    public static int Stage(string id) { int v; return stage.TryGetValue(id, out v) ? v : 0; }

    public static int Progress(Quest q)
    {
        if (q.absolute && q.id == "pen") return FarmStore.Instance != null ? FarmStore.Instance.PenLevel : 1;
        if (q.id == "race") return FarmStore.Instance != null ? FarmStore.Instance.RaceWins : 0;
        if (q.id == "mazayen") return FarmStore.Instance != null ? FarmStore.Instance.MazayenWins : 0;
        return Count(q.id);
    }

    public static bool Finished(Quest q) { return Stage(q.id) >= q.goals.Length; }
    public static int Goal(Quest q) { return q.goals[Mathf.Min(Stage(q.id), q.goals.Length - 1)]; }
    public static int Reward(Quest q) { return q.rewards[Mathf.Min(Stage(q.id), q.rewards.Length - 1)]; }
    public static bool Ready(Quest q) { return !Finished(q) && Progress(q) >= Goal(q); }

    public static int ReadyCount
    {
        get { int n = 0; foreach (Quest q in All) if (Ready(q)) n++; return n; }
    }

    public static bool Claim(Quest q)
    {
        if (!Ready(q)) return false;
        int reward = Reward(q);
        stage[q.id] = Stage(q.id) + 1;
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddMoney(reward);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }
        LevelManager.AddXp(10 + Stage(q.id) * 5);
        if (OnChanged != null) OnChanged();
        return true;
    }

    // ===== الحفظ =====
    [System.Serializable] public class Entry { public string id; public int count, stage; }

    public static List<Entry> Save()
    {
        List<Entry> l = new List<Entry>();
        foreach (Quest q in All) l.Add(new Entry { id = q.id, count = Count(q.id), stage = Stage(q.id) });
        return l;
    }

    public static void Restore(List<Entry> saved)
    {
        counts.Clear(); stage.Clear();
        if (saved != null)
            foreach (Entry e in saved)
            {
                if (string.IsNullOrEmpty(e.id)) continue;
                counts[e.id] = Mathf.Max(0, e.count);
                stage[e.id] = Mathf.Max(0, e.stage);
            }
        if (OnChanged != null) OnChanged();
    }
}
