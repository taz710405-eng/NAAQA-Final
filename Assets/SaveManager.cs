using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// ===== حفظ تقدم اللاعب وتحميله =====
// يشتغل لحاله: ما يحتاج تحطينه في المشهد ولا تربطين شي في Inspector.
// - يحفظ على الجهاز دايمًا (PlayerPrefs)
// - ولو اللاعب مسجل دخول، يرفع نفس الحفظ لـ Supabase (عمود save_data في جدول players)
// - لما اللاعب يسجل دخول: لو حسابه فيه حفظ ياخذه، ولو فاضي يرفع اللي لعبه
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("التوقيت")]
    public float checkEvery = 2f;        // كل كم ثانية نشوف إذا تغير شي
    public float cloudEvery = 15f;       // أقل وقت بين رفعتين للسحابة

    // LockUntilTutorial يستخدمها عشان ما يطلع "انفتح المتجر!" وقت التحميل
    public static bool JustRestored
    {
        get { return restoreTime > 0f && Time.unscaledTime - restoreTime < 1.5f; }
    }
    static float restoreTime = -1f;

    bool ready = false;            // ما نحفظ قبل ما نحمّل، عشان ما نمسح الحفظ القديم
    public bool Ready { get { return ready; } }
    string lastSnapshot = "";
    bool cloudDirty = false;
    bool cloudBusy = false;
    float lastCloudTime = -999f;

    // ===== يشغّل نفسه أول ما تبدأ اللعبة =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SaveManager");
        go.AddComponent<SaveManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        AccountManager.OnLoggedIn += HandleLoggedIn;
        AccountManager.OnLoggedOut += HandleLoggedOut;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        AccountManager.OnLoggedIn -= HandleLoggedIn;
        AccountManager.OnLoggedOut -= HandleLoggedOut;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Start()
    {
        StartCoroutine(LoadForScene());
    }

    void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        // المشهد الأول يتحمل من Start، هذا للمشاهد اللي بعده
        if (Time.frameCount <= 1) return;
        ready = false;
        StartCoroutine(LoadForScene());
    }

    // ===== التحميل أول ما يفتح المشهد =====
    IEnumerator LoadForScene()
    {
        // نخلي كل السكربتات تخلص Start حقها أول
        yield return null;
        yield return null;

        if (!HasGame())
        {
            ready = false;   // مشهد ما فيه اللعبة، ما نحفظ فيه
            yield break;
        }

        // ناخذ وقت السيرفر قبل ما نحسب وقت الغياب. ننتظر ٢٫٥ ثانية بالكثير عشان التحميل ما يتأخر
        // (أنظمة ثانية مثل WaterSystem تفترض الحفظ يتحمل خلال ٣ ثواني). لو تأخر: ما فيه وقت غياب هالمرة.
        if (!ServerTime.Synced && !timeSyncBusy)
        {
            StartCoroutine(SyncTime());
            float tw = 0f;
            while (timeSyncBusy && tw < 2.5f) { tw += Time.unscaledDeltaTime; yield return null; }
        }

        SaveData local = LoadLocal();
        if (local != null) Apply(local);
        else MailBox.CheckDailyGift();
        // حفظ الجهاز كان متزامن مع السحابة: نعتبر الحالة بعد إكمال الإنتاج متزامنة برضو
        if (local != null && local.syncver == SyncVer && local.rev > 0 && Fingerprint(local) == (local.syncedfp ?? ""))
            StartCoroutine(Rebase(syncedFp));

        ready = true;
        lastSnapshot = SnapshotJson(Capture());

        // لو مسجل دخول نقارن مع السحابة
        if (IsLoggedIn())
            yield return SyncWithCloud(false);
    }

    float timeSyncTimer = 30f;
    bool timeSyncBusy;
    IEnumerator SyncTime()
    {
        timeSyncBusy = true;
        yield return ServerTime.Sync();
        timeSyncBusy = false;
    }

    void Update()
    {
        // لو ما قدرنا ناخذ وقت السيرفر، نحاول كل ٣٠ ثانية
        if (!ServerTime.Synced && !timeSyncBusy)
        {
            timeSyncTimer -= Time.unscaledDeltaTime;
            if (timeSyncTimer <= 0f) { timeSyncTimer = 30f; StartCoroutine(SyncTime()); }
        }
        if (!ready) return;

        checkTimer += Time.unscaledDeltaTime;
        if (checkTimer < checkEvery) return;
        checkTimer = 0f;

        if (!HasGame()) return;

        SaveData now = Capture();
        string snap = SnapshotJson(now);
        timerSaveClock += checkEvery;
        if (snap != lastSnapshot)
        {
            lastSnapshot = snap;
            Stamp(now);
            SaveLocal(now);
            cloudDirty = true;
            timerSaveClock = 0f;
        }
        else if (timerSaveClock >= 20f && (now.orderWait > 0f || (now.customer != null && now.customer.has) || now.camels.Exists(c => c.producing) || now.placed.Exists(p => !string.IsNullOrEmpty(p.extra) && !p.extra.StartsWith("pen:"))))
        {
            // على الويب ما نضمن حفظ وقت الخروج، فنحفظ عدّاد الإبل كل شوي على الجهاز
            Stamp(now);
            SaveLocal(now);
            timerSaveClock = 0f;
        }

        if (cloudDirty && !cloudBusy && !syncing && !conflictOpen && IsLoggedIn() && Time.unscaledTime - lastCloudTime >= cloudEvery)
            StartCoroutine(Upload());
    }
    float checkTimer = 0f;
    float timerSaveClock = 0f;

    // الجوال: لما يطلع من اللعبة نحفظ على طول
    void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
        else if (!timeSyncBusy) StartCoroutine(SyncTime());   // رجع للعبة: نحدّث وقت السيرفر
    }

    void OnApplicationQuit()
    {
        SaveNow();
    }

    // تقدرين تناديها من أي مكان لو تبين حفظ فوري
    public void SaveNow()
    {
        if (!ready || !HasGame()) return;

        SaveData now = Capture();
        lastSnapshot = SnapshotJson(now);
        Stamp(now);
        SaveLocal(now);

        if (IsLoggedIn() && !cloudBusy)
        {
            cloudDirty = true;
            StartCoroutine(Upload());
        }
    }

    // ===== الدخول والخروج =====
    void HandleLoggedIn()
    {
        if (!ready) return;   // لو لسه ما تحمل، LoadForScene بيسوي المزامنة
        StartCoroutine(SyncWithCloud(true));
    }

    void HandleLoggedOut()
    {
        // تقدّم الحساب محفوظ عنده، فما نحفظه كضيف
        cloudDirty = false;
    }

    // يرفع آخر تقدم للسحابة وينتظر لين يخلص (قبل تسجيل الخروج)
    public IEnumerator FlushToCloud()
    {
        if (!ready || !HasGame() || !IsLoggedIn()) yield break;

        SaveData now = Capture();
        Stamp(now);
        SaveLocal(now);

        float wait = 0f;
        while (cloudBusy && wait < 8f) { wait += Time.unscaledDeltaTime; yield return null; }
        cloudDirty = true;
        yield return Upload(false);   // لو فيه تعارض: حفظ الجهاز يبقى وننحل الدخول الجاي
    }

    // بعد تسجيل الخروج: نبدأ لعبة جديدة نظيفة (بدون تقدم الحساب اللي طلع)
    public void PrepareFreshStart()
    {
        ready = false;
        baseRev = 0;
        syncedFp = "";
        cloudDirty = false;
        lastSnapshot = "";

        PlayerPrefs.DeleteKey("naaqa_save_guest");
        PlayerPrefs.Save();

        ShopItemView.RestoreBoughtIds(null);
        MailBox.Restore(null, 0);
        Quests.Restore(null);
        CamelProfiles.Restore(null);
        WaterSystem.WellGift = 0;
        WaterSystem.IntroDone = false;
        WaterSystem.StallGift = false;
        TutorialEnd.Stage = 0;
        WaterSystem.FarmIntro = false;
        OrderManager.SecondCamel = false;
        DailyReward.Streak = 0;
        DailyReward.LastDay = 0;
        DailyReward.CycleStart = 0;
        DailyReward.ClaimedMask = 0;
        EmergencyAid.LastUnix = 0;
        EmergencyAid.PendingWater = 0;
        WaterSystem.RestoreRefill(0f, 0f);
        StallCustomers.Restore(null, 0f);
        Milker.Restore(null, false);
        if (GameManager.Instance != null) GameManager.Instance.ResetResources();
    }

    // حذف الحساب: يمسح حفظ الجهاز (للحساب والضيف) وما يرفع شي بعدها
    public void WipeEverything()
    {
        ready = false;
        baseRev = 0;
        syncedFp = "";
        cloudDirty = false;
        lastSnapshot = "";
        AccountManager acc = AccountManager.Instance;
        if (acc != null && !string.IsNullOrEmpty(acc.UserId)) PlayerPrefs.DeleteKey("naaqa_save_" + acc.UserId);
        PlayerPrefs.DeleteKey("naaqa_save_guest");
        PlayerPrefs.Save();
    }

    // ===== المزامنة مع السحابة =====
    // كل حفظ معه:
    //  rev      = رقم نسخة السحابة اللي هالحفظ مبني عليها. السحابة هي اللي تعطي الرقم: كل رفع ناجح = الرقم + ١.
    //  syncver  = ٢ للحفظ اللي على هالنظام (الحفظ القديم ما فيه).
    //  syncedfp = بصمة آخر محتوى انرفع للسحابة أو انسحب منها، عشان نعرف هل فيه تقدم ما ارتفع.
    // الرفع مشروط على السيرفر نفسه: «حدّث صف اللاعب بس إذا السحابة للحين على نسختي».
    // هذا تحديث واحد في Postgres (UPDATE ... WHERE rev = X)، فلو جهازين رفعوا بنفس اللحظة واحد بس ينجح،
    // والثاني يرجع له صفر صفوف ونعرف إن فيه تعارض.
    public const int SyncVer = 2;
    static long baseRev;            // نسخة السحابة اللي اللعبة الحين مبنية عليها (٠ = سحابة قديمة أو فاضية)
    static string syncedFp = "";    // بصمة آخر محتوى متزامن مع السحابة
    bool syncing;                   // نقارن مع السحابة الحين (ما نرفع وقتها)
    bool conflictOpen;              // نافذة الاختيار مفتوحة (ما نرفع لين يختار)

    enum CloudResult { Ok, Rejected, NetError, Denied }

    static bool CloudHas(SaveData d) { return d != null && d.savedAt > 0; }
    static long CloudRev(SaveData d) { return d != null && d.syncver == SyncVer ? d.rev : 0; }

    // بصمة المحتوى (بدون الأوقات والعدادات وأرقام المزامنة). نمرره على JSON أول عشان تطلع نفسها للحفظ المحمّل والحالي
    string Fingerprint(SaveData d)
    {
        if (d == null) return "";
        SaveData copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
        string snap = SnapshotJson(copy);
        ulong h = 14695981039346656037UL;
        foreach (char c in snap) { h ^= c; h *= 1099511628211UL; }
        return h.ToString("x16");
    }

    // ===== سحب الحفظ من السحابة =====
    SaveData fetched;
    bool fetchedRow;               // صف اللاعب موجود (حتى لو حفظه فاضي)
    CloudResult fetchResult;

    IEnumerator FetchCloud()
    {
        fetched = null; fetchedRow = false; fetchResult = CloudResult.NetError;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) yield break;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            UnityWebRequest req = acc.NewRequest("GET", "/rest/v1/players?id=eq." + acc.UserId + "&select=save_data", null);
            yield return req.SendWebRequest();
            ServerTime.FromResponse(req);
            if (req.responseCode == 401 && attempt == 0)
            {
                yield return acc.EnsureFreshToken(true);
                if (!acc.LastRefreshOk) yield break;
                continue;
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                string raw = req.downloadHandler.text;
                fetchedRow = !string.IsNullOrEmpty(raw) && raw.Trim() != "[]";
                fetched = ParseCloud(raw);
                fetchResult = CloudResult.Ok;
            }
            else
            {
                fetchResult = (req.responseCode == 401 || req.responseCode == 403) ? CloudResult.Denied : CloudResult.NetError;
                Debug.LogWarning("ما قدرت أجيب الحفظ من السحابة: " + req.responseCode + " " + req.downloadHandler.text);
            }
            yield break;
        }
    }

    // ===== الرفع المشروط (مقارنة وتبديل على السيرفر) =====
    CloudResult casResult;

    [System.Serializable] class PatchBody { public string display_name; public SaveData save_data; public string updated_at; }

    IEnumerator PutCas(SaveData data, long fromRev)
    {
        casResult = CloudResult.NetError;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) yield break;
        data.syncver = SyncVer;
        data.rev = fromRev + 1;
        data.syncedfp = "";
        // الشرط: السحابة للحين على نسختنا (أو حفظ قديم/فاضي لو ما عندنا نسخة)
        string cond = fromRev > 0
            ? "&save_data-%3E%3Erev=eq." + fromRev + "&save_data-%3E%3Esyncver=eq." + SyncVer
            : "&save_data-%3E%3Esyncver=is.null";
        for (int attempt = 0; attempt < 2; attempt++)
        {
            PatchBody body = new PatchBody();
            body.display_name = string.IsNullOrEmpty(acc.DisplayName) ? "لاعب" : acc.DisplayName;
            body.save_data = data;
            body.updated_at = System.DateTime.UtcNow.ToString("o");
            UnityWebRequest req = acc.NewRequest("PATCH", "/rest/v1/players?id=eq." + acc.UserId + cond + "&select=id", JsonUtility.ToJson(body));
            req.SetRequestHeader("Prefer", "return=representation");
            yield return req.SendWebRequest();
            ServerTime.FromResponse(req);
            if (req.responseCode == 401 && attempt == 0)
            {
                yield return acc.EnsureFreshToken(true);
                if (!acc.LastRefreshOk) yield break;
                continue;
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                string raw = req.downloadHandler.text;
                // صف واحد رجع = انحدّث. صفر صفوف = الشرط ما انطبق (جهاز ثاني رفع قبلنا، أو الصف مو موجود، أو الصلاحية)
                casResult = (string.IsNullOrEmpty(raw) || raw.Trim() == "[]") ? CloudResult.Rejected : CloudResult.Ok;
            }
            else
            {
                casResult = (req.responseCode == 401 || req.responseCode == 403) ? CloudResult.Denied : CloudResult.NetError;
                Debug.LogWarning("ما انحفظ في السحابة: " + req.responseCode + " " + req.downloadHandler.text);
            }
            yield break;
        }
    }

    // صف اللاعب مو موجود: نضيفه. الإضافة نفسها ذرّية (المفتاح id)، فلو جهاز ثاني سبقنا يرجع ٤٠٩
    IEnumerator InsertRow(SaveData data)
    {
        casResult = CloudResult.NetError;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) yield break;
        data.syncver = SyncVer;
        data.rev = 1;
        data.syncedfp = "";
        UpsertBody body = new UpsertBody();
        body.id = acc.UserId;
        body.display_name = string.IsNullOrEmpty(acc.DisplayName) ? "لاعب" : acc.DisplayName;
        body.save_data = data;
        body.updated_at = System.DateTime.UtcNow.ToString("o");
        UnityWebRequest req = acc.NewRequest("POST", "/rest/v1/players", JsonUtility.ToJson(body));
        req.SetRequestHeader("Prefer", "return=minimal");
        yield return req.SendWebRequest();
        ServerTime.FromResponse(req);
        if (req.result == UnityWebRequest.Result.Success) casResult = CloudResult.Ok;
        else if (req.responseCode == 409) casResult = CloudResult.Rejected;
        else
        {
            casResult = (req.responseCode == 401 || req.responseCode == 403) ? CloudResult.Denied : CloudResult.NetError;
            Debug.LogWarning("ما قدرت أسوي صف الحفظ: " + req.responseCode + " " + req.downloadHandler.text);
        }
    }

    // ===== المقارنة مع السحابة (وقت الدخول وأول ما تفتح اللعبة) =====
    // justLoggedIn = true: حساب دخل الحين
    IEnumerator SyncWithCloud(bool justLoggedIn)
    {
        AccountManager acc = AccountManager.Instance;
        if (acc == null) yield break;
        float w = 0f;
        while (cloudBusy && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }

        syncing = true;
        yield return acc.EnsureFreshToken();
        if (!acc.LastRefreshOk) { syncing = false; yield break; }
        yield return FetchCloud();
        syncing = false;
        if (fetchResult != CloudResult.Ok) yield break;   // ما فيه نت أو صلاحية: نكمل على حفظ الجهاز، ونحاول بعدين

        SaveData cloud = CloudHas(fetched) ? fetched : null;
        SaveData local = LoadLocal();
        if (local != null && local.savedAt <= 0) local = null;

        // توه داخل وعنده حفظ لهالحساب على الجهاز: نرجّعه أول (بدل تقدم الضيف اللي على الشاشة)
        if (justLoggedIn && local != null)
        {
            Apply(local);
            lastSnapshot = SnapshotJson(Capture());
        }

        // ١. السحابة فاضية: نرفع اللي عندنا
        if (cloud == null)
        {
            cloudDirty = true;
            yield return Upload();
            yield break;
        }

        long cRev = CloudRev(cloud);

        // ٢. ما فيه حفظ لهالحساب على الجهاز: ناخذ السحابة
        if (local == null)
        {
            AdoptCloud(cloud);
            yield break;
        }

        // ٣. السحابة حفظ قديم (قبل هالنظام): نفس القاعدة القديمة بالوقت، والرفع بعدها مشروط
        if (cRev == 0)
        {
            if (local.savedAt > cloud.savedAt) { baseRev = 0; cloudDirty = true; yield return Upload(); }
            else AdoptCloud(cloud);
            yield break;
        }

        // ٤. حفظ الجهاز قديم أو عمره ما تزامن على هالنظام: ما نعرف من أي نسخة طلع
        bool localLegacy = local.syncver != SyncVer || local.rev == 0;
        if (localLegacy)
        {
            if (Fingerprint(local) == Fingerprint(cloud)) AdoptCloud(cloud);
            else if (local.savedAt > cloud.savedAt) ShowConflict(Capture(), cloud);   // ممكن يكون أحدث: نخلي اللاعب يختار (الحالة الحالية فيها إنتاج الغياب)
            else AdoptCloud(cloud);
            yield break;
        }

        // ٥. الاثنين على هالنظام
        long baseOf = local.rev;
        bool localNew = Fingerprint(local) != (local.syncedfp ?? "");   // فيه تقدم انحفظ على الجهاز وما ارتفع
        if (cRev == baseOf)
        {
            // السحابة ما تغيرت من آخر مزامنة
            if (localNew) { cloudDirty = true; yield return Upload(); }
        }
        else if (cRev > baseOf)
        {
            // السحابة تقدمت (جهاز ثاني رفع)
            if (!localNew) AdoptCloud(cloud);
            else if (Fingerprint(local) == Fingerprint(cloud)) { baseRev = cRev; syncedFp = Fingerprint(cloud); }
            else ShowConflict(Capture(), cloud);   // الحالة الحالية = حفظ الجهاز + إنتاج الغياب
        }
        else
        {
            // السحابة ورا نسختنا (انمسحت أو رجعت): نبني على اللي فيها ونرفع
            baseRev = cRev;
            cloudDirty = true;
            yield return Upload();
        }
    }

    // ناخذ حفظ السحابة (ما عندنا شي جديد يضيع)
    void AdoptCloud(SaveData cloud)
    {
        cloud.syncedfp = Fingerprint(cloud);
        Apply(cloud);
        baseRev = CloudRev(cloud);
        syncedFp = cloud.syncedfp;
        SaveLocal(cloud);
        lastSnapshot = SnapshotJson(Capture());
        cloudDirty = false;
        Notify("تم تحميل تقدمك!");
        Debug.Log("تحمل الحفظ من السحابة (نسخة " + baseRev + ")");
        StartCoroutine(Rebase(syncedFp));
    }

    // بعد التحميل اللعبة تكمل الإنتاج اللي صار وانت طالع (الحليب، المزارع، العامل)، فيتغير المحتوى شوي
    // بدون ما يلعب اللاعب. نعتبر هالحالة هي المتزامنة، عشان ما تطلع نافذة تعارض على شي ما سواه
    IEnumerator Rebase(string fpAtLoad)
    {
        yield return new WaitForSecondsRealtime(2f);
        if (syncedFp == fpAtLoad && !conflictOpen && ready) syncedFp = Fingerprint(Capture());
    }

    // ===== الرفع =====
    // askUser = false وقت تسجيل الخروج: لو فيه تعارض نخلي حفظ الجهاز وننحل الدخول الجاي
    IEnumerator Upload(bool askUser = true)
    {
        AccountManager acc = AccountManager.Instance;
        if (acc == null || !acc.IsLoggedIn || !HasGame() || syncing || conflictOpen || cloudBusy) yield break;

        cloudBusy = true;
        lastCloudTime = Time.unscaledTime;

        yield return acc.EnsureFreshToken();
        if (!acc.LastRefreshOk) { cloudBusy = false; yield break; }

        SaveData data = Capture();
        string fp = Fingerprint(data);
        if (baseRev > 0 && fp == syncedFp) { cloudDirty = false; cloudBusy = false; yield break; }   // ما فيه شي جديد
        Stamp(data);
        long fromRev = baseRev;
        yield return PutCas(data, fromRev);

        if (casResult == CloudResult.Ok)
        {
            baseRev = fromRev + 1;
            syncedFp = fp;
            cloudDirty = false;
            SaveData now = Capture();   // نحفظ على الجهاز إن هالمحتوى صار متزامن
            Stamp(now);
            SaveLocal(now);
            Debug.Log("انحفظ التقدم في السحابة (نسخة " + baseRev + ")");
        }
        else if (casResult == CloudResult.Rejected)
        {
            yield return ResolveRejected(data, fp, askUser);
        }
        else if (casResult == CloudResult.Denied)
        {
            lastCloudTime = Time.unscaledTime + 300f;   // ما نعيد كل شوي
            Debug.LogWarning("Supabase رفض الحفظ (صلاحية). تأكدي إن ملف Supabase/save_progress.sql انشغّل");
        }
        // NetError: cloudDirty يبقى، ونحاول بعد cloudEvery
        cloudBusy = false;
    }

    // الرفع انرفض: نسحب السحابة ونشوف وش صار
    IEnumerator ResolveRejected(SaveData data, string fp, bool askUser)
    {
        yield return FetchCloud();
        if (fetchResult != CloudResult.Ok) yield break;

        // صف اللاعب مو موجود: نضيفه
        if (!fetchedRow)
        {
            yield return InsertRow(data);
            if (casResult == CloudResult.Ok)
            {
                baseRev = 1; syncedFp = fp; cloudDirty = false;
                SaveData now = Capture(); Stamp(now); SaveLocal(now);
            }
            yield break;   // ٤٠٩ = جهاز ثاني سبقنا: الرفعة الجاية بتنرفض وتجي هنا مع الصف
        }

        SaveData cloud = CloudHas(fetched) ? fetched : null;
        long cRev = CloudRev(cloud);
        if (cRev == baseRev)
        {
            // السحابة على نفس نسختنا والتحديث ما انطبق: يعني Supabase منعه (صلاحية التحديث ناقصة)
            lastCloudTime = Time.unscaledTime + 300f;
            Debug.LogWarning("الرفع المشروط ما حدّث شي والسحابة ما تغيرت: غالبًا صلاحية naaqa_update_own ناقصة في Supabase");
            yield break;
        }
        if (cloud == null) { baseRev = 0; cloudDirty = true; yield break; }   // الحفظ في السحابة انمسح: الرفعة الجاية تبني من الصفر

        // السحابة تقدمت: فيه تقدم عندنا ما ارتفع؟
        SaveData current = Capture();
        bool localNew = Fingerprint(current) != syncedFp;
        if (!localNew) { AdoptCloud(cloud); yield break; }
        if (Fingerprint(current) == Fingerprint(cloud)) { baseRev = cRev; syncedFp = Fingerprint(cloud); cloudDirty = false; yield break; }
        if (!askUser) yield break;   // وقت الخروج: حفظ الجهاز يبقى، وننحل الدخول الجاي
        ShowConflict(current, cloud);
    }

    // ===== التعارض الحقيقي: اللاعب يختار =====
    void ShowConflict(SaveData local, SaveData cloud)
    {
        if (conflictOpen) return;
        conflictOpen = true;
        cloudDirty = false;
        SaveConflictScreen.Show(local, cloud, chooseLocal => Resolve(chooseLocal, local, cloud), Later);
    }

    // سكّر النافذة بدون ما يختار: ما نرفع شي، ونرجع نسأله بعد شوي (الرفعة الجاية بتنرفض وترجع النافذة)
    void Later()
    {
        conflictOpen = false;
        cloudDirty = true;
        lastCloudTime = Time.unscaledTime + 60f;
    }

    void Resolve(bool chooseLocal, SaveData local, SaveData cloud)
    {
        long cRev = CloudRev(cloud);
        SaveData keep = chooseLocal ? local : cloud;
        SaveData drop = chooseLocal ? cloud : local;

        // ١. النسخة اللي ما انختارت تنحفظ احتياط على الجهاز قبل أي تبديل (ما تنمسح تلقائيًا)
        Backup(drop, chooseLocal ? "cloud" : "device");

        // ٢. ما نخلي المكافآت تتكرر: اللي انستلم في أي نسخة يبقى مستلم
        MergeClaims(keep, drop);

        // ٣. نطبّق المختار ونبني على نسخة السحابة الحالية، ونرفع (مشروط برضو)
        keep.savedAt = chooseLocal ? 0 : keep.savedAt;   // حفظ الجهاز: ما نحسب له وقت غياب ثاني
        Apply(keep);
        baseRev = cRev;
        syncedFp = "";   // نجبر الرفع
        SaveData now = Capture();
        Stamp(now);
        SaveLocal(now);
        lastSnapshot = SnapshotJson(now);
        conflictOpen = false;
        cloudDirty = true;
        lastCloudTime = -999f;
        Notify(chooseLocal ? "اعتمدنا تقدم هالجهاز" : "اعتمدنا التقدم المحفوظ في السحابة");
    }

    // اللي انستلم في النسخة الثانية يبقى مستلم (المكافأة اليومية، الفزعة، هدايا البريد)
    static void MergeClaims(SaveData keep, SaveData other)
    {
        if (keep == null || other == null) return;
        if (other.dailyDay > keep.dailyDay)
        {
            keep.dailyDay = other.dailyDay; keep.dailyStreak = other.dailyStreak;
            keep.dailyCycle = other.dailyCycle; keep.dailyMask = other.dailyMask;
        }
        else if (other.dailyDay == keep.dailyDay && other.dailyCycle == keep.dailyCycle)
            keep.dailyMask |= other.dailyMask;
        if (other.aidAt > keep.aidAt) keep.aidAt = other.aidAt;
        if (other.lastGiftDay > keep.lastGiftDay) keep.lastGiftDay = other.lastGiftDay;
        if (keep.mail != null && other.mail != null)
            foreach (MailBox.Letter l in keep.mail)
            {
                MailBox.Letter o = other.mail.Find(x => x.time == l.time && x.text == l.text);
                if (o != null && o.claimed) { l.claimed = true; l.read = true; }
            }
    }

    // نسخة احتياط على الجهاز (ما تنمسح تلقائيًا)
    void Backup(SaveData d, string source)
    {
        if (d == null) return;
        string key = LocalKey() + "_backups";
        int n = PlayerPrefs.GetInt(key, 0) + 1;
        PlayerPrefs.SetString(key + "_" + n, JsonUtility.ToJson(d));
        PlayerPrefs.SetString(key + "_" + n + "_info", source + " " + ServerTime.UnixLong);
        PlayerPrefs.SetInt(key, n);
        PlayerPrefs.Save();
        Debug.Log("انحفظت نسخة احتياط (" + source + ") رقم " + n);
    }

    SaveData ParseCloud(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.Trim() == "[]") return null;
        try
        {
            CloudRows rows = JsonUtility.FromJson<CloudRows>("{\"rows\":" + raw + "}");
            if (rows == null || rows.rows == null || rows.rows.Length == 0) return null;
            return rows.rows[0].save_data;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("حفظ السحابة خربان: " + e.Message);
            return null;
        }
    }

    // ===== جمع حالة اللعبة =====
    SaveData Capture()
    {
        SaveData d = new SaveData();
        d.syncver = SyncVer;
        d.rev = baseRev;
        d.syncedfp = syncedFp;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            d.milk = gm.milkCount;
            d.money = gm.money;
            d.feed = gm.feedCount;
        }

        if (TaskManager.Instance != null)
            d.tasks = TaskManager.Instance.GetDone();

        OrderManager om = FindAnyObjectByType<OrderManager>();
        if (om != null)
        {
            d.orderDelivered = om.OrderDelivered;
            d.penUpgraded = om.IsUpgraded;
            d.saveVer = 2;
            d.secondCamel = OrderManager.SecondCamel;
            d.orderMilk = om.requiredMilk;
            d.orderReward = om.rewardCoins;
            d.ordersCompleted = om.OrdersCompleted;
            d.orderWait = Mathf.Ceil(om.WaitLeft);
        }

        if (LevelManager.Instance != null)
        {
            d.level = LevelManager.Instance.Level;
            d.xp = LevelManager.Instance.Xp;
        }

        d.breeds = CamelBreeds.ExtraBreeds();

        foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Include))
        {
            CamelEntry e = new CamelEntry();
            e.name = c.gameObject.name;
            e.ready = c.IsMilkReady;
            e.producing = c.IsProducing;
            // نقرّب لأقرب ثانية عشان ما ينحفظ كل فريم
            e.remaining = Mathf.Ceil(c.Remaining);
            e.dated = c.DatedThisCycle;
            d.camels.Add(e);
        }
        d.camels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        d.bought = ShopItemView.GetBoughtIds();
        d.bought.Sort(string.CompareOrdinal);

        if (InventoryManager.Instance != null)
        {
            foreach (KeyValuePair<string, int> kv in InventoryManager.Instance.GetAllCounts())
                d.bag.Add(new ItemCount { id = kv.Key, count = kv.Value });
            d.bag.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        }

        if (PlacementManager.Instance != null)
        {
            foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            {
                ItemProducer prod = p.go != null ? p.go.GetComponent<ItemProducer>() : null;
                ExtraPen pen = p.go != null ? p.go.GetComponent<ExtraPen>() : null;
                FarmPlot farm = p.go != null ? p.go.GetComponent<FarmPlot>() : null;
                string ex = prod != null ? prod.SaveState() : pen != null ? pen.SaveState() : farm != null ? farm.SaveState() : "";
                d.placed.Add(new PlacedEntry { id = p.id, position = p.position, rotation = p.rotation, extra = ex });
            }
        }

        if (FarmStore.Instance != null)
        {
            d.dates = FarmStore.Instance.Dates;
            d.citron = FarmStore.Instance.Citron;
            d.water = FarmStore.Instance.Water;
            d.waterCap = FarmStore.Instance.WaterCap;
            d.souqLevel = FarmStore.Instance.SouqLevel;
            d.raceWins = FarmStore.Instance.RaceWins;
            d.mazayenWins = FarmStore.Instance.MazayenWins;
            d.penLevel = FarmStore.Instance.PenLevelSaved;
            d.trough = FarmStore.Instance.HasTrough;
        }

        if (PenVisual.Instance != null)
        {
            d.penMoved = true;
            d.penPos = PenVisual.Instance.transform.position;
        }

        d.camelNames = CamelProfiles.Save();
        d.wellGift = WaterSystem.WellGift;
        d.waterIntro = WaterSystem.IntroDone;
        d.stallGift = WaterSystem.StallGift;
        d.endStage = TutorialEnd.Stage;
        d.dailyStreak = DailyReward.Streak;
        d.dailyDay = DailyReward.LastDay;
        d.dailyCycle = DailyReward.CycleStart;
        d.dailyMask = DailyReward.ClaimedMask;
        d.aidAt = EmergencyAid.LastUnix;
        d.aidWater = EmergencyAid.PendingWater;
        d.wellRefill = WaterSystem.RefillProgress;
        d.customer = StallCustomers.Capture();
        d.farmIntro = WaterSystem.FarmIntro;
        d.workers = Milker.Save();
        d.mail = MailBox.Save();
        d.quests = Quests.Save();
        d.lastGiftDay = MailBox.LastGiftDay;

        return d;
    }

    // ===== ترجيع حالة اللعبة =====
    const string RemovedCorner = "fence_corner";   // زاوية السور (انشالت من اللعبة)
    const string TrophyId = "trophy";              // كأس السباق: يطلع بس لو فاز بسباق
    const int CornerPrice = 14;
    int removedCorners;

    void Apply(SaveData d)
    {
        if (d == null) return;
        restoreTime = Time.unscaledTime;
        baseRev = d.syncver == SyncVer ? d.rev : 0;   // نسخة السحابة اللي هالحفظ مبني عليها
        syncedFp = d.syncedfp ?? "";

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.milkCount = Mathf.Max(0, d.milk);
            gm.money = Mathf.Max(0, d.money);
            gm.feedCount = Mathf.Max(0, d.feed);
        }

        OrderManager om = FindAnyObjectByType<OrderManager>();
        // حفظ قديم (قبل التعديل) وكان الحوش متطور: كانت جته الناقة الثانية، تبقى له
        OrderManager.SecondCamel = d.saveVer >= 2 ? d.secondCamel : d.penUpgraded;
        if (om != null) om.RestoreState(d.orderDelivered, d.penUpgraded, d.orderMilk, d.orderReward, d.ordersCompleted, d.orderWait);

        if (LevelManager.Instance != null && d.level > 0)
            LevelManager.Instance.Restore(d.level, d.xp);

        ShopItemView.RestoreBoughtIds(d.bought);

        // الإبل اللي انشرت من سوق الإبل
        CamelBreeds.ClearExtra();
        if (d.breeds != null)
            foreach (string b in d.breeds) CamelBreeds.Spawn(b);

        // الإبل: نكمل الإنتاج، ونحسب الوقت اللي كان فيه اللاعب برا اللعبة
        float away = Away(d);
        if (d.camels != null)
        {
            foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Include))
            {
                CamelEntry e = d.camels.Find(x => x.name == c.gameObject.name);
                if (e == null) continue;
                c.RestoreState(e.ready, e.producing, e.remaining - away, e.dated);
            }
        }

        if (TaskManager.Instance != null)
            TaskManager.Instance.RestoreDone(d.tasks);

        if (InventoryManager.Instance != null)
        {
            Dictionary<string, int> bag = new Dictionary<string, int>();
            if (d.bag != null)
                foreach (ItemCount c in d.bag)
                    if (!string.IsNullOrEmpty(c.id)) bag[c.id] = c.count;
            // زاوية السور انشالت من اللعبة: اللي في الشنطة ينرجع سعره
            int cornerBag;
            if (bag.TryGetValue(RemovedCorner, out cornerBag)) { removedCorners += cornerBag; bag.Remove(RemovedCorner); }
            // كأس السباق ما يكون عنده إلا لو فاز بسباق
            if (d.raceWins <= 0) bag.Remove(TrophyId);
            InventoryManager.Instance.SetAllCounts(bag);
        }

        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.ClearPlaced();
            if (d.placed != null)
                foreach (PlacedEntry p in d.placed)
                {
                    if (p.id == RemovedCorner) { removedCorners++; continue; }   // زاوية السور انشالت
                    if (p.id == TrophyId && d.raceWins <= 0) continue;            // الكأس بس للي فاز بسباق
                    GameObject go = PlacementManager.Instance.RestorePlaced(p.id, p.position, p.rotation);
                    ItemProducer prod = go != null ? go.GetComponent<ItemProducer>() : null;
                    if (prod != null) prod.LoadState(p.extra, away);
                    ExtraPen pen = go != null ? go.GetComponent<ExtraPen>() : null;
                    if (pen != null) pen.LoadState(p.extra);
                    FarmPlot farm = go != null ? go.GetComponent<FarmPlot>() : null;
                    if (farm != null) farm.LoadState(p.extra, away);
                }
        }

        // زاوية السور انشالت من اللعبة: نرجّع سعرها (مرة وحدة، لأنها ما تنحفظ بعدها)
        if (removedCorners > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(removedCorners * CornerPrice);
            Notify("زاوية السور انشالت من اللعبة، ورجعنا لك " + NaaqaUI.Ar(removedCorners * CornerPrice) + " قرش");
            removedCorners = 0;
        }

        // مكان الحوش الرئيسي (لو حركه اللاعب)
        if (d.penMoved && PenVisual.Instance != null && !PlacementManager.IsPlacing)
            PenVisual.Instance.transform.position = d.penPos;

        // بعد الأغراض (عشان سعة الماء ما تنحسب غلط)
        if (FarmStore.Instance != null)
        {
            FarmStore.Instance.Restore(d.dates, d.citron, d.water, d.waterCap, d.souqLevel, d.raceWins);
            FarmStore.Instance.RestoreMazayen(d.mazayenWins);
            FarmStore.Instance.RestoreExtra(d.penLevel, d.trough);
        }

        CamelProfiles.Restore(d.camelNames);
        WaterSystem.WellGift = d.wellGift;
        WaterSystem.IntroDone = d.waterIntro;
        WaterSystem.StallGift = d.stallGift;
        // حفظ قديم (قبل آخر الشرح الجديد) وعنده الكشك: ما نعيد عليه الشرح
        TutorialEnd.Stage = d.endStage > 0 ? d.endStage : (d.stallGift ? TutorialEnd.Done : 0);
        if (TutorialEnd.Stage > 0 && TutorialEnd.Stage < TutorialEnd.PlaceKiosk) TutorialEnd.Stage = TutorialEnd.PlaceKiosk;   // (خطوات المزرعة القديمة انتقلت لأول الشرح)
        WaterSystem.FarmIntro = d.farmIntro || d.waterIntro;
        DailyReward.Streak = d.dailyStreak;
        DailyReward.LastDay = d.dailyDay;
        DailyReward.CycleStart = d.dailyCycle;
        DailyReward.ClaimedMask = d.dailyMask;
        EmergencyAid.LastUnix = d.aidAt;
        EmergencyAid.PendingWater = d.aidWater;
        WaterSystem.RestoreRefill(d.wellRefill, away);   // البير يعبي وقت الغياب
        StallCustomers.Restore(d.customer, away);
        Milker.Restore(d.workers, d.milker);
        Milker.QueueOffline(d.camels, away);
        MailBox.Restore(d.mail, d.lastGiftDay);
        Quests.Restore(d.quests);
        MailBox.CheckDailyGift();
        FixStarterItems();

        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
    }

    // الخيمة تنشرى مرة وحدة لكل حساب: لو انشرت وما هي متركبة ولا في الشنطة نرجعها للشنطة،
    // ولو هي متركبة وفي الشنطة بعد، نشيلها من الشنطة
    void FixStarterItems()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // الحوض انشال من اللعبة: لو كان متركب أو في الشنطة (من النسخة القديمة) نشيله
        if (PlacementManager.Instance != null)
            foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
                if (p.id == "trough") PlacementManager.Instance.RemovePlaced(p.go);
        while (inv.GetCount("trough") > 0) inv.Remove("trough");
        if (FarmStore.Instance != null) FarmStore.Instance.SetTrough(false);   // الحوض انشال
        foreach (string id in ShopItemView.GetBoughtIds())
        {
            ShopExtraItems.ItemDef def = ShopExtraItems.Get(id);
            bool once = id == "tent" || (def != null && def.once && def.id != "trough");
            if (!once) continue;
            bool placed = false;
            if (PlacementManager.Instance != null)
                foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
                    if (p.id == id) { placed = true; break; }
            int inBag = inv.GetCount(id);
            if (!placed && inBag == 0 && inv.GetData(id) != null) inv.Add(id);
            else if (placed) while (inv.GetCount(id) > 0) inv.Remove(id);
            else while (inv.GetCount(id) > 1) inv.Remove(id);
        }
    }

    // ===== الحفظ على الجهاز =====
    string LocalKey()
    {
        AccountManager acc = AccountManager.Instance;
        string who = (acc != null && acc.IsLoggedIn && !string.IsNullOrEmpty(acc.UserId)) ? acc.UserId : "guest";
        return "naaqa_save_" + who;
    }

    void SaveLocal(SaveData d)
    {
        PlayerPrefs.SetString(LocalKey(), JsonUtility.ToJson(d));
        PlayerPrefs.Save();
    }

    SaveData LoadLocal()
    {
        string json = PlayerPrefs.GetString(LocalKey(), "");
        if (json == "") return null;
        try { return JsonUtility.FromJson<SaveData>(json); }
        catch { Debug.LogWarning("الحفظ على الجهاز خربان، نبدأ من جديد"); return null; }
    }

    // للتجربة: يمسح حفظ الجهاز (من قائمة الترس ⋮ على السكربت في Inspector)
    [ContextMenu("امسح الحفظ المحلي")]
    public void DeleteLocalSave()
    {
        PlayerPrefs.DeleteKey(LocalKey());
        PlayerPrefs.Save();
        Debug.Log("انمسح الحفظ المحلي: " + LocalKey());
    }

    // ===== أدوات =====
    bool HasGame()
    {
        return GameManager.Instance != null && TaskManager.Instance != null;
    }

    bool IsLoggedIn()
    {
        return AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn;
    }

    // نفس الحالة بدون الوقت، عشان نعرف إذا تغير شي فعلًا
    // (بدون الوقت وعدّاد الإبل، لأنها تتغير كل ثانية. تنحفظ مع أي تغيير ثاني ووقت الخروج)
    string SnapshotJson(SaveData d)
    {
        long t = d.savedAt;
        d.savedAt = 0;
        long rv = d.rev; d.rev = 0;
        int sv = d.syncver; d.syncver = 0;
        string sf = d.syncedfp; d.syncedfp = null;
        float wait = d.orderWait;
        d.orderWait = 0f;
        bool tok = d.timeOk; d.timeOk = false;
        float wr = d.wellRefill; d.wellRefill = 0f;
        float cw = d.customer != null ? d.customer.wait : 0f; if (d.customer != null) d.customer.wait = 0f;
        string[] ex = new string[d.placed.Count];
        for (int i = 0; i < ex.Length; i++) { ex[i] = d.placed[i].extra; if (ex[i] == null || !ex[i].StartsWith("pen:")) d.placed[i].extra = ""; }
        float[] rem = new float[d.camels.Count];
        for (int i = 0; i < rem.Length; i++) { rem[i] = d.camels[i].remaining; d.camels[i].remaining = 0f; }

        string json = JsonUtility.ToJson(d);

        d.savedAt = t;
        d.rev = rv; d.syncver = sv; d.syncedfp = sf;
        d.orderWait = wait;
        d.timeOk = tok; d.wellRefill = wr; if (d.customer != null) d.customer.wait = cw;
        for (int i = 0; i < ex.Length; i++) d.placed[i].extra = ex[i];
        for (int i = 0; i < rem.Length; i++) d.camels[i].remaining = rem[i];
        return json;
    }

    // الوقت من السيرفر (ServerTime)، مو من ساعة الجهاز
    static long NowUnix()
    {
        return ServerTime.UnixLong;
    }

    // نختم الحفظ بالوقت، ونعلّم هل الوقت من السيرفر (موثوق) ولا من الجهاز
    static void Stamp(SaveData d)
    {
        d.savedAt = NowUnix();
        d.timeOk = ServerTime.Synced;
        d.timeVer = 1;
    }

    // كم ثانية غاب اللاعب: بس لو الوقت الحين ووقت الحفظ من السيرفر.
    // لو ما فيه نت (أو الحفظ انختم بساعة الجهاز) ما نعطيه وقت غياب، عشان تغيير ساعة الجهاز ما يفيد
    static float Away(SaveData d)
    {
        if (d == null || d.savedAt <= 0 || !ServerTime.Synced) return 0f;
        bool trusted = d.timeOk || d.timeVer == 0;   // الحفظ القديم (قبل هالنظام) نقبله مرة
        if (!trusted) return 0f;
        return Mathf.Max(0f, (float)(ServerTime.Unix - d.savedAt));
    }

    void Notify(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
    }

    // ===== شكل الحفظ =====
    [System.Serializable]
    public class SaveData
    {
        public int version = 6;
        public long savedAt;
        public long rev;               // رقم نسخة السحابة اللي هالحفظ مبني عليها (السحابة تزيده مع كل رفع ناجح)
        public int syncver;            // ٢ = نظام الرفع المشروط (القديم ما فيه)
        public string syncedfp;        // بصمة آخر محتوى متزامن مع السحابة
        public bool timeOk;            // savedAt من وقت السيرفر
        public int timeVer;            // ١ = بعد نظام وقت السيرفر
        public float wellRefill;       // كم ثانية مرت من تعبئة البير الجاية
        public int aidWater;           // ماء المساعدة الطارئة اللي ينتظر بير
        public StallCustomers.Saved customer = new StallCustomers.Saved();   // الزبون الواقف عند الكشك
        public int milk;
        public int money;
        public int feed;
        public bool[] tasks = new bool[0];
        public bool orderDelivered;
        public bool penUpgraded;
        public int saveVer;            // ٢ = بعد ما صار تطوير الحوش ما يعطي ناقة
        public bool secondCamel;       // جته الناقة الثانية من الشرح (الحفظ القديم)
        public int orderMilk;
        public int orderReward;
        public int ordersCompleted;
        public float orderWait;
        public int dates, citron, water, waterCap, souqLevel = 1, raceWins, mazayenWins;
        public int level = 1;
        public int xp;
        public List<ItemCount> bag = new List<ItemCount>();
        public List<PlacedEntry> placed = new List<PlacedEntry>();
        public List<CamelEntry> camels = new List<CamelEntry>();
        public List<string> bought = new List<string>();
        public List<string> breeds = new List<string>();
        public int gold;               // (الحفظ القديم، الذهب انشال)
        public int penLevel = 1;
        public List<MailBox.Letter> mail = new List<MailBox.Letter>();
        public bool trough;
        public List<Quests.Entry> quests = new List<Quests.Entry>();
        public long lastGiftDay;
        public List<CamelProfiles.Entry> camelNames = new List<CamelProfiles.Entry>();
        public int wellGift;
        public bool waterIntro;
        public bool stallGift;
        public int endStage;
        public int dailyStreak;        // مكافأة الدخول اليومي: آخر يوم استلمه (١ لين ٧)
        public long dailyDay;          // اليوم اللي استلم فيه آخر مرة
        public long dailyCycle;        // أول يوم في دورة المكافأة الحالية
        public int dailyMask;          // الأيام اللي استلمها في الدورة
        public long aidAt;             // آخر مرة أخذ المساعدة الطارئة (يونكس)
        public bool farmIntro;         // خلص شرح مزرعة العلف           // وين وصل في آخر الشرح (TutorialEnd)
        public bool milker;            // (الحفظ القديم)
        public List<Milker.Worker> workers = new List<Milker.Worker>();
        public bool penMoved;          // الحوش الرئيسي انتقل من مكانه الأصلي
        public Vector3 penPos;
    }

    [System.Serializable] public class CamelEntry { public string name; public bool ready; public bool producing; public float remaining; public bool dated; }

    [System.Serializable] public class ItemCount { public string id; public int count; }
    [System.Serializable] public class PlacedEntry { public string id; public Vector3 position; public Quaternion rotation; public string extra; }

    [System.Serializable] class CloudRow { public SaveData save_data; }
    [System.Serializable] class CloudRows { public CloudRow[] rows; }
    [System.Serializable] class UpsertBody { public string id; public string display_name; public SaveData save_data; public string updated_at; }
}
