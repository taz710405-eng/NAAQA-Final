using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ===== الحلابين =====
// كل حلاب بـ٩٠٠ قرش ويمسك حوشين كحد أقصى (تختار أي حوشين من نافذة العمال).
// يمشي بين حوشينه (على الطريق الترابي لو فيه)، ويطعم الإبل من علفك ومويتك ويحلبها، وياخذ قرش من كل حليب.
// وأنت برا اللعبة يكمل شغله لين ٨ ساعات، ويوقف لو خلص العلف أو المويه أو القروش (ما ينحسب مرتين).
public class Milker : MonoBehaviour
{
    public const int HireCost = GameRules.WorkerPrice;
    public const int FeePerMilk = GameRules.WorkerFeePerMilk;

    [System.Serializable] public class Worker { public int a = -1; public int b = -1; }
    public static readonly List<Worker> Workers = new List<Worker>();
    public static bool Hired { get { return Workers.Count > 0; } }

    static Milker instance;
    readonly List<Agent> agents = new List<Agent>();
    static int milkSince, feedSince, feeSince;
    float reportTimer = 25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Workers.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (instance != null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        instance = new GameObject("Milkers").AddComponent<Milker>();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    // ===== الأحواش =====
    public static int PenCount { get { return 1 + ExtraPen.All().Count; } }
    public static int MaxWorkers { get { return (PenCount + 1) / 2; } }
    public static bool PenValid(int i) { return i == -1 || (i >= 0 && i < ExtraPen.All().Count); }
    public static string PenName(int i) { return i == -1 ? "الحوش الرئيسي" : "حوش إضافي " + NaaqaUI.Ar(i + 1); }

    // كل الخيارات: حوشين (أو حوش واحد لو ما فيه غيره)
    public static List<Vector2Int> Options()
    {
        List<int> pens = new List<int> { -1 };
        for (int i = 0; i < ExtraPen.All().Count; i++) pens.Add(i);
        List<Vector2Int> l = new List<Vector2Int>();
        if (pens.Count == 1) { l.Add(new Vector2Int(-1, -1)); return l; }
        for (int x = 0; x < pens.Count; x++)
            for (int y = x + 1; y < pens.Count; y++) l.Add(new Vector2Int(pens[x], pens[y]));
        return l;
    }

    public static string Hire()
    {
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv < GameRules.WorkersUnlock) return "التوظيف يفتح في المستوى " + NaaqaUI.Ar(GameRules.WorkersUnlock);
        if (Workers.Count >= MaxWorkers)
            return "كل حلاب يمسك حوشين، وعندك " + NaaqaUI.Ar(PenCount) + (PenCount == 1 ? " حوش" : " أحواش") + ". زد أحواشك عشان توظف أكثر";
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.SpendMoney(HireCost)) return "ما عندك قروش كافية! التوظيف يبي " + NaaqaUI.Ar(HireCost) + " قرش";
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();

