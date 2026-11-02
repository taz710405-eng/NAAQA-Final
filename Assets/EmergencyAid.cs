using UnityEngine;
using UnityEngine.SceneManagement;

// ===== المساعدة الطارئة =====
// لو اللاعب علق: قروشه أقل من ٥، وما عنده علف ولا ماء
// نعطيه ٥ علف و٥ ماء و١٠ قروش، مرة كل ٧٢ ساعة، بدون خبرة.
// الماء ينحط في الآبار بحد سعتها. لو ما عنده بير متركب (أو ما فيه مكان)، الباقي ينتظر
// (PendingWater، ينحفظ) وينحط أول ما يركّب بير. ما يضيع ولا ينعطى أكثر من السعة.
// الوقت من السيرفر (ServerTime)، وما تشتغل إلا بعد ما ناخذ وقت السيرفر.
public class EmergencyAid : MonoBehaviour
{
    public static long LastUnix;   // آخر مرة أخذها (ثواني يونكس، وقت السيرفر)
    public static int PendingWater;   // ماء المساعدة اللي ينتظر بير
    static EmergencyAid instance;
    float timer = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { LastUnix = 0; PendingWater = 0; }

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
        instance = new GameObject("EmergencyAid").AddComponent<EmergencyAid>();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    static long Now { get { return ServerTime.UnixLong; } }

    // نحط الماء المعلّق في الآبار لو فيه مكان
    static void FlushWater(FarmStore fs, bool notify = true)
    {
        if (PendingWater <= 0 || fs.WaterCap <= 0 || fs.Water >= fs.WaterCap) return;
        int got = fs.AddWater(PendingWater);
        PendingWater -= got;
        if (notify && got > 0 && NotificationManager.Instance != null)
            NotificationManager.Instance.Show("انحط ماء الفزعة في البير +" + NaaqaUI.Ar(got) + " ماء");
    }

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 5f;
        if (SaveManager.Instance == null || !SaveManager.Instance.Ready) return;
        if (!WaterSystem.TutorialDone || TutorialEnd.Stage < TutorialEnd.Done) return;
        GameManager gm = GameManager.Instance;
        FarmStore fs = FarmStore.Instance;
        if (gm == null || fs == null) return;
        FlushWater(fs);
        if (!ServerTime.Synced) return;   // ما نعتمد على ساعة الجهاز
        if (PendingWater > 0) return;     // عنده ماء فزعة ينتظر بير، فهو مو عالق
        if (gm.money >= GameRules.AidCoinsBelow || gm.feedCount > 0 || fs.Water > 0) return;
        if (LastUnix > 0 && Now - LastUnix < GameRules.AidCooldownHours * 3600) return;

        LastUnix = Now;
        gm.AddFeed(GameRules.AidFeed);
        PendingWater = GameRules.AidWater;
        FlushWater(fs, false);   // ينحط الحين لو فيه بير، والباقي ينتظر
        gm.AddMoney(GameRules.AidCoins);
        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show(PendingWater > 0
                ? "وصلتك فزعة! ٥ علف و١٠ قروش، والماء ينتظرك أول ما تركّب بير"
                : "وصلتك فزعة! ٥ علف و٥ ماء و١٠ قروش عشان تكمل شغلك");
        if (SaveManager.Instance != null) SaveManager.Instance.SaveNow();
    }
}
