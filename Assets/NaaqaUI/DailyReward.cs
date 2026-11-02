using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;

// ===== مكافأة الدخول اليومي =====
// دورة ٧ أيام: ١٥، ٢٠، ٢٥، ٣٠، ٣٥، ٤٥، ٨٠ (المجموع ٢٥٠). مكافأة وحدة كل يوم.
// لو فوّت يوم ما يرجع للأول: اليوم الفايت يروح عليه ويكمل الأسبوع.
// اليوم ينحسب بوقت السيرفر (Supabase) مو ساعة الجهاز، بتوقيت السعودية.
// تطلع شاشتها بعد ما يخلص الشرح كامل (بعد «يا هلا ومرحبا بك في ناقة»)، ومرة كل يوم بعدها.
public class DailyReward : MonoBehaviour
{
    public static int[] Rewards { get { return GameRules.DailyRewards; } }

    public static int Streak;     // آخر يوم استلمه في الدورة (١ لين ٧)
    public static long LastDay;   // رقم اليوم اللي استلم فيه
    public static long CycleStart;   // أول يوم في الدورة الحالية
    public static int ClaimedMask;   // الأيام اللي استلمها في الدورة (بت لكل يوم)

    static DailyReward instance;
    NaaqaWindow win;
    Image[] tiles = new Image[7];
    RTLTextMeshPro[] tileText = new RTLTextMeshPro[7];
    Image[] checks = new Image[7];
    Button claim;
    RTLTextMeshPro claimText;
    float wait = 2.5f;
    bool claimedNow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Streak = 0; LastDay = 0; CycleStart = 0; ClaimedMask = 0; }

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
        instance = new GameObject("DailyReward").AddComponent<DailyReward>();
    }

    void OnDestroy() { if (instance == this) instance = null; if (win != null) Destroy(win.gameObject); }

    // ===== وقت السيرفر =====
    // (ServerTime ياخذه من Supabase، وSaveManager هو اللي يسوي الطلب)
    static bool synced { get { return ServerTime.Synced; } }
    public static System.DateTime ServerUtc { get { return ServerTime.Utc; } }
    public static bool Synced { get { return ServerTime.Synced; } }

    // اليوم بتوقيت السعودية (UTC+3) من وقت السيرفر
    static long Today { get { return (long)(ServerUtc.AddHours(3).Date - new System.DateTime(2020, 1, 1)).TotalDays; } }

    public static bool Available { get { return synced && LastDay != Today; } }

    // الدورة: لو مر أسبوع من أولها تبدأ دورة جديدة
    static bool NewCycle { get { return CycleStart <= 0 || Today - CycleStart >= 7 || Today < CycleStart; } }

    // اليوم اللي بيستلمه الحين (١ لين ٧): حسب كم يوم مر من أول الدورة (الأيام الفايتة تروح)
    public static int NextDay { get { return NewCycle ? 1 : (int)(Today - CycleStart) + 1; } }

    void Update()
    {
        if (win != null && win.IsOpen) return;
        if (!Available || !Ready()) { wait = 2.5f; return; }
        wait -= Time.unscaledDeltaTime;
        if (wait > 0f) return;
        Show();
    }

    // نطلعها بس بعد الشرح كامل، وما فيه شي ثاني مفتوح
    static bool Ready()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.Ready) return false;
        if (!WaterSystem.TutorialDone || TutorialEnd.Stage < TutorialEnd.Done) return false;
        if (PlacementManager.IsPlacing) return false;
        UIManager ui = UIManager.Instance;
        if (ui != null && ((ui.startScreen != null && ui.startScreen.activeInHierarchy) || (ui.hud != null && !ui.hud.activeInHierarchy))) return false;
        if (SouqScreen.Instance != null && SouqScreen.Instance.gameObject.activeInHierarchy) return false;
        foreach (NaaqaWindow w in FindObjectsByType<NaaqaWindow>(FindObjectsInactive.Exclude)) if (w.IsOpen) return false;
        return true;
    }

    void Show()
    {
        if (win == null) Build();
        claimedNow = false;
        Refresh();
        win.Open();
    }

    void Build()
    {
        win = NaaqaWindow.Create("DailyReward", "مكافأة الدخول اليومي", "mail", new Vector2(1300, 640), 820);
        win.transform.SetParent(transform, false);
        win.subtitle.text = "ادخل كل يوم واستلم قروشك، واليوم السابع جايزته أكبر! لو فوّت يوم يروح عليك وتكمل الأسبوع";

        // سبع خانات (من اليمين لليسار)
        float w = 160f, gap = 14f;
        float total = 7 * w + 6 * gap;
        for (int i = 0; i < 7; i++)
        {
            Image t = NaaqaUI.Tile(win.body, "Day" + (i + 1));
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(w, i == 6 ? 230f : 210f);
            r.anchoredPosition = new Vector2(total / 2f - w / 2f - i * (w + gap), -14f);
            tiles[i] = t;

            RTLTextMeshPro day = NaaqaUI.Text(t.transform, "Day", "اليوم " + NaaqaUI.Ar(i + 1), 24, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
            NaaqaUI.Place(day.rectTransform, new Vector2(0.5f, 1f), new Vector2(w - 10f, 36f), new Vector2(0f, -10f));

            Image coin = NaaqaUI.IconImg(t.transform, "coin", i == 6 ? 84 : 66);
            NaaqaUI.Place(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i == 6 ? 84 : 66, i == 6 ? 84 : 66), new Vector2(0f, 8f));

            RTLTextMeshPro amt = NaaqaUI.Text(t.transform, "Amount", NaaqaUI.Ar(Rewards[i]), i == 6 ? 34 : 28, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
            NaaqaUI.Place(amt.rectTransform, new Vector2(0.5f, 0f), new Vector2(w - 10f, 40f), new Vector2(0f, 12f));
            tileText[i] = amt;

            Image ok = NaaqaUI.IconImg(t.transform, "ok", 54);
            NaaqaUI.Place(ok.rectTransform, new Vector2(1f, 1f), new Vector2(54, 54), new Vector2(-6f, -6f));
            checks[i] = ok;
        }

        claim = NaaqaUI.MakeButton(win.body, "Claim", "", NaaqaUI.ButtonStyle.Primary, 32, out claimText);
        NaaqaUI.Place((RectTransform)claim.transform, new Vector2(0.5f, 0f), new Vector2(460, 92), new Vector2(0f, 24f));
        claim.onClick.AddListener(Claim);
        // لو سكّرها بدون ما يضغط «استلم» نعطيه مكافأته برضو (عشان ما ترجع تطلع له)
        win.onClose = () => { if (!claimedNow && Available) Give(); };
    }

    void Refresh()
    {
        int next = claimedNow ? Streak : NextDay;
        int mask = claimedNow || !NewCycle ? ClaimedMask : 0;
        for (int i = 0; i < 7; i++)
        {
            bool got = (mask & (1 << i)) != 0;
            bool today = i == next - 1;
            bool missed = !got && i < next - 1;
            tiles[i].color = today ? new Color(1f, 0.78f, 0.38f, 0.32f) : got ? new Color(1f, 1f, 1f, 0.03f) : missed ? new Color(0f, 0f, 0f, 0.25f) : new Color(1f, 1f, 1f, 0.08f);
            checks[i].gameObject.SetActive(got);
        }
        int amount = Rewards[next - 1];
        claimText.text = claimedNow ? "تمام" : "استلم +" + NaaqaUI.Ar(amount) + " قرش";
    }

    void Claim()
    {
        if (claimedNow || !Available) { win.Close(); return; }
        int day = NextDay, amount = Give();
        win.SetStatus(day == 7 ? "كفو! خلصت الأسبوع كامل +" + NaaqaUI.Ar(amount) + " قرش" : "استلمت +" + NaaqaUI.Ar(amount) + " قرش، تعال بكرة!", true);
        CamelSounds.Play("pour");
        StartCoroutine(CloseLater());
    }

    int Give()
    {
        if (!Available) return 0;   // ما تنستلم مرتين باليوم
        if (NewCycle) { CycleStart = Today; ClaimedMask = 0; }
        int day = NextDay;
        int amount = Rewards[day - 1];
        ClaimedMask |= 1 << (day - 1);
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddMoney(amount);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }
        Streak = day;
        LastDay = Today;
        claimedNow = true;
        Refresh();
        return amount;
    }

    System.Collections.IEnumerator CloseLater()
    {
        yield return new WaitForSecondsRealtime(1.6f);
        if (win != null && win.IsOpen) win.Close();
    }
}
