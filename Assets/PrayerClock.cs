using UnityEngine;

// ===== الأذان (لمسة ثقافية) =====
// كل ١٠ دقايق لعب يطلع إشعار الأذان (وصوت لو موجود). ما يقفّل شي.
// اللاعب يقدر يطفيه من الإعدادات.
public static class PrayerClock
{
    public const float Cycle = 600f;      // بين أذان وأذان
    public const float Closed = 0f;       // السوق ما يقفّل

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt("naaqa_adhan", 1) == 1; }
        set { PlayerPrefs.SetInt("naaqa_adhan", value ? 1 : 0); }
    }

    static readonly string[] Names = { "الفجر", "الظهر", "العصر", "المغرب", "العشاء" };
    static int lastAnnounced = -1;

    static float T { get { return Time.realtimeSinceStartup + Cycle - 120f; } }   // أول أذان بعد دقيقتين

    public static bool IsPrayerTime { get { return false; } }   // ما فيه إغلاق
    static bool InAdhan { get { return (T % Cycle) < 8f; } }
    public static float SecondsToOpen { get { return IsPrayerTime ? Closed - (T % Cycle) : 0f; } }
    public static float SecondsToNext { get { return IsPrayerTime ? 0f : Cycle - (T % Cycle); } }
    public static string CurrentName { get { return Names[((int)(T / Cycle)) % Names.Length]; } }
    public static string NextName { get { return Names[((int)(T / Cycle) + 1) % Names.Length]; } }

    // يناديها أي سكربت كل فريم (السوق أو المسجد) عشان تطلع رسالة الأذان والفتح مرة وحدة
    public static void Tick()
    {
        int n = (int)(T / Cycle);
        if (InAdhan && n != lastAnnounced)
        {
            lastAnnounced = n;
            if (!Enabled) return;
            Notify("الله أكبر.. حان وقت أذان " + CurrentName);
            CamelSounds.Play("adhan", 10f);
        }
    }

    static void Notify(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
    }
}
