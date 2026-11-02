using UnityEngine;
using UnityEngine.Networking;

// ===== وقت السيرفر (مو ساعة الجهاز) =====
// ناخذ الوقت من دالة server_now في Supabase (ملف Supabase/server_time.sql)، ولو ما هي موجودة
// من رأس الرد «Date». المتصفح ما يخلي اللعبة تقرا رأس Date، فعلى الويب لازم الدالة.
// وبعدها نعدّ بساعة يونتي الداخلية
// (realtimeSinceStartup) اللي ما تتأثر لو اللاعب غيّر ساعة جهازه.
// يستخدمه الحفظ (وقت الغياب)، والمكافأة اليومية، والمساعدة الطارئة.
// مو مدير جديد: كلاس ثابت بس، والطلب نفسه يسويه SaveManager.
public static class ServerTime
{
    static double serverUnix;     // وقت السيرفر لحظة آخر مزامنة (ثواني يونكس)
    static double atRealtime;     // ساعة يونتي الداخلية لحظتها
    public static bool Synced { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Synced = false; serverUnix = 0; atRealtime = 0; }

    // الوقت الحين (ثواني يونكس). لو ما تزامن، يرجع ساعة الجهاز (ولا نعتمد عليه في الغياب)
    public static double Unix
    {
        get
        {
            if (!Synced) return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return serverUnix + (Time.realtimeSinceStartupAsDouble - atRealtime);
        }
    }

    public static long UnixLong { get { return (long)Unix; } }
    public static System.DateTime Utc { get { return new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(Unix); } }

    // ناخذ الوقت من رد أي طلب (حتى لو الطلب نفسه فشل بس السيرفر رد)
    public static bool FromResponse(UnityWebRequest req)
    {
        if (req == null || req.responseCode <= 0) return false;
        string date = req.GetResponseHeader("Date");
        if (string.IsNullOrEmpty(date)) return false;
        System.DateTime t;
        if (!System.DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out t))
            return false;
        serverUnix = (t - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        atRealtime = Time.realtimeSinceStartupAsDouble;
        Synced = true;
        return true;
    }

    // الوقت من جسم الرد: "2026-11-01T05:20:11.123456+00:00"
    static bool FromBody(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        string v = raw.Trim().Trim('"');
        System.DateTimeOffset t;
        if (!System.DateTimeOffset.TryParse(v, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out t))
            return false;
        serverUnix = t.ToUnixTimeMilliseconds() / 1000.0;
        atRealtime = Time.realtimeSinceStartupAsDouble;
        Synced = true;
        return true;
    }

    // طلب خفيف للسيرفر عشان ناخذ الوقت (يناديه SaveManager)
    public static System.Collections.IEnumerator Sync()
    {
        AccountManager acc = AccountManager.Instance;
        if (acc == null) yield break;

        // ١. دالة server_now (تشتغل على الويب والجوال والكمبيوتر)
        UnityWebRequest req = acc.NewRequest("POST", "/rest/v1/rpc/server_now", "{}");
        req.timeout = 6;
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success && FromBody(req.downloadHandler.text);
        long code = req.responseCode;
        req.Dispose();
        if (ok) yield break;

        // ٢. الدالة مو موجودة أو ما ردت: رأس Date (ما يشتغل على المتصفح)
        if (code > 0) Debug.LogWarning("دالة server_now ما ردت (" + code + "). شغّلي ملف Supabase/server_time.sql");
        req = acc.NewRequest("GET", "/auth/v1/health", null);
        req.timeout = 6;
        yield return req.SendWebRequest();
        if (!FromResponse(req)) Debug.LogWarning("ما قدرنا ناخذ وقت السيرفر (" + req.responseCode + ")");
        req.Dispose();
    }
}
