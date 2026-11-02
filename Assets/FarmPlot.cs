using UnityEngine;
using UnityEngine.EventSystems;

// ===== المزرعة (علف أو تمر) =====
// تزرعها (تشرب ٥ مويه للدورة)، تستنى، وتحصد.
// العلف: ٥ / ٧ / ١٠ كل ٣ د / ٢:٣٠ / ٢ د. التمر: ٥ حبات كل ٥ دقايق.
// التطوير ١٥٠ ثم ٣٠٠ (يفتح مستوى ٥)، وشكلها يكبر مع التطوير.
public class FarmPlot : MonoBehaviour
{
    public enum State { Empty, Growing, Ready }

    public string id;            // feed_farm أو date_farm
    public int Level { get; private set; } = 1;
    public State state = State.Empty;
    public float left;

    public bool IsFeed { get { return id == "feed_farm"; } }
    public string ProductName { get { return IsFeed ? "علف" : "تمر"; } }
    public string Title { get { return IsFeed ? "مزرعة علف" : "مزرعة تمر"; } }
    public int Yield { get { return YieldAt(Level); } }
    public static int YieldAt(int l) { return l >= 3 ? 10 : l == 2 ? 7 : 5; }
    public float GrowTime { get { return IsFeed ? GameRules.FeedFarmSeconds[Mathf.Clamp(Level, 1, 3) - 1] : GameRules.DateFarmSeconds; } }
    public const int MaxLevel = 3;
    public static int UpgradeCost(int toLevel) { return GameRules.FarmUpgradeCost(toLevel); }

