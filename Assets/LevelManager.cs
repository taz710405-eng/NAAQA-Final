using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== المستوى والخبرة =====
// كل مهمة وطلب وحليب يعطي خبرة، ولما تمتلي الخبرة يرتفع المستوى ويجي إشعار ومكافأة.
// الشريط يطلع فوق في الوسط (ينبني لحاله داخل HUD، بدون تعديل المشهد).
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    // ===== أرقام التوازن (تقدرين تغيرينها هنا) =====
    // (نظام الاقتصاد النهائي: NAAQA_ECONOMY_SYSTEM_FINAL)
    public const int XpPerTask = 15;       // مهام الشرح
    public const int XpPerOrder = 3;       // تسليم طلب زبون
    public const int XpPerMilk = 5;        // جمع حليب من الناقة
    public const int XpPerHarvest = 3;     // حصاد مزرعة
    public const int XpPerPurchase = 2;    // شراء غرض (متجر، ناقة، تطوير)
    public const int XpPerRace = 20;       // إكمال سباق (فوز أو خسارة)
    public const int XpPerMazayen = 25;    // إكمال المزاين
    public const int XpPerTraveler = 4;    // مسافر في المصلى

    // الخبرة المطلوبة من المستوى الحالي للي بعده: ١٢٠ + ٣٠ × (المستوى − ١) → مستوى ٢٢ = ٨٨٢٠ خبرة كلها
    public static int XpForLevel(int level) { return 120 + 30 * (Mathf.Max(1, level) - 1); }
    // مكافأة الوصول لمستوى جديد: ١٠ + ٢ × المستوى الجديد
    public static int LevelReward(int newLevel) { return 10 + 2 * newLevel; }

    public int Level { get; private set; }
    public int Xp { get; private set; }

    // الواجهة
    RectTransform widget;
    Image fill;
    RTLTextMeshPro levelText, xpText;
    float shownFill = 0f;
    float pulse = 0f;

    // ===== تركيب تلقائي =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;  // مشهد ما فيه اللعبة
        if (Instance != null) return;
        GameObject g = new GameObject("LevelManager");
        Instance = g.AddComponent<LevelManager>();
    }

    void Awake()
    {
        Level = 1;
        Xp = 0;
        ShopItemView.OnItemBought += OnBought;
    }

    void OnDestroy()
    {
        ShopItemView.OnItemBought -= OnBought;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        BuildWidget();
        Refresh(true);
    }

    void OnBought(string id) { AddXp(XpPerPurchase); }

    // ===== إضافة خبرة (أي سكربت يناديها) =====
    public static void AddXp(int amount)
    {
        if (Instance == null || amount <= 0) return;
        Instance.Gain(amount);
    }

    void Gain(int amount)
    {
        Xp += amount;
        bool up = false;
        while (Xp >= XpForLevel(Level))
        {
            Xp -= XpForLevel(Level);
            Level++;
            up = true;

            int bonus = LevelReward(Level);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(bonus);
                if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
            }
            if (ToastView.Instance != null)
                ToastView.Instance.Show("مستوى جديد! وصلت المستوى " + NaaqaUI.Ar(Level) + " (+" + NaaqaUI.Ar(bonus) + " قرش)", 3.5f);
            else if (NotificationManager.Instance != null)
                NotificationManager.Instance.Show("مستوى جديد! المستوى " + NaaqaUI.Ar(Level));

            string unlock = GameRules.UnlockMessage(Level);
            if (unlock != null && NotificationManager.Instance != null) NotificationManager.Instance.Show(unlock);
        }
        if (up) pulse = 1f;
        Refresh(false);
    }

    // من الحفظ
    public void Restore(int level, int xp)
    {
        Level = Mathf.Max(1, level);
        Xp = Mathf.Max(0, xp);
        Refresh(true);
    }

    // ===== الواجهة =====
    void BuildWidget()
    {
        if (widget != null) return;
        Transform hud = UIManager.Instance != null && UIManager.Instance.hud != null ? UIManager.Instance.hud.transform : null;
        if (hud == null) return;

        widget = NaaqaUI.Rect("LevelBar", hud);
        widget.anchorMin = widget.anchorMax = new Vector2(0.5f, 1f);
        widget.pivot = new Vector2(0.5f, 1f);
        widget.sizeDelta = new Vector2(380, 84);
        widget.anchoredPosition = new Vector2(330, -14);

        // خلفية السدو (نفس عدادات القروش والحليب)، تحتها شريط السدو
        Image bg = NaaqaUI.Img(widget, "Bg", NaaqaUI.Rounded(), new Color(0.12f, 0.07f, 0.03f, 0.82f), 38f);
        NaaqaUI.Stretch(bg.rectTransform, 0, 40, 0, 0);
        bool sadu = SaduFit.Apply(bg, "pill");
        if (!sadu)
        {
            Image line = NaaqaUI.Img(widget, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.75f, 0.4f, 0.45f), 38f);
            NaaqaUI.Stretch(line.rectTransform, 0, 40, 0, 0);
        }
        float bottom = sadu ? 30f : 12f;   // فوق شريط السدو

        // الشريط
        Image track = NaaqaUI.Img(widget, "Track", NaaqaUI.Rounded(), new Color(1, 1, 1, 0.14f), 7f);
        track.rectTransform.anchorMin = new Vector2(0, 0);
        track.rectTransform.anchorMax = new Vector2(1, 0);
        track.rectTransform.offsetMin = new Vector2(26, bottom);
        track.rectTransform.offsetMax = new Vector2(-112, bottom + 13);

        RectTransform mask = NaaqaUI.Rect("Mask", track.transform);
        NaaqaUI.Stretch(mask);
        mask.gameObject.AddComponent<RectMask2D>();
        fill = NaaqaUI.Img(mask, "Fill", NaaqaUI.RoundedGradient(NaaqaUI.OrangeLight, NaaqaUI.OrangeDark, "btnPrimary"), Color.white, 7f);
        fill.rectTransform.anchorMin = new Vector2(1, 0);   // يتعبى من اليمين (عربي)
        fill.rectTransform.anchorMax = new Vector2(1, 1);
        fill.rectTransform.pivot = new Vector2(1, 0.5f);
        fill.rectTransform.anchoredPosition = Vector2.zero;

        // «المستوى» + الخبرة فوق الشريط
        RTLTextMeshPro title = NaaqaUI.Text(widget, "Title", "المستوى", 21, NaaqaUI.Cream, TextAlignmentOptions.MidlineRight, true);
        title.rectTransform.anchorMin = new Vector2(0, 0);
        title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(26, bottom + 14);
        title.rectTransform.offsetMax = new Vector2(-114, -6);

        xpText = NaaqaUI.Text(widget, "Xp", "", 19, NaaqaUI.Cream, TextAlignmentOptions.MidlineLeft);
        xpText.rectTransform.anchorMin = new Vector2(0, 0);
        xpText.rectTransform.anchorMax = new Vector2(1, 1);
        xpText.rectTransform.offsetMin = new Vector2(26, bottom + 14);
        xpText.rectTransform.offsetMax = new Vector2(-114, -6);

        // دائرة الرقم (يمين): إطار السدو
        Sprite frame = NaaqaUI.Load("sadu_frame");
        Image circle = NaaqaUI.Img(widget, "Badge", frame != null ? frame : NaaqaUI.Circle(), frame != null ? Color.white : NaaqaUI.Orange);
        circle.preserveAspect = true;
        circle.rectTransform.anchorMin = circle.rectTransform.anchorMax = new Vector2(1, 0.5f);
        circle.rectTransform.pivot = new Vector2(1, 0.5f);
        circle.rectTransform.sizeDelta = new Vector2(96, 96);
        circle.rectTransform.anchoredPosition = new Vector2(0, 0);
        levelText = NaaqaUI.Text(circle.transform, "Num", "١", 36, frame != null ? NaaqaUI.OrangeLight : Color.white, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(levelText.rectTransform, 0, 0, 0, 3);
    }

    // HudSkin يحطه بالزاوية فوق يمين
    public void MoveToCorner()
    {
        if (widget == null) BuildWidget();
        if (widget == null) return;
        NaaqaUI.Place(widget, new Vector2(1f, 1f), new Vector2(380, 84), new Vector2(-12, -10));
    }

    void Refresh(bool instant)
    {
        if (widget == null) return;
        levelText.text = NaaqaUI.Ar(Level);
        xpText.text = NaaqaUI.Ar(Xp) + " / " + NaaqaUI.Ar(XpForLevel(Level));
        if (instant) shownFill = Target();
    }

    float Target() { return Mathf.Clamp01(Xp / (float)XpForLevel(Level)); }

    void Update()
    {
        if (widget == null || fill == null) return;

        // الشريط يتعبى بنعومة
        shownFill = Mathf.MoveTowards(shownFill, Target(), Time.unscaledDeltaTime * 0.8f);
        RectTransform track = (RectTransform)fill.transform.parent;
        fill.rectTransform.sizeDelta = new Vector2(track.rect.width * shownFill, 0);

        // نبضة عند المستوى الجديد
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime * 1.5f);
            widget.localScale = Vector3.one * (1f + Mathf.Sin(pulse * Mathf.PI) * 0.12f);
        }
    }
}
