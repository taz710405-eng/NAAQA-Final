using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ===== ملف اللاعب (يفتح من الخيمة) =====
// صورة رمزية يختارها، واسمه في اللعبة، وإحصائياته، وحفظ التغييرات، وتسجيل الخروج.
public class ProfileScreen : MonoBehaviour
{
    public static ProfileScreen Instance;
    public const int AvatarCount = 6;

    Canvas canvas;
    CanvasGroup group;
    RectTransform card;

    Image bigAvatar;
    RTLTextMeshPro headerName;
    TextMeshProUGUI headerEmail;
    TMP_InputField nameField;
    RTLTextMeshPro levelBadge;
    readonly List<Image> avatarRings = new List<Image>();
    readonly Dictionary<string, RTLTextMeshPro> stats = new Dictionary<string, RTLTextMeshPro>();

    Button saveButton, logoutButton;
    RTLTextMeshPro saveText, logoutText, status;

    RectTransform canvasRect;
    float animScale = 1f;
    int avatar = 0;
    int savedAvatar = 0;
    string savedName = "";
    bool busy = false;
    float logoutArmedUntil = 0f;

    // ===== فتح =====
    public static void Show()
    {
        AccountManager acc = AccountManager.Instance;
        if (acc == null || !acc.IsLoggedIn)
        {
            // ما فيه حساب: نطلب منه يسجل دخول
            LoginScreen.Show();
            return;
        }

        if (PlacementManager.IsPlacing) return;

        if (Instance == null)
        {
            GameObject g = new GameObject("ProfileScreen");
            Instance = g.AddComponent<ProfileScreen>();
            Instance.Build();
        }
        Instance.gameObject.SetActive(true);
        Instance.Open();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static Sprite AvatarSprite(string id)
    {
        int i;
        if (!int.TryParse(id, out i)) i = 0;
        return NaaqaUI.Load("avatar_" + Mathf.Clamp(i, 0, AvatarCount - 1));
    }

    // ===== البناء =====
    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("ProfileCanvas", 850);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        Transform root = canvas.transform;
        canvasRect = (RectTransform)root;

        // خلفية معتمة، والضغط عليها يسكّر
        Image dim = NaaqaUI.Img(root, "Dim", null, new Color(0.04f, 0.02f, 0f, 0.62f));
        NaaqaUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(Close);

        card = NaaqaUI.Rect("Card", root);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(900, 0);

        VerticalLayoutGroup v = card.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(52, 52, 36, 30);
        v.spacing = 10;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        ContentSizeFitter csf = card.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image shadow = NaaqaUI.Img(card, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.7f), 36f);
        NaaqaUI.Stretch(shadow.rectTransform, -40, -40, -30, -50);
        Ignore(shadow);
        Image bg = NaaqaUI.Img(card, "Bg", NaaqaUI.RoundedGradient(new Color32(42, 27, 16, 246), new Color32(22, 13, 8, 250), "profileBg"), Color.white, 36f);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;   // الضغط داخل البطاقة ما يسكّرها
        Ignore(bg);
        Image border = NaaqaUI.Img(card, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.28f), 36f);
        NaaqaUI.Stretch(border.rectTransform);
        Ignore(border);

        BuildClose(card);
        BuildIdentity(card);
        Divider(card);

        Label(card, "اختر صورتك");
        BuildAvatarPicker(card);

        Label(card, "اسمك في اللعبة");
        nameField = NaaqaUI.Field(card, "NameField", "اكتب اسمك", true, TMP_InputField.ContentType.Standard, 68);
        nameField.characterLimit = 20;
        nameField.onValueChanged.AddListener(_ => RefreshSaveButton());

        Label(card, "إحصائياتك");
        BuildStats(card);

        Spacer(card, 4);
        BuildButtons(card);

        status = NaaqaUI.Text(card, "Status", "", 23, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(status, 30);
    }

    // زر الإغلاق في زاوية البطاقة (برا الترتيب)
    void BuildClose(Transform parent)
    {
        RTLTextMeshPro x;
        Button close = NaaqaUI.MakeButton(parent, "Close", "×", NaaqaUI.ButtonStyle.Secondary, 40, out x);
        Ignore(close);
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
        crt.pivot = new Vector2(0f, 1f);
        crt.sizeDelta = new Vector2(56, 56);
        crt.anchoredPosition = new Vector2(28, -28);
        close.onClick.AddListener(Close);
    }

    void BuildIdentity(Transform parent)
    {
        RectTransform row = NaaqaUI.Rect("Identity", parent);
        NaaqaUI.Height(row, 150);

        // الصورة يمين مع حلقة برتقالية
        Image ring = NaaqaUI.Img(row, "Ring", NaaqaUI.Circle(), NaaqaUI.Orange);
        ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        ring.rectTransform.pivot = new Vector2(1f, 0.5f);
        ring.rectTransform.sizeDelta = new Vector2(150, 150);
        ring.rectTransform.anchoredPosition = Vector2.zero;
        Image gap = NaaqaUI.Img(ring.transform, "Gap", NaaqaUI.Circle(), new Color32(32, 20, 12, 255));
        NaaqaUI.Center(gap.rectTransform, new Vector2(140, 140), Vector2.zero);
        bigAvatar = NaaqaUI.Img(ring.transform, "Avatar", AvatarSprite("0"), Color.white);
        NaaqaUI.Center(bigAvatar.rectTransform, new Vector2(128, 128), Vector2.zero);
        bigAvatar.preserveAspect = true;

        // الاسم والبريد يسارها
        RectTransform info = NaaqaUI.Rect("Info", row);
        NaaqaUI.Stretch(info, 90, 180, 0, 0);

        headerName = NaaqaUI.Text(info, "Name", "", 44, NaaqaUI.Cream, TextAlignmentOptions.BottomRight, true);
        headerName.rectTransform.anchorMin = new Vector2(0, 0.5f);
        headerName.rectTransform.anchorMax = new Vector2(1, 1);
        headerName.rectTransform.offsetMin = new Vector2(0, 8);
        headerName.rectTransform.offsetMax = new Vector2(0, -12);

        headerEmail = NaaqaUI.Latin(info, "Email", "", 25, NaaqaUI.Muted, TextAlignmentOptions.TopRight);
        headerEmail.rectTransform.anchorMin = new Vector2(0, 0.2f);
        headerEmail.rectTransform.anchorMax = new Vector2(1, 0.5f);
        headerEmail.rectTransform.offsetMin = headerEmail.rectTransform.offsetMax = Vector2.zero;

        levelBadge = NaaqaUI.Text(info, "Badge", "تقدمك محفوظ في حسابك", 21, NaaqaUI.OkColor, TextAlignmentOptions.TopRight);
        RTLTextMeshPro badge = levelBadge;
        badge.rectTransform.anchorMin = new Vector2(0, 0f);
        badge.rectTransform.anchorMax = new Vector2(1, 0.22f);
        badge.rectTransform.offsetMin = badge.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildAvatarPicker(Transform parent)
    {
        RectTransform row = NaaqaUI.Rect("Avatars", parent);
        NaaqaUI.Height(row, 96);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.reverseArrangement = true;
        h.spacing = 18;
        h.childAlignment = TextAnchor.MiddleRight;
        h.childControlWidth = false; h.childControlHeight = false;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        for (int i = 0; i < AvatarCount; i++)
        {
            int id = i;
            Image ring = NaaqaUI.Img(row, "Pick" + i, NaaqaUI.Circle(), NaaqaUI.Orange);
            ring.rectTransform.sizeDelta = new Vector2(90, 90);
            ring.raycastTarget = true;
            Image pic = NaaqaUI.Img(ring.transform, "Pic", AvatarSprite(i.ToString()), Color.white);
            NaaqaUI.Center(pic.rectTransform, new Vector2(78, 78), Vector2.zero);

            Button b = ring.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => PickAvatar(id));
            ring.gameObject.AddComponent<NaaqaButtonFx>();
            avatarRings.Add(ring);
        }
    }

    void BuildStats(Transform parent)
    {
        RectTransform grid = NaaqaUI.Rect("Stats", parent);
        NaaqaUI.Height(grid, 196);
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(252, 90);
        g.spacing = new Vector2(16, 16);
        g.startCorner = GridLayoutGroup.Corner.UpperRight;
        g.startAxis = GridLayoutGroup.Axis.Horizontal;
        g.childAlignment = TextAnchor.UpperCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;

        StatTile(grid, "money", "القروش");
        StatTile(grid, "milk", "الحليب");
        StatTile(grid, "feed", "العلف");
        StatTile(grid, "camels", "الإبل");
        StatTile(grid, "orders", "الطلبات المسلّمة");
        StatTile(grid, "tasks", "المهام");
    }

    void StatTile(Transform parent, string key, string label)
    {
        Image tile = NaaqaUI.Img(parent, "Stat_" + key, NaaqaUI.Rounded(), new Color(1, 1, 1, 0.06f), 20f);
        RTLTextMeshPro value = NaaqaUI.Text(tile.transform, "Value", "٠", 36, NaaqaUI.OrangeLight, TextAlignmentOptions.Center, true);
        value.rectTransform.anchorMin = new Vector2(0, 0.38f);
        value.rectTransform.anchorMax = new Vector2(1, 1f);
        value.rectTransform.offsetMin = value.rectTransform.offsetMax = Vector2.zero;
        RTLTextMeshPro lbl = NaaqaUI.Text(tile.transform, "Label", label, 21, NaaqaUI.Muted, TextAlignmentOptions.Center);
        lbl.rectTransform.anchorMin = new Vector2(0, 0.06f);
        lbl.rectTransform.anchorMax = new Vector2(1, 0.42f);
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
        stats[key] = value;
    }

    void BuildButtons(Transform parent)
    {
        RectTransform row = NaaqaUI.Rect("Buttons", parent);
        NaaqaUI.Height(row, 76);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.reverseArrangement = true;
        h.spacing = 16;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;

        saveButton = NaaqaUI.MakeButton(row, "Save", "حفظ التغييرات", NaaqaUI.ButtonStyle.Primary, 30, out saveText);
        LayoutElement se = saveButton.gameObject.AddComponent<LayoutElement>();
        se.flexibleWidth = 1f;
        saveButton.onClick.AddListener(OnSave);

        logoutButton = NaaqaUI.MakeButton(row, "Logout", "تسجيل الخروج", NaaqaUI.ButtonStyle.Danger, 27, out logoutText);
        LayoutElement le = logoutButton.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 290f;
        logoutButton.onClick.AddListener(OnLogout);
    }

    RTLTextMeshPro Label(Transform parent, string text)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Label", text, 23, new Color(1f, 0.9f, 0.78f, 0.85f), TextAlignmentOptions.BottomRight);
        NaaqaUI.Height(t, 30);
        return t;
    }

    void Divider(Transform parent)
    {
        Image d = NaaqaUI.Img(parent, "Divider", null, new Color(1f, 0.85f, 0.65f, 0.12f));
        NaaqaUI.Height(d, 2);
    }

    void Spacer(Transform parent, float h)
    {
        NaaqaUI.Height(NaaqaUI.Rect("Space", parent), h);
    }

    static void Ignore(Component c)
    {
        c.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    // ===== تعبئة البيانات =====
    void Open()
    {
        AccountManager acc = AccountManager.Instance;
        savedName = acc != null ? (acc.DisplayName ?? "") : "";
        int.TryParse(acc != null ? acc.AvatarId : "0", out savedAvatar);
        savedAvatar = Mathf.Clamp(savedAvatar, 0, AvatarCount - 1);
        avatar = savedAvatar;

        headerName.text = savedName;
        headerEmail.text = acc != null ? acc.UserEmail : "";
        nameField.text = savedName;
        logoutArmedUntil = 0f;
        logoutText.text = "تسجيل الخروج";
        SetStatus("", NaaqaUI.Muted);

        ApplyAvatar();
        RefreshStats();
        RefreshSaveButton();

        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    void RefreshStats()
    {
        GameManager gm = GameManager.Instance;
        SetStat("money", gm != null ? gm.money : 0);
        SetStat("milk", gm != null ? gm.milkCount : 0);
        SetStat("feed", gm != null ? gm.feedCount : 0);

        int camels = 0;
        foreach (Camel c in FindObjectsByType<Camel>(FindObjectsInactive.Exclude)) camels++;
        SetStat("camels", camels);

        OrderManager om = FindAnyObjectByType<OrderManager>();
        SetStat("orders", om != null ? om.OrdersCompleted : 0);

        int done = 0, total = 0;
        if (TaskManager.Instance != null)
        {
            bool[] d = TaskManager.Instance.GetDone();
            total = d.Length;
            foreach (bool b in d) if (b) done++;
        }
        stats["tasks"].text = NaaqaUI.Ar(done) + " / " + NaaqaUI.Ar(total);

        if (levelBadge != null)
        {
            int lvl = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
            levelBadge.text = "المستوى " + NaaqaUI.Ar(lvl) + "  •  تقدمك محفوظ في حسابك";
        }
    }

    void SetStat(string key, int value)
    {
        RTLTextMeshPro t;
        if (stats.TryGetValue(key, out t)) t.text = NaaqaUI.Ar(value);
    }

    void PickAvatar(int id)
    {
        if (busy) return;
        avatar = id;
        ApplyAvatar();
        RefreshSaveButton();
    }

    void ApplyAvatar()
    {
        bigAvatar.sprite = AvatarSprite(avatar.ToString());
        for (int i = 0; i < avatarRings.Count; i++)
        {
            bool on = i == avatar;
            avatarRings[i].color = on ? NaaqaUI.Orange : new Color(1, 1, 1, 0.12f);
            avatarRings[i].transform.GetChild(0).localScale = Vector3.one * (on ? 1f : 0.92f);
        }
    }

    bool Changed()
    {
        return avatar != savedAvatar || nameField.text.Trim() != savedName;
    }

    void RefreshSaveButton()
    {
        if (saveButton == null) return;
        saveButton.interactable = !busy && Changed();
    }

    // لو الشاشة قصيرة، البطاقة تصغر عشان تدخل كاملة
    void LateUpdate()
    {
        if (card == null || canvasRect == null) return;
        float h = card.rect.height;
        float fit = h > 0f ? Mathf.Min(1f, (canvasRect.rect.height - 50f) / h) : 1f;
        card.localScale = Vector3.one * fit * animScale;
    }

    void Update()
    {
        // الإحصائيات تتحدث وهي مفتوحة
        if (Time.frameCount % 20 == 0) RefreshStats();

        if (logoutArmedUntil > 0f && Time.unscaledTime > logoutArmedUntil)
        {
            logoutArmedUntil = 0f;
            logoutText.text = "تسجيل الخروج";
        }
    }

    // ===== حفظ =====
    void OnSave()
    {
        if (busy || !Changed()) return;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) return;

        string name = nameField.text.Trim();
        string err = acc.ValidateName(name);
        if (err != null) { SetStatus(err, NaaqaUI.ErrorColor); return; }

        busy = true;
        RefreshSaveButton();
        saveText.text = "جاري الحفظ...";
        StartCoroutine(acc.ApiUpdateProfile(name, avatar.ToString(), e =>
        {
            busy = false;
            saveText.text = "حفظ التغييرات";
            if (e != null) { SetStatus(e, NaaqaUI.ErrorColor); RefreshSaveButton(); return; }

            savedName = name;
            savedAvatar = avatar;
            headerName.text = name;
            SetStatus("تم حفظ التغييرات", NaaqaUI.OkColor);
            RefreshSaveButton();
        }));
    }

    // ===== تسجيل الخروج (ضغطتين للتأكيد) =====
    void OnLogout()
    {
        if (busy) return;
        if (Time.unscaledTime > logoutArmedUntil)
        {
            logoutArmedUntil = Time.unscaledTime + 3f;
            logoutText.text = "متأكد؟ اضغط مرة ثانية";
            return;
        }
        StartCoroutine(LogoutFlow());
    }

    IEnumerator LogoutFlow()
    {
        busy = true;
        saveButton.interactable = false;
        logoutButton.interactable = false;
        SetStatus("نحفظ تقدمك قبل الخروج...", NaaqaUI.Muted);

        if (SaveManager.Instance != null)
            yield return SaveManager.Instance.FlushToCloud();

        if (AccountManager.Instance != null) AccountManager.Instance.LogoutNow();
        if (SaveManager.Instance != null) SaveManager.Instance.PrepareFreshStart();

        // نبدأ اللعبة من جديد على شاشة البداية
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SetStatus(string msg, Color c)
    {
        status.color = c;
        status.text = msg;
    }

    // ===== حركات =====
    IEnumerator FadeIn()
    {
        group.alpha = 0f;
        group.blocksRaycasts = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            group.alpha = k;
            animScale = Mathf.Lerp(0.94f, 1f, k);
            yield return null;
        }
        animScale = 1f;
    }

    public void Close()
    {
        if (busy) return;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        group.blocksRaycasts = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.2f;
            group.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