    SpriteRenderer sr, mark;
    float basePpu, baseWidth;
    Vector2 pivot;
    int shownLevel = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PlacementManager.OnItemPlaced -= OnPlaced;
        PlacementManager.OnItemPlaced += OnPlaced;
    }

    static void OnPlaced(string id, GameObject go)
    {
        if ((id != "feed_farm" && id != "date_farm") || go == null) return;
        FarmPlot f = go.GetComponent<FarmPlot>();
        if (f == null) { f = go.AddComponent<FarmPlot>(); f.id = id; }
    }

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            basePpu = sr.sprite.pixelsPerUnit;
            baseWidth = sr.sprite.bounds.size.x;
            pivot = new Vector2(sr.sprite.pivot.x / sr.sprite.rect.width, sr.sprite.pivot.y / sr.sprite.rect.height);
        }
        BoxCollider box = GetComponentInChildren<BoxCollider>(true);
        if (box != null && box.GetComponent<FarmTap>() == null) box.gameObject.AddComponent<FarmTap>().farm = this;

        // علامة «جاهز» فوق المزرعة
        GameObject m = new GameObject("ReadyMark");
        m.transform.SetParent(sr != null ? sr.transform : transform, false);
        mark = m.AddComponent<SpriteRenderer>();
        if (sr != null) { mark.sortingLayerID = sr.sortingLayerID; mark.sortingOrder = sr.sortingOrder + 20; }
        m.SetActive(false);
    }

    void Start()
    {
        mark.sprite = NaaqaUI.Icon(id == "date_farm" ? "dates_farm" : "feed");
        ApplyLook();
    }

    void Update()
    {
        if (state == State.Growing)
        {
            left -= Time.deltaTime;
            if (left <= 0f) { left = 0f; state = State.Ready; }
        }
        ApplyLook();
        UpdateTimer();
    }

    void OnDestroy() { if (timer != null) Destroy(timer.gameObject); }
    void OnDisable() { if (timer != null) timer.gameObject.SetActive(false); }

    // ===== عداد فوق المزرعة وهي تنبت (نفس عداد الناقة) =====
    static Canvas timerCanvas;
    RectTransform timer;
    RTLTMPro.RTLTextMeshPro timerText;

    // عدادات المزارع ما تطلع فوق شاشة البداية أو الدخول
    static bool OnMenu()
    {
        UIManager ui = UIManager.Instance;
        return ui != null && ((ui.startScreen != null && ui.startScreen.activeInHierarchy) || (ui.hud != null && !ui.hud.activeInHierarchy));
    }

    void UpdateTimer()
    {
        bool show = state == State.Growing && sr != null && sr.enabled && !PlacementManager.IsPlacing && !OnMenu();
        if (!show) { if (timer != null && timer.gameObject.activeSelf) timer.gameObject.SetActive(false); return; }
        if (timer == null)
        {
            if (timerCanvas == null) timerCanvas = NaaqaUI.MakeCanvas("FarmTimers", 30);
            UnityEngine.UI.Image pill = NaaqaUI.Img(timerCanvas.transform, "FarmTimer", null, Color.white);
            NaaqaUI.DarkPill(pill, 14f);
            pill.raycastTarget = false;
            timer = pill.rectTransform;
            timer.anchorMin = timer.anchorMax = Vector2.zero;
            timer.sizeDelta = new Vector2(110, 36);
            timerText = NaaqaUI.Text(timer, "T", "", 21, NaaqaUI.Cream, TMPro.TextAlignmentOptions.Center, true);
            timerText.rectTransform.anchorMin = Vector2.zero; timerText.rectTransform.anchorMax = Vector2.one;
            timerText.rectTransform.offsetMin = Vector2.zero; timerText.rectTransform.offsetMax = Vector2.zero;
            timerText.raycastTarget = false;
        }
        if (!timer.gameObject.activeSelf) timer.gameObject.SetActive(true);
        int s = Mathf.CeilToInt(left);
        timerText.text = NaaqaUI.Ar(s / 60) + ":" + NaaqaUI.Ar(s % 60 / 10) + NaaqaUI.Ar(s % 10);
        Camera cam = Camera.main;
        if (cam == null) return;
        Bounds b = sr.bounds;
        Vector3 sp = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, b.max.z));
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)timerCanvas.transform, (Vector2)sp + new Vector2(0f, 12f), null, out local))
        {
            RectTransform cr = (RectTransform)timerCanvas.transform;
            timer.anchoredPosition = local + cr.rect.size * 0.5f;
        }
    }

    void ApplyLook()
    {
        if (sr == null) return;
        if (shownLevel != Level && basePpu > 0f)
        {
            shownLevel = Level;
            Texture2D tex = Resources.Load<Texture2D>(ShopExtraItems.Folder + "/" + id + (Level >= 2 ? "_2" : ""));
            if (tex != null)
            {
                float w = baseWidth * (Level == 1 ? 1f : Level == 2 ? 1.2f : 1.4f);
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.width / w);
                BoxCollider box = GetComponentInChildren<BoxCollider>(true);
                if (box != null)
                {
                    Bounds lb = sr.sprite.bounds;
                    box.size = new Vector3(lb.size.x * 0.8f, lb.size.y * 0.8f, 0.02f);
                    box.center = new Vector3(lb.center.x, lb.center.y, 0f);
                }
            }
        }
        // فاضية = تربة باهتة، تنمو = عادي، جاهزة = علامة فوقها
        sr.color = state == State.Empty ? new Color(0.78f, 0.72f, 0.62f, 1f) : Color.white;
        bool ready = state == State.Ready;
        if (mark == null) return;
        if (mark.gameObject.activeSelf != ready) mark.gameObject.SetActive(ready);
        if (ready && sr.sprite != null)
        {
            Bounds lb = sr.sprite.bounds;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.25f;
            mark.transform.localPosition = new Vector3(lb.center.x, lb.max.y * 0.85f + bob, -0.05f);
            float want = baseWidth * 0.28f;
            float now = mark.sprite != null ? mark.sprite.bounds.size.x : 1f;
            mark.transform.localScale = Vector3.one * (want / Mathf.Max(0.01f, now));
        }
    }

    // ===== أفعال =====
    public string Plant()
    {
        if (state != State.Empty) return "المزرعة مزروعة من قبل";
        if (!WaterSystem.TryDrink(GameRules.FarmWaterPerCycle, true)) return "الماء غير كافٍ! تحتاج " + NaaqaUI.Ar(GameRules.FarmWaterPerCycle) + " مويه عشان تزرع";
        state = State.Growing;
        left = GrowTime;
        CamelSounds.Play("pour");
        return null;
    }

    public static System.Action<FarmPlot> OnHarvest;   // للشرح

    public string Harvest()
    {
        if (state != State.Ready) return "للحين ما جهزت";
        int y = Yield;
        if (IsFeed)
        {
            if (GameManager.Instance != null) GameManager.Instance.feedCount += y;
            if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        }
        else if (FarmStore.Instance != null) FarmStore.Instance.AddDates(y);
        state = State.Empty;
        LevelManager.AddXp(LevelManager.XpPerHarvest);
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show("حصدت +" + NaaqaUI.Ar(y) + " " + ProductName);
        if (OnHarvest != null) OnHarvest(this);
        return null;
    }

    public string Upgrade()
    {
        if (Level >= MaxLevel) return "وصلت أعلى مستوى";
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv < GameRules.FarmUpgradeUnlock) return "تطوير المزارع يفتح في المستوى " + NaaqaUI.Ar(GameRules.FarmUpgradeUnlock);
        int cost = UpgradeCost(Level + 1);
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.SpendMoney(cost)) return "ما عندك قروش كافية! التطوير يبي " + NaaqaUI.Ar(cost) + " قرش";
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        Level++;
        LevelManager.AddXp(LevelManager.XpPerPurchase);
        return null;
    }

    // ===== الحفظ (في خانة extra حق الغرض المتركب) =====
    public string SaveState() { return "farm:" + Level + ":" + (int)state + ":" + Mathf.RoundToInt(left); }

    public void LoadState(string s, float away)
    {
        if (string.IsNullOrEmpty(s) || !s.StartsWith("farm:")) return;
        string[] p = s.Split(':');
        int l, st, lf;
        if (p.Length > 1 && int.TryParse(p[1], out l)) Level = Mathf.Clamp(l, 1, MaxLevel);
        if (p.Length > 2 && int.TryParse(p[2], out st)) state = (State)Mathf.Clamp(st, 0, 2);
        if (p.Length > 3 && int.TryParse(p[3], out lf)) left = lf;
        if (state == State.Growing) { left -= away; if (left <= 0f) { left = 0f; state = State.Ready; } }
    }
}

// الضغط على المزرعة: لو جاهزة تنحصد، وإلا تفتح نافذتها
public class FarmTap : MonoBehaviour
{
    public FarmPlot farm;

    void OnMouseUpAsButton()
    {
        if (farm == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (PlacementManager.IsPlacing || EditMode.Active) return;
        if (farm.state == FarmPlot.State.Ready) { farm.Harvest(); return; }
        FarmScreen.Show(farm);
    }
}