        // نعطيه أول حوشين ما أحد ماسكهم
        Worker w = new Worker();
        List<Vector2Int> opts = Options();
        w.a = opts[0].x; w.b = opts[0].y;
        foreach (Vector2Int o in opts)
        {
            bool used = false;
            foreach (Worker x in Workers) if (Covers(x, o.x) || Covers(x, o.y)) { used = true; break; }
            if (!used) { w.a = o.x; w.b = o.y; break; }
        }
        Workers.Add(w);
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("توظف حلاب جديد! بيمسك " + PensText(w));
        return null;
    }

    public static string PensText(Worker w)
    {
        return w.a == w.b ? PenName(w.a) : PenName(w.a) + " و" + PenName(w.b);
    }

    static bool Covers(Worker w, int pen) { return w.a == pen || w.b == pen; }

    public static void Fire(int i)
    {
        if (i < 0 || i >= Workers.Count) return;
        Workers.RemoveAt(i);
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("استغنيت عن الحلاب");
    }

    public static void NextPens(int i)
    {
        if (i < 0 || i >= Workers.Count) return;
        List<Vector2Int> opts = Options();
        Worker w = Workers[i];
        int cur = opts.FindIndex(o => o.x == w.a && o.y == w.b);
        Vector2Int n = opts[(cur + 1) % opts.Count];
        w.a = n.x; w.b = n.y;
    }

    // ===== الحفظ =====
    public static List<Worker> Save()
    {
        List<Worker> l = new List<Worker>();
        foreach (Worker w in Workers) l.Add(new Worker { a = w.a, b = w.b });
        return l;
    }

    public static void Restore(List<Worker> saved, bool oldHired)
    {
        Workers.Clear();
        if (saved != null) foreach (Worker w in saved) if (w != null) Workers.Add(new Worker { a = w.a, b = w.b });
        if (Workers.Count == 0 && oldHired) Workers.Add(new Worker { a = -1, b = 0 });   // الحفظ القديم (حلاب واحد)
    }

    // ===== الشغل وأنت برا اللعبة =====
    // SaveManager يعطينا حالة الإبل وقت الحفظ والمدة اللي كان فيها برا، ونحسبها مرة وحدة بعد ما تجهز الأحواش
    static List<SaveManager.CamelEntry> offlineCamels;
    static float offlineAway, offlineDelay;

    public static void QueueOffline(List<SaveManager.CamelEntry> saved, float away)
    {
        offlineCamels = null;
        if (saved == null || away <= 1f || Workers.Count == 0) return;
        offlineCamels = new List<SaveManager.CamelEntry>(saved);
        offlineAway = Mathf.Min(away, GameRules.WorkerOfflineMaxHours * 3600f);
        offlineDelay = 1.5f;
    }

    void RunOffline()
    {
        List<SaveManager.CamelEntry> list = offlineCamels;
        offlineCamels = null;
        GameManager gm = GameManager.Instance;
        FarmStore st = FarmStore.Instance;
        if (list == null || gm == null || st == null) return;

        HashSet<int> pens = new HashSet<int>();
        foreach (Worker w in Workers) { pens.Add(w.a); pens.Add(w.b); }

        int milk = 0, fees = 0;
        foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Exclude))
        {
            if (!pens.Contains(PenVisual.PenIndexOf(c))) continue;
            SaveManager.CamelEntry e = list.Find(x => x.name == c.gameObject.name);
            if (e == null) continue;
            float P = Mathf.Max(1f, c.productionTime);
            float T = offlineAway, t = 0f;
            bool ready = e.ready, producing = e.producing && !e.ready;
            float left = e.remaining;
            bool stopped = false;
            while (!stopped)
            {
                if (producing)
                {
                    if (t + left > T) { left = t + left - T; break; }   // للحين تنتج
                    t += left; producing = false; ready = true;
                }
                if (ready)
                {
                    if (gm.money < FeePerMilk) break;                 // ما فيه قروش يدفع للحلاب: يوقف
                    gm.money -= FeePerMilk; fees += FeePerMilk;
                    milk++; ready = false;
                }
                if (t >= T) break;
                if (gm.feedCount < 1 || st.Water < 1) { stopped = true; break; }   // خلص العلف أو المويه: يوقف
                gm.feedCount -= 1;
                st.UseWater(1);
                producing = true; left = P;
            }
            c.RestoreState(ready, producing, producing ? left : 0f);
        }

        if (milk > 0)
        {
            gm.AddMilk(milk);
            Quests.Add("milk", milk);
            LevelManager.AddXp(LevelManager.XpPerMilk * milk);
        }
        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        if (milk > 0 && NotificationManager.Instance != null)
            NotificationManager.Instance.Show("وأنت غايب، الحلابين جمعوا " + NaaqaUI.Ar(milk) + " حليب (-" + NaaqaUI.Ar(fees) + " قرش)");
    }

    // ===== التشغيل =====
    void Update()
    {
        if (offlineCamels != null && PenVisual.Instance != null)
        {
            offlineDelay -= Time.unscaledDeltaTime;
            if (offlineDelay <= 0f) RunOffline();
        }

        bool run = WaterSystem.TutorialDone && PenVisual.Instance != null;
        while (agents.Count < (run ? Workers.Count : 0)) agents.Add(new GameObject("Milker_" + agents.Count).AddComponent<Agent>());
        while (agents.Count > (run ? Workers.Count : 0)) { Agent g = agents[agents.Count - 1]; agents.RemoveAt(agents.Count - 1); if (g != null) Destroy(g.gameObject); }
        for (int i = 0; i < agents.Count; i++) agents[i].worker = Workers[i];

        reportTimer -= Time.deltaTime;
        if (reportTimer <= 0f)
        {
            reportTimer = 25f;
            if ((milkSince > 0 || feedSince > 0) && NotificationManager.Instance != null)
                NotificationManager.Instance.Show("الحلابين: جمعوا " + NaaqaUI.Ar(milkSince) + " حليب وأطعموا " + NaaqaUI.Ar(feedSince) + " (-" + NaaqaUI.Ar(feeSince) + " قرش)");
            milkSince = feedSince = feeSince = 0;
        }
    }

    // حلاب واحد يمشي ويشتغل
    public class Agent : MonoBehaviour
    {
        public Worker worker;
        SpriteRenderer sr;
        Sprite idle, walk;
        int goal = -2;                 // الحوش اللي رايح له
        readonly List<Vector3> way = new List<Vector3>();
        float workTimer, stayTimer;
        int idleTicks;
        bool placed;

        void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            idle = Load("npc_milker");
            walk = Load("npc_milker_walk");
            if (idle == null) idle = Load("npc_man");
            sr.sprite = idle;
        }

        static Sprite Load(string n)
        {
            Texture2D t = Resources.Load<Texture2D>("NaaqaUI/" + n);
            return t != null ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.03f), t.height / 1.7f) : null;
        }

        void Update()
        {
            if (worker == null || sr == null || PenVisual.Instance == null) return;
            SpriteRenderer pen = PenVisual.Instance.GetComponent<SpriteRenderer>();
            sr.sortingLayerID = pen.sortingLayerID;
            sr.sortingOrder = pen.sortingOrder + 8;
            bool hide = PlacementManager.IsPlacing;
            if (sr.enabled == hide) sr.enabled = !hide;
            if (hide) return;

            int a = Milker.PenValid(worker.a) ? worker.a : -1;
            int b = Milker.PenValid(worker.b) ? worker.b : a;
            if (goal != a && goal != b) { goal = a; Plan(); }

            if (way.Count > 0)
            {
                Vector3 t = way[0];
                Vector3 p = transform.position;
                Vector3 np = Vector3.MoveTowards(p, t, 2.2f * Time.deltaTime);
                if (Mathf.Abs(np.x - p.x) > 0.0005f) sr.flipX = np.x < p.x;
                transform.position = np;
                sr.sprite = walk != null && Mathf.Sin(Time.time * 8f) > 0f ? walk : idle;
                if ((np - t).sqrMagnitude < 0.01f) way.RemoveAt(0);
                return;
            }

            sr.sprite = idle;
            // يشتغل عند الحوش
            workTimer -= Time.deltaTime;
            stayTimer -= Time.deltaTime;
            if (workTimer <= 0f)
            {
                workTimer = 1.5f;
                if (WorkOn(goal)) idleTicks = 0; else idleTicks++;
            }
            // يروح للحوش الثاني بعد شوي
            if (a != b && (stayTimer <= 0f || idleTicks >= 3))
            {
                goal = goal == a ? b : a;
                Plan();
            }
        }

        void Plan()
        {
            way.Clear();
            stayTimer = 8f; idleTicks = 0;
            Vector3 to;
            if (!PenVisual.PenFront(goal, out to)) return;
            if (!placed) { placed = true; transform.position = to; return; }
            // على الطريق الترابي لو يوصل بين الحوشين
            List<Vector3> r = DirtPaths.Route(transform.position, to, false, 5f);
            if (r != null) foreach (Vector3 v in r) way.Add(new Vector3(v.x, to.y, v.z));
            way.Add(to);
        }

        // يطعم أو يحلب ناقة وحدة في هالحوش
        bool WorkOn(int penIdx)
        {
            if (offlineCamels != null) return false;   // ننتظر حساب الشغل اللي صار وأنت برا
            GameManager gm = GameManager.Instance;
            foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Exclude))
            {
                if (PenVisual.PenIndexOf(c) != penIdx) continue;
                if (c.IsMilkReady && c.WorkerCollect())
                {
                    milkSince++;
                    if (gm != null && gm.SpendMoney(FeePerMilk)) feeSince += FeePerMilk;
                    if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
                    return true;
                }
                if (!c.IsProducing && !c.IsMilkReady && c.WorkerFeed()) { feedSince++; return true; }
            }
            return false;
        }
    }
}
