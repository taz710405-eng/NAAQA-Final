using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Camel : MonoBehaviour
{
    [Header("Optional")]
    public GameObject milkReadyIcon;

    [Header("Timer")]
    public GameObject timerBadge;
    public TMP_Text timerText;
    public bool arabicDigits = true;
    public Vector2 timerScreenOffset = new Vector2(0f, 120f);

    [Header("Settings")]
    public float productionTime = 10f;

    private bool isProducing = false;
    private bool milkReady = false;
    private float remaining = 0f;

    // للحفظ
    private bool started = false;
    private bool hasPending = false;
    private bool pendingReady, pendingProducing;
    private float pendingRemaining;

    GameObject TimerObject
    {
        get
        {
            if (timerBadge != null) return timerBadge;
            if (timerText != null) return timerText.gameObject;
            return null;
        }
    }

    // العداد فوق الناقة: شريط صغير غامق شفاف (بدل الخشب الكبير)
    void StyleTimer()
    {
        timerScreenOffset = new Vector2(0f, 70f);
        GameObject t = TimerObject;
        if (t == null) return;
        UnityEngine.UI.Image img = t.GetComponent<UnityEngine.UI.Image>();
        RectTransform rt = t.GetComponent<RectTransform>();
        if (img != null)
        {
            NaaqaUI.DarkPill(img, 14f);
            if (rt != null) { rt.sizeDelta = new Vector2(96, 34); rt.localScale = Vector3.one; }
        }
        foreach (TMP_Text tx in t.GetComponentsInChildren<TMP_Text>(true))
        {
            tx.color = NaaqaUI.Cream;
            tx.enableAutoSizing = false;
            tx.fontSize = 20;
            tx.fontStyle = FontStyles.Bold;
            tx.alignment = TextAlignmentOptions.Center;
            if (img != null && tx.transform != t.transform)
            {
                tx.rectTransform.anchorMin = Vector2.zero; tx.rectTransform.anchorMax = Vector2.one;
                tx.rectTransform.offsetMin = Vector2.zero; tx.rectTransform.offsetMax = new Vector2(0, -2);
                tx.rectTransform.localScale = Vector3.one;
            }
        }
        // صور قديمة داخل العداد (ساعة خشب) تختفي
        if (img != null)
            foreach (UnityEngine.UI.Image ci in t.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (ci != img && ci.name != "PillLine") ci.enabled = false;
    }

    void Start()
    {
        // وقت الحليب حسب ندرة الناقة (شائعة ٣٠ ث، نادرة ٢٥، أسطورية ٢٠)
        CamelBreeds.Breed br = CamelBreeds.Get(CamelBreeds.BreedOf(this));
        productionTime = GameRules.MilkTime(br != null ? br.rarity : 0);
        StyleTimer();
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);
        if (TimerObject != null) TimerObject.SetActive(false);

        started = true;
        if (hasPending)
        {
            hasPending = false;
            ApplyState(pendingReady, pendingProducing, pendingRemaining);
        }
    }

    // ===== الحفظ =====
    public bool IsMilkReady { get { return milkReady; } }
    public bool IsProducing { get { return isProducing; } }
    public float Remaining { get { return isProducing ? remaining : 0f; } }

    public void RestoreState(bool ready, bool producing, float timeLeft, bool dated = false)
    {
        pendingDated = dated;
        // لو الناقة لسه ما بدأت (مثل الناقة الثانية المطفية)، نطبقه في Start
        if (!started)
        {
            hasPending = true;
            pendingReady = ready;
            pendingProducing = producing;
            pendingRemaining = timeLeft;
            return;
        }
        ApplyState(ready, producing, timeLeft);
    }
    bool pendingDated;

    void ApplyState(bool ready, bool producing, float timeLeft)
    {
        StopAllCoroutines();
        CamelAnimator anim = GetComponent<CamelAnimator>();
        if (anim != null) anim.SetResting(false);
        isProducing = false;
        milkReady = false;
        if (TimerObject != null) TimerObject.SetActive(false);
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);

        if (ready || (producing && timeLeft <= 0f))
        {
            milkReady = true;
            if (milkReadyIcon != null) milkReadyIcon.SetActive(true);
        }
        else if (producing)
        {
            StartCoroutine(ProduceMilk(timeLeft));
            datedThisCycle = pendingDated;   // التمرة اللي انحطت قبل الخروج تنحسب (ما يحط ثانية)
        }
    }

    void LateUpdate()
    {
        if (TimerObject != null)
        {
            // العداد ما يطلع على شاشة البداية
            UIManager ui = UIManager.Instance;
            bool menu = ui != null && ((ui.startScreen != null && ui.startScreen.activeInHierarchy) || (ui.hud != null && !ui.hud.activeInHierarchy));
            bool show = isProducing && !menu;
            if (TimerObject.activeSelf != show) TimerObject.SetActive(show);
            if (show) FollowCamel();
        }
    }

    void OnMouseDown()
    {
        // وقت التركيب الناقة ما تنضغط
        if (PlacementManager.IsPlacing || EditMode.Active)
        {
            Debug.Log("الناقة ما تنضغط: وضع التركيب شغال");
            return;
        }

        // إذا فيه نافذة أو زر فوق الناقة، الضغطة له مو للناقة
        if (IsPointerOverBlockingUI()) return;

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.ClickCamel);

        if (milkReady)
            CollectMilk();
        else if (TutorialDone())
            CamelActions.Show(this);   // بعد الشرح: زرين فوقها (أطعم / معلومات)
        else if (!isProducing)
            FeedCamel();
        else
            Notify("الناقة تنتج الحين، انتظر شوي");
    }

    // يتأكد إن فيه واجهة فوق الماوس (ويتجاهل ظل الشرح)
    bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (RaycastResult r in results)
        {
            if (r.gameObject.GetComponentInParent<TutorialSpotlight>() != null) continue;
            Debug.Log("الضغطة انمسكت من: " + r.gameObject.name, r.gameObject);
            return true;
        }
        return false;
    }

    static bool TutorialDone()
    {
        OrderManager om = FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
        return om == null || om.IsUpgraded;
    }

    // زر «أطعم» اللي فوق الناقة
    public void Feed()
    {
        if (milkReady) { CollectMilk(); return; }
        if (isProducing) { Notify("الناقة تنتج الحين، انتظر شوي"); return; }
        FeedCamel();
    }

    void FeedCamel() { FeedWith(false, false); }

    // كل دورة: علفة وحدة + مويه وحدة = حليبة وحدة. quiet = الحلاب (بدون تنبيهات)
    // (التمر ما يصير أكل: يسرّع الدورة بس، تمرة وحدة لكل دورة)
    bool FeedWith(bool dates, bool quiet)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;

        if (gm.feedCount < 1) { if (!quiet) NotifyWarning("العلف غير كافٍ! اشترِ علف أو ازرع"); return false; }
        if (dates && (FarmStore.Instance == null || FarmStore.Instance.Dates < 1)) { if (!quiet) NotifyWarning("ما عندك تمر كافي!"); return false; }

        // تشرب حبة مويه مع الأكل
        if (!WaterSystem.TryDrink(1, !quiet)) return false;

        gm.SpendFeed(1);
        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        Quests.Add("feed");

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.FeedCamel);

        CamelAnimator anim = GetComponent<CamelAnimator>();
        if (anim != null) anim.PlayEat();
        if (!quiet) CamelSounds.Play("camel");

        StartCoroutine(ProduceMilk(productionTime));
        if (dates) UseDate();
        return true;
    }

    // ===== التمر: تمرة وحدة لكل دورة تنصّف الوقت الباقي =====
    bool datedThisCycle;
    public bool DatedThisCycle { get { return datedThisCycle; } }
    public bool CanUseDate { get { return !milkReady && !datedThisCycle && FarmStore.Instance != null && FarmStore.Instance.Dates > 0; } }

    void UseDate()
    {
        if (!isProducing || datedThisCycle || milkReady) return;
        if (FarmStore.Instance == null || FarmStore.Instance.Dates < 1) return;
        FarmStore.Instance.AddDates(-1);   // تنخصم على طول، وما تقدر تنباع بعدها
        datedThisCycle = true;
        remaining *= 0.5f;
        CamelAnimator anim = GetComponent<CamelAnimator>();
        if (anim != null) anim.PlayEat();
    }

    // زر «تمر» فوق الناقة: لو تنتج الحين ينصّف الوقت الباقي، ولو جوعانة تاكل علف + تمرة
    public void FeedDates()
    {
        if (milkReady) { CollectMilk(); return; }
        if (isProducing)
        {
            if (datedThisCycle) { NotifyWarning("أكلت تمرة في هالدورة، ما تاكل ثانية"); return; }
            if (FarmStore.Instance == null || FarmStore.Instance.Dates < 1) { NotifyWarning("ما عندك تمر كافي!"); return; }
            UseDate();
            return;
        }
        FeedWith(true, false);
    }

    // ===== الحلاب =====
    public bool WorkerFeed()
    {
        if (milkReady || isProducing) return false;
        return FeedWith(false, true);
    }

    public bool WorkerCollect()
    {
        if (!milkReady || GameManager.Instance == null) return false;
        GameManager.Instance.AddMilk(1);
        Quests.Add("milk");
        if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
        milkReady = false;
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);
        LevelManager.AddXp(LevelManager.XpPerMilk);
        return true;
    }

    IEnumerator ProduceMilk(float duration)
    {
        isProducing = true;
        datedThisCycle = false;
        remaining = duration;
        CamelAnimator rest = GetComponent<CamelAnimator>();
        if (rest != null) rest.SetResting(true);

        if (TimerObject != null)
        {
            TimerObject.SetActive(true);
            FollowCamel();
        }

        while (remaining > 0f)
        {
            if (timerText != null)
                timerText.text = FormatTime(remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }

        isProducing = false;
        milkReady = true;
        if (rest != null) rest.SetResting(false);

        if (TimerObject != null) TimerObject.SetActive(false);
        if (milkReadyIcon != null) milkReadyIcon.SetActive(true);

        Notify("الحليب جاهز! اضغط على الناقة");
    }

    void CollectMilk()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!", this);
            return;
        }

        GameManager.Instance.AddMilk(1);
        Quests.Add("milk");

        if (MilkManager.Instance != null)
            MilkManager.Instance.UpdateMilkText();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.CollectMilk);

        milkReady = false;
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);

        LevelManager.AddXp(LevelManager.XpPerMilk);
        CamelAnimator happy = GetComponent<CamelAnimator>();
        if (happy != null) { happy.SetResting(false); happy.PlayHappy(); }
        Notify("+" + ToArabicNumbers("1") + " حليب");
    }

    void Notify(string message)
    {
        Debug.Log(message);
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show(message);
    }

    void NotifyWarning(string message)
    {
        Debug.Log(message);
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(message);
    }

    string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        int m = total / 60;
        int s = total % 60;

        string result = m.ToString("00") + ":" + s.ToString("00");
        return arabicDigits ? ToArabicNumbers(result) : result;
    }

    void FollowCamel()
    {
        Camera cam = Camera.main;
        GameObject target = TimerObject;
        if (cam == null || target == null) return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null) return;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        RectTransform parent = rect.parent as RectTransform;
        if (canvas == null || parent == null) return;

        // فوق راس الناقة (أعلى نقطة في صورتها)، مو عند رجولها
        Vector3 top = transform.position;
        SpriteRenderer body = null; float area = 0f;
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled || (milkReadyIcon != null && r.transform.IsChildOf(milkReadyIcon.transform))) continue;
            float a = r.bounds.size.x * r.bounds.size.z;
            if (a > area) { area = a; body = r; }
        }
        if (body != null) { Bounds bb = body.bounds; top = new Vector3(bb.center.x, bb.max.y, bb.max.z); }
        Vector2 screenPos = (Vector2)cam.WorldToScreenPoint(top) + new Vector2(0f, 22f);
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCam, out localPos))
            rect.localPosition = localPos;
    }

    string ToArabicNumbers(string text)
    {
        char[] chars = text.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }

        return new string(chars);
    }
}