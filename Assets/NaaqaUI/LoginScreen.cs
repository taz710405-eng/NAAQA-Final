using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== شاشة تسجيل الدخول / حساب جديد =====
// تنبني كاملة بالكود (ما تحتاج شي في المشهد) وتطلع بعد زر «ابدأ».
// الخلفية نفس فيديو الصحراء، واللوقو يسار، والبطاقة يمين.
public class LoginScreen : MonoBehaviour
{
    public static LoginScreen Instance;

    // لو تبين زر «العب بدون حساب» خليها true
    public static bool AllowGuest = false;

    enum Mode { Login, SignUp, Otp }
    Mode mode = Mode.Login;

    // العناصر
    Canvas canvas;
    CanvasGroup rootGroup;
    RawImage video;
    RectTransform card;
    Vector2 cardPos;

    RTLTextMeshPro title, subtitle;
    GameObject tabs;
    Image tabLoginBg, tabSignUpBg;
    RTLTextMeshPro tabLoginText, tabSignUpText;

    GameObject form;
    GameObject nameLabel, passHint;
    TMP_InputField nameField, emailField, passField;
    RTLTextMeshPro eyeText;

    GameObject otpGroup;
    TextMeshProUGUI otpEmail;
    TMP_InputField codeField;
    RTLTextMeshPro resendText;
    Button resendButton;
    float resendReadyAt = 0f;

    Button mainButton;
    RTLTextMeshPro mainText;
    RTLTextMeshPro status;
    GameObject guestButton;

    bool busy = false;
    bool showPass = false;
    float animScale = 1f;   // للحركات
    RectTransform canvasRect;
    string busyLabel = "";

    // ===== فتح =====
    public static void ShowIfNeeded()
    {
        AccountManager acc = AccountManager.Instance;
        if (acc == null) { Debug.LogWarning("AccountManager مو موجود، نتخطى تسجيل الدخول"); return; }

        if (acc.IsLoggedIn)
        {
            if (NotificationManager.Instance != null && !string.IsNullOrEmpty(acc.DisplayName))
                NotificationManager.Instance.Show("هلا " + acc.DisplayName + "!");
            return;
        }
        Show();
    }

    public static void Show()
    {
        if (Instance != null) { Instance.gameObject.SetActive(true); Instance.Open(); return; }
        GameObject g = new GameObject("LoginScreen");
        Instance = g.AddComponent<LoginScreen>();
        Instance.Build();
        Instance.Open();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===== البناء =====
    void Build()
    {
        canvas = NaaqaUI.MakeCanvas("LoginCanvas", 900);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;
        canvasRect = (RectTransform)root;
        rootGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        // خلفية: فيديو + تظليل (أغمق جهة البطاقة)
        RectTransform vrt = NaaqaUI.Rect("VideoBG", root);
        NaaqaUI.Stretch(vrt);
        video = vrt.gameObject.AddComponent<RawImage>();
        video.raycastTarget = true;   // يمسك الضغطات عشان ما توصل للعبة
        AspectRatioFitter fit = vrt.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f / 9f;

        Image dim = NaaqaUI.Img(root, "Dim", null, new Color(0.05f, 0.02f, 0f, 0.28f));
        NaaqaUI.Stretch(dim.rectTransform);
        Image side = NaaqaUI.Img(root, "SideShade",
            NaaqaUI.HorizontalFade(new Color(0, 0, 0, 0f), new Color(0.06f, 0.025f, 0f, 0.7f), "loginSide"), Color.white);
        NaaqaUI.Stretch(side.rectTransform);

        BuildBrand(root);
        BuildCard(root);
        ApplyMode(Mode.Login, false);
    }

    void BuildBrand(Transform root)
    {
        RectTransform brand = NaaqaUI.Rect("Brand", root);
        brand.anchorMin = new Vector2(0f, 0f);
        brand.anchorMax = new Vector2(0.5f, 1f);
        brand.offsetMin = brand.offsetMax = Vector2.zero;

        Image logo = NaaqaUI.Img(brand, "Logo", NaaqaUI.Load("logo"), Color.white);
        logo.preserveAspect = true;
        NaaqaUI.Center(logo.rectTransform, new Vector2(780, 354), new Vector2(0, 70));

        RTLTextMeshPro tag = NaaqaUI.Text(brand, "Tagline", "من تراثنا تبدأ الحكاية", 40, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Center(tag.rectTransform, new Vector2(760, 60), new Vector2(0, -150));
        Shadow s1 = tag.gameObject.AddComponent<Shadow>();
        s1.effectColor = new Color(0.15f, 0.06f, 0f, 0.7f);
        s1.effectDistance = new Vector2(2, -3);

        RTLTextMeshPro sub = NaaqaUI.Text(brand, "Sub", "اعتنِ بإبلك، واجمع الحليب، وكبّر حوشك", 27, new Color(1f, 0.93f, 0.82f, 0.9f), TextAlignmentOptions.Center);
        NaaqaUI.Center(sub.rectTransform, new Vector2(760, 50), new Vector2(0, -205));
        Shadow s2 = sub.gameObject.AddComponent<Shadow>();
        s2.effectColor = new Color(0.15f, 0.06f, 0f, 0.6f);
        s2.effectDistance = new Vector2(1, -2);
    }

    void BuildCard(Transform root)
    {
        card = NaaqaUI.Rect("Card", root);
        card.anchorMin = card.anchorMax = new Vector2(0.74f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(700, 0);
        cardPos = card.anchoredPosition;

        VerticalLayoutGroup v = card.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(52, 52, 42, 34);
        v.spacing = 12;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        ContentSizeFitter csf = card.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ظل + خلفية + إطار (برا الترتيب)
        Image shadow = NaaqaUI.Img(card, "Shadow", NaaqaUI.SoftShadow(), new Color(1, 1, 1, 0.65f), 36f);
        NaaqaUI.Stretch(shadow.rectTransform, -40, -40, -30, -50);
        Ignore(shadow);
        Image bg = NaaqaUI.Img(card, "Bg", NaaqaUI.RoundedGradient(new Color32(40, 26, 15, 236), new Color32(20, 12, 7, 240), "cardBg"), Color.white, 36f);
        NaaqaUI.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Ignore(bg);
        Image border = NaaqaUI.Img(card, "Border", NaaqaUI.RoundedOutline(), new Color(1f, 0.72f, 0.35f, 0.28f), 36f);
        NaaqaUI.Stretch(border.rectTransform);
        Ignore(border);
        // خط برتقالي رفيع فوق
        Image accent = NaaqaUI.Img(card, "Accent", NaaqaUI.Rounded(), NaaqaUI.Orange, 3f);
        accent.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        accent.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        accent.rectTransform.sizeDelta = new Vector2(120, 6);
        accent.rectTransform.anchoredPosition = new Vector2(0, -3);
        Ignore(accent);

        // العنوان
        title = NaaqaUI.Text(card, "Title", "", 44, NaaqaUI.Cream, TextAlignmentOptions.Right, true);
        NaaqaUI.Height(title, 56);
        subtitle = NaaqaUI.Text(card, "Subtitle", "", 25, NaaqaUI.Muted, TextAlignmentOptions.Right);
        NaaqaUI.Height(subtitle, 38);

        Spacer(card, 4);
        BuildTabs(card);
        Spacer(card, 2);

        // النموذج
        RectTransform f = NaaqaUI.Rect("Form", card);
        form = f.gameObject;
        VerticalLayoutGroup fv = form.AddComponent<VerticalLayoutGroup>();
        fv.spacing = 8;
        fv.childControlWidth = true; fv.childControlHeight = true;
        fv.childForceExpandWidth = true; fv.childForceExpandHeight = false;

        nameLabel = Label(f, "اسمك في اللعبة").gameObject;
        nameField = NaaqaUI.Field(f, "NameField", "مثال: سارة", true, TMP_InputField.ContentType.Standard, 68);
        nameField.characterLimit = 20;
        GameObject nameGap = Spacer(f, 4);
        nameGap.name = "NameGap";

        Label(f, "البريد الإلكتروني");
        emailField = NaaqaUI.Field(f, "EmailField", "name@gmail.com", false, TMP_InputField.ContentType.EmailAddress, 68);
        Spacer(f, 4);

        Label(f, "كلمة المرور");
        passField = NaaqaUI.Field(f, "PassField", "••••••••", false, TMP_InputField.ContentType.Password, 68);
        // زر إظهار/إخفاء داخل الخانة (يسار)
        RTLTextMeshPro eyeLbl;
        Button eye = NaaqaUI.MakeButton(passField.transform, "Eye", "إظهار", NaaqaUI.ButtonStyle.Ghost, 22, out eyeLbl);
        RectTransform ert = (RectTransform)eye.transform;
        ert.anchorMin = ert.anchorMax = new Vector2(0f, 0.5f);
        ert.pivot = new Vector2(0f, 0.5f);
        ert.sizeDelta = new Vector2(110, 52);
        ert.anchoredPosition = new Vector2(8, 0);
        eyeText = eyeLbl;
        eye.onClick.AddListener(TogglePassword);
        // نخلي مكان للزر عشان الكتابة ما تدخل تحته
        RectTransform area = (RectTransform)passField.textViewport;
        area.offsetMin = new Vector2(130, area.offsetMin.y);

        passHint = NaaqaUI.Text(f, "PassHint", "٨ أحرف على الأقل، فيها حرف كبير وصغير ورقم ورمز", 21, new Color(Muted.r, Muted.g, Muted.b, 0.65f), TextAlignmentOptions.Right).gameObject;
        NaaqaUI.Height(passHint.GetComponent<RTLTextMeshPro>(), 30);

        // رمز التحقق
        BuildOtp(card);

        Spacer(card, 6);
        mainButton = NaaqaUI.MakeButton(card, "MainButton", "", NaaqaUI.ButtonStyle.Primary, 32, out mainText);
        NaaqaUI.Height(mainButton, 80);
        mainButton.onClick.AddListener(OnMain);

        status = NaaqaUI.Text(card, "Status", "", 24, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(status, 36);

        if (AllowGuest)
        {
            RTLTextMeshPro gl;
            Button guest = NaaqaUI.MakeButton(card, "Guest", "العب بدون حساب", NaaqaUI.ButtonStyle.Ghost, 23, out gl);
            NaaqaUI.Height(guest, 44);
            gl.color = NaaqaUI.Muted;
            guest.onClick.AddListener(() => Close(false));
            guestButton = guest.gameObject;
        }

        // Enter = الزر الرئيسي
        nameField.onSubmit.AddListener(_ => OnMain());
        emailField.onSubmit.AddListener(_ => OnMain());
        passField.onSubmit.AddListener(_ => OnMain());
        codeField.onSubmit.AddListener(_ => OnMain());
    }

    static Color Muted { get { return NaaqaUI.Muted; } }

    void BuildTabs(Transform parent)
    {
        Image box = NaaqaUI.Img(parent, "Tabs", NaaqaUI.Rounded(), new Color(1, 1, 1, 0.06f), 22f);
        NaaqaUI.Height(box, 70);
        tabs = box.gameObject;

        HorizontalLayoutGroup h = tabs.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(6, 6, 6, 6);
        h.spacing = 6;
        h.reverseArrangement = true;   // من اليمين لليسار
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = true;

        tabLoginBg = Tab(tabs.transform, "TabLogin", "تسجيل الدخول", out tabLoginText, () => SwitchTo(Mode.Login));
        tabSignUpBg = Tab(tabs.transform, "TabSignUp", "حساب جديد", out tabSignUpText, () => SwitchTo(Mode.SignUp));
    }

    Image Tab(Transform parent, string name, string label, out RTLTextMeshPro text, UnityEngine.Events.UnityAction onClick)
    {
        Image bg = NaaqaUI.Img(parent, name, NaaqaUI.RoundedGradient(NaaqaUI.OrangeLight, NaaqaUI.OrangeDark, "btnPrimary"), Color.white, 17f);
        bg.raycastTarget = true;
        Button b = bg.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        text = NaaqaUI.Text(bg.transform, "Label", label, 26, Color.white, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(text.rectTransform, 0, 0, 0, 3);
        return bg;
    }

    void BuildOtp(Transform parent)
    {
        RectTransform g = NaaqaUI.Rect("Otp", parent);
        otpGroup = g.gameObject;
        VerticalLayoutGroup v = otpGroup.AddComponent<VerticalLayoutGroup>();
        v.spacing = 10;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // دائرة فيها رمز البريد
        RectTransform iconRow = NaaqaUI.Rect("IconRow", g);
        NaaqaUI.Height(iconRow, 96);
        Image circle = NaaqaUI.Img(iconRow, "Circle", NaaqaUI.Circle(), new Color(NaaqaUI.Orange.r, NaaqaUI.Orange.g, NaaqaUI.Orange.b, 0.16f));
        NaaqaUI.Center(circle.rectTransform, new Vector2(92, 92), Vector2.zero);
        TextMeshProUGUI at = NaaqaUI.Latin(circle.transform, "At", "@", 52, NaaqaUI.OrangeLight);
        NaaqaUI.Stretch(at.rectTransform, 0, 0, 0, 6);

        RTLTextMeshPro sent = NaaqaUI.Text(g, "Sent", "أرسلنا رمز التحقق إلى بريدك", 26, NaaqaUI.Muted, TextAlignmentOptions.Center);
        NaaqaUI.Height(sent, 38);
        otpEmail = NaaqaUI.Latin(g, "Email", "", 27, NaaqaUI.OrangeLight);
        NaaqaUI.Height(otpEmail, 38);

        codeField = NaaqaUI.Field(g, "CodeField", "اكتب الرمز", false, TMP_InputField.ContentType.IntegerNumber, 92);
        codeField.characterLimit = 8;
        codeField.textComponent.alignment = TextAlignmentOptions.Center;
        codeField.textComponent.fontSize = 46;
        codeField.textComponent.characterSpacing = 28;
        codeField.pointSize = 46;
        ((TMP_Text)codeField.placeholder).alignment = TextAlignmentOptions.Center;

        RTLTextMeshPro spam = NaaqaUI.Text(g, "Spam", "ما وصلك؟ شيك على مجلد Spam", 21, new Color(Muted.r, Muted.g, Muted.b, 0.6f), TextAlignmentOptions.Center);
        NaaqaUI.Height(spam, 30);

        // روابط: إعادة الإرسال + تغيير البريد
        RectTransform row = NaaqaUI.Rect("Links", g);
        NaaqaUI.Height(row, 46);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.reverseArrangement = true;
        h.spacing = 10;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = true;

        resendButton = NaaqaUI.MakeButton(row, "Resend", "إرسال الرمز مرة ثانية", NaaqaUI.ButtonStyle.Ghost, 22, out resendText);
        resendButton.onClick.AddListener(OnResend);
        RTLTextMeshPro backText;
        Button back = NaaqaUI.MakeButton(row, "Back", "تغيير البريد", NaaqaUI.ButtonStyle.Ghost, 22, out backText);
        backText.color = NaaqaUI.Muted;
        back.onClick.AddListener(() => SwitchTo(Mode.SignUp));
    }

    RTLTextMeshPro Label(Transform parent, string text)
    {
        RTLTextMeshPro t = NaaqaUI.Text(parent, "Label", text, 23, new Color(1f, 0.9f, 0.78f, 0.85f), TextAlignmentOptions.BottomRight);
        NaaqaUI.Height(t, 30);
        return t;
    }

    GameObject Spacer(Transform parent, float h)
    {
        RectTransform r = NaaqaUI.Rect("Space", parent);
        NaaqaUI.Height(r, h);
        return r.gameObject;
    }

    static void Ignore(Component c)
    {
        LayoutElement le = c.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    // ===== الأوضاع =====
    void SwitchTo(Mode m)
    {
        if (busy || m == mode) return;
        ApplyMode(m, true);
    }

    void ApplyMode(Mode m, bool animate)
    {
        mode = m;
        bool otp = m == Mode.Otp;
        bool signUp = m == Mode.SignUp;

        tabs.SetActive(!otp);
        form.SetActive(!otp);
        otpGroup.SetActive(otp);

        nameLabel.SetActive(signUp);
        nameField.gameObject.SetActive(signUp);
        Transform gap = form.transform.Find("NameGap");
        if (gap != null) gap.gameObject.SetActive(signUp);
        passHint.SetActive(signUp);

        SetTab(tabLoginBg, tabLoginText, m == Mode.Login);
        SetTab(tabSignUpBg, tabSignUpText, signUp);

        switch (m)
        {
            case Mode.Login:
                title.text = "أهلًا بعودتك";
                subtitle.text = "سجّل دخولك وكمّل من وين وقفت";
                mainText.text = "دخول";
                break;
            case Mode.SignUp:
                title.text = "حساب جديد";
                subtitle.text = "سوّ حسابك عشان ينحفظ تقدمك";
                mainText.text = "إنشاء الحساب";
                break;
            case Mode.Otp:
                title.text = "تأكيد البريد";
                subtitle.text = "خطوة أخيرة وتبدأ رحلتك";
                mainText.text = "تأكيد";
                otpEmail.text = AccountManager.Instance != null ? AccountManager.Instance.UserEmail : "";
                codeField.text = "";
                break;
        }

        SetStatus("", NaaqaUI.Muted);
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);

        if (animate) StartCoroutine(Pop());

        // نحط المؤشر في أول خانة
        TMP_InputField first = otp ? codeField : signUp ? nameField : emailField;
        if (!Application.isMobilePlatform && first != null && first.gameObject.activeInHierarchy) first.ActivateInputField();
    }

    void SetTab(Image bg, RTLTextMeshPro text, bool on)
    {
        bg.color = on ? Color.white : new Color(1, 1, 1, 0);
        text.color = on ? Color.white : NaaqaUI.Muted;
    }

    // ===== الأزرار =====
    void TogglePassword()
    {
        showPass = !showPass;
        passField.contentType = showPass ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        passField.ForceLabelUpdate();
        eyeText.text = showPass ? "إخفاء" : "إظهار";
    }

    void OnMain()
    {
        if (busy) return;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) { SetStatus("صار خطأ، جرب مرة ثانية", NaaqaUI.ErrorColor); return; }

        switch (mode)
        {
            case Mode.Login:
                SetBusy(true, "جاري الدخول");
                StartCoroutine(acc.ApiLogin(emailField.text, passField.text, (err, needsOtp) =>
                {
                    SetBusy(false);
                    if (err == null) { Success(); return; }
                    if (needsOtp)
                    {
                        ApplyMode(Mode.Otp, true);
                        SetStatus(err, NaaqaUI.OkColor);
                        StartCoroutine(acc.ApiResend(_ => { }));
                        resendReadyAt = Time.unscaledTime + 45f;
                        return;
                    }
                    Fail(err);
                }));
                break;

            case Mode.SignUp:
                SetBusy(true, "جاري إنشاء الحساب");
                StartCoroutine(acc.ApiSignUp(nameField.text, emailField.text, passField.text, err =>
                {
                    SetBusy(false);
                    if (err != null) { Fail(err); return; }
                    ApplyMode(Mode.Otp, true);
                    resendReadyAt = Time.unscaledTime + 45f;
                }));
                break;

            case Mode.Otp:
                SetBusy(true, "جاري التأكيد");
                StartCoroutine(acc.ApiVerify(codeField.text, err =>
                {
                    SetBusy(false);
                    if (err != null) { Fail(err); return; }
                    Success();
                }));
                break;
        }
    }

    void OnResend()
    {
        if (busy || Time.unscaledTime < resendReadyAt) return;
        AccountManager acc = AccountManager.Instance;
        if (acc == null) return;

        resendReadyAt = Time.unscaledTime + 45f;
        SetStatus("جاري الإرسال...", NaaqaUI.Muted);
        StartCoroutine(acc.ApiResend(err =>
        {
            if (err == null) SetStatus("أرسلنا رمز جديد على بريدك", NaaqaUI.OkColor);
            else { Fail(err); resendReadyAt = 0f; }
        }));
    }

    void Success()
    {
        AccountManager acc = AccountManager.Instance;
        string name = acc != null ? acc.DisplayName : "";
        SetStatus("هلا " + name + "! جاري تحميل تقدمك...", NaaqaUI.OkColor);
        mainButton.interactable = false;
        StartCoroutine(CloseAfter(0.9f));
    }

    void Fail(string err)
    {
        SetStatus(err, NaaqaUI.ErrorColor);
        StartCoroutine(Shake());
    }

    void SetBusy(bool on, string label = "")
    {
        busy = on;
        busyLabel = label;
        mainButton.interactable = !on;
        if (!on)
        {
            mainText.text = mode == Mode.Login ? "دخول" : mode == Mode.SignUp ? "إنشاء الحساب" : "تأكيد";
        }
        else SetStatus("", NaaqaUI.Muted);
    }

    void SetStatus(string msg, Color c)
    {
        status.color = c;
        status.text = msg;
    }

    // لو الشاشة قصيرة (جوال عريض)، البطاقة تصغر عشان تدخل كاملة
    void LateUpdate()
    {
        if (card == null || canvasRect == null) return;
        float h = card.rect.height;
        float fit = h > 0f ? Mathf.Min(1f, (canvasRect.rect.height - 50f) / h) : 1f;
        card.localScale = Vector3.one * fit * animScale;
    }

    void Update()
    {
        // نقاط متحركة وقت الانتظار
        if (busy)
        {
            int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
            mainText.text = busyLabel + new string('.', dots);
        }

        // عداد إعادة الإرسال
        if (mode == Mode.Otp && resendText != null)
        {
            float left = resendReadyAt - Time.unscaledTime;
            bool ready = left <= 0f;
            resendButton.interactable = ready;
            resendText.text = ready ? "إرسال الرمز مرة ثانية" : "إرسال الرمز (" + NaaqaUI.Ar(Mathf.CeilToInt(left)) + ")";
            resendText.color = ready ? NaaqaUI.OrangeLight : new Color(Muted.r, Muted.g, Muted.b, 0.5f);
        }
    }

    // ===== حركات =====
    void Open()
    {
        if (video != null) MenuVideo.Attach(video);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        rootGroup.alpha = 0f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.45f;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            rootGroup.alpha = k;
            card.anchoredPosition = cardPos + new Vector2(0f, Mathf.Lerp(-40f, 0f, k));
            yield return null;
        }
        card.anchoredPosition = cardPos;
    }

    IEnumerator CloseAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Close(true);
    }

    void Close(bool loggedIn)
    {
        StartCoroutine(FadeOutAndHide());
    }

    IEnumerator FadeOutAndHide()
    {
        rootGroup.interactable = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.4f;
            rootGroup.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }
        if (video != null) MenuVideo.Detach(video);
        Destroy(gameObject);
    }

    IEnumerator Pop()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            float k = Mathf.Clamp01(t);
            animScale = 0.985f + 0.015f * (1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        animScale = 1f;
    }

    IEnumerator Shake()
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float a = (1f - t / 0.35f) * 14f;
            card.anchoredPosition = cardPos + new Vector2(Mathf.Sin(t * 60f) * a, 0f);
            yield return null;
        }
        card.anchoredPosition = cardPos;
    }
}
