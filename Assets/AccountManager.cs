using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using RTLTMPro;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance;

    [Header("Supabase")]
    public string supabaseUrl = "https://yalgofpqvezrlqrcbqgz.supabase.co";
    public string supabaseKey = "sb_publishable_mDm-JRJOXANoUnvrATAe7g_50yjZ_nh";

    [Header("النافذة")]
    public GameObject accountWindow;
    public GameObject dimBackground;

    [Header("الخانات")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField otpInput;

    [Header("الأزرار والنص")]
    public Button mainButton;
    public Button switchButton;
    public RTLTextMeshPro statusText;

    [Header("زر العين (إظهار كلمة السر)")]
    public Button eyeButton;
    public Image eyeIcon;
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    [Header("الألوان")]
    public Color errorColor = new Color(0.8f, 0.15f, 0.1f);
    public Color okColor = new Color(0.15f, 0.55f, 0.2f);
    public Color infoColor = new Color(0.35f, 0.22f, 0.1f);

    public bool IsLoggedIn { get { return !string.IsNullOrEmpty(AccessToken); } }
    public string AccessToken { get; private set; }
    public string UserId { get; private set; }
    public string UserEmail { get; private set; }
    public string DisplayName { get; private set; }

    public static System.Action OnLoggedIn;
    public static System.Action OnLoggedOut;

    enum Mode { SignUp, Login, Otp, Profile }
    private Mode mode = Mode.SignUp;
    private bool busy = false;
    private bool showPassword = false;

    void Awake()
    {
        Instance = this;
        LoadSession();
    }

    void Start()
    {
        if (mainButton != null) mainButton.onClick.AddListener(OnMain);
        if (switchButton != null) switchButton.onClick.AddListener(OnSwitch);
        if (eyeButton != null) eyeButton.onClick.AddListener(TogglePassword);
        ApplyPasswordVisibility();
    }

    // ===== فتح النافذة (الخيمة تنادي هذي) =====
    public void Open()
    {
        if (accountWindow != null) accountWindow.SetActive(true);
        if (dimBackground != null) dimBackground.SetActive(true);
        showPassword = false;
        ApplyPasswordVisibility();
        SetMode(IsLoggedIn ? Mode.Profile : Mode.SignUp);
    }

    public void Close()
    {
        if (accountWindow != null) accountWindow.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
    }

    void SetMode(Mode m)
    {
        mode = m;
        Show(nameInput, m == Mode.SignUp);
        Show(emailInput, m == Mode.SignUp || m == Mode.Login);
        Show(passwordInput, m == Mode.SignUp || m == Mode.Login);
        Show(otpInput, m == Mode.Otp);
        if (eyeButton != null) eyeButton.gameObject.SetActive(m == Mode.SignUp || m == Mode.Login);

        switch (m)
        {
            case Mode.SignUp:
                SetButton(mainButton, "إنشاء حساب");
                SetButton(switchButton, "عندك حساب؟ سجل دخول");
                Info("");
                break;
            case Mode.Login:
                SetButton(mainButton, "دخول");
                SetButton(switchButton, "ما عندك حساب؟ سجل جديد");
                Info("");
                break;
            case Mode.Otp:
                SetButton(mainButton, "تأكيد");
                SetButton(switchButton, "أرسل الرمز مرة ثانية");
                Ok("أرسلنا رمز التحقق على إيميلك (شيك على Spam بعد)");
                break;
            case Mode.Profile:
                SetButton(mainButton, "تمام");
                SetButton(switchButton, "تسجيل خروج");
                Info("هلا " + DisplayName + "!\n" + UserEmail);
                break;
        }
    }

    void OnMain()
    {
        if (busy) return;
        switch (mode)
        {
            case Mode.SignUp: StartCoroutine(SignUp()); break;
            case Mode.Login: StartCoroutine(Login()); break;
            case Mode.Otp: StartCoroutine(VerifyOtp()); break;
            case Mode.Profile: Close(); break;
        }
    }

    void OnSwitch()
    {
        if (busy) return;
        switch (mode)
        {
            case Mode.SignUp: SetMode(Mode.Login); break;
            case Mode.Login: SetMode(Mode.SignUp); break;
            case Mode.Otp: StartCoroutine(ResendOtp()); break;
            case Mode.Profile: Logout(); break;
        }
    }

    // ===== زر العين =====
    void TogglePassword()
    {
        showPassword = !showPassword;
        ApplyPasswordVisibility();
    }

    void ApplyPasswordVisibility()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = showPassword
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        if (eyeIcon != null && eyeOpenSprite != null && eyeClosedSprite != null)
            eyeIcon.sprite = showPassword ? eyeOpenSprite : eyeClosedSprite;
        else
            SetButton(eyeButton, showPassword ? "إخفاء" : "إظهار");
    }

    // ===== التحقق =====
    string ValidateName(string name)
    {
        if (name == "") return "اكتب اسمك";
        if (name.Length < 2) return "الاسم قصير، لازم حرفين على الأقل";
        if (name.Length > 20) return "الاسم طويل، الحد ٢٠ حرف";
        if (!Regex.IsMatch(name, @"^[\p{L}\p{M} ]+$")) return "الاسم حروف بس، بدون أرقام أو رموز";
        return null;
    }

    string ValidateEmail(string email)
    {
        if (email == "") return "اكتب إيميلك";
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$")) return "الإيميل غلط، مثال: name@gmail.com";
        return null;
    }

    string ValidatePassword(string pass)
    {
        if (pass == "") return "اكتب كلمة السر";
        if (pass.Length < 8) return "كلمة السر لازم ٨ أحرف على الأقل";
        if (!Regex.IsMatch(pass, "[A-Z]")) return "كلمة السر لازم فيها حرف كبير (A-Z)";
        if (!Regex.IsMatch(pass, "[a-z]")) return "كلمة السر لازم فيها حرف صغير (a-z)";
        if (!Regex.IsMatch(pass, "[0-9]")) return "كلمة السر لازم فيها رقم";
        if (!Regex.IsMatch(pass, @"[^A-Za-z0-9]")) return "كلمة السر لازم فيها رمز مثل ! @ # $";
        return null;
    }

    // ===== تسجيل جديد =====
    IEnumerator SignUp()
    {
        string name = Val(nameInput), email = Val(emailInput), pass = RawVal(passwordInput);

        string err = ValidateName(name);
        if (err == null) err = ValidateEmail(email);
        if (err == null) err = ValidatePassword(pass);
        if (err != null) { Error(err); yield break; }

        SignUpBody body = new SignUpBody();
        body.email = email;
        body.password = pass;
        body.data = new UserMeta { display_name = name };

        busy = true;
        Info("جاري التسجيل...");
        UnityWebRequest req = Post("/auth/v1/signup", JsonUtility.ToJson(body), null);
        yield return req.SendWebRequest();
        busy = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Error(ErrorToArabic(req.downloadHandler.text));
            yield break;
        }

        DisplayName = name;
        UserEmail = email;
        SetMode(Mode.Otp);
    }

    // ===== تأكيد الرمز =====
    IEnumerator VerifyOtp()
    {
        string code = Val(otpInput);
        if (code == "") { Error("اكتب الرمز اللي وصلك"); yield break; }
        if (!Regex.IsMatch(code, @"^[0-9]{6,8}$")) { Error("الرمز أرقام بس"); yield break; }

        VerifyBody body = new VerifyBody { type = "signup", email = UserEmail, token = code };

        busy = true;
        Info("جاري التأكيد...");
        UnityWebRequest req = Post("/auth/v1/verify", JsonUtility.ToJson(body), null);
        yield return req.SendWebRequest();
        busy = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Error(ErrorToArabic(req.downloadHandler.text));
            yield break;
        }

        SessionResponse s = JsonUtility.FromJson<SessionResponse>(req.downloadHandler.text);
        UseSession(s);
        yield return CreatePlayerRow();

        SetMode(Mode.Profile);
        Ok("تم إنشاء حسابك! هلا بك");
    }

    IEnumerator ResendOtp()
    {
        ResendBody body = new ResendBody { type = "signup", email = UserEmail };

        busy = true;
        Info("جاري الإرسال...");
        UnityWebRequest req = Post("/auth/v1/resend", JsonUtility.ToJson(body), null);
        yield return req.SendWebRequest();
        busy = false;

        if (req.result != UnityWebRequest.Result.Success)
            Error(ErrorToArabic(req.downloadHandler.text));
        else
            Ok("أرسلنا رمز جديد على إيميلك");
    }

    // ===== دخول =====
    IEnumerator Login()
    {
        string email = Val(emailInput), pass = RawVal(passwordInput);

        string err = ValidateEmail(email);
        if (err == null && pass == "") err = "اكتب كلمة السر";
        if (err != null) { Error(err); yield break; }

        LoginBody body = new LoginBody { email = email, password = pass };

        busy = true;
        Info("جاري الدخول...");
        UnityWebRequest req = Post("/auth/v1/token?grant_type=password", JsonUtility.ToJson(body), null);
        yield return req.SendWebRequest();
        busy = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            string raw = req.downloadHandler.text;
            if (raw.Contains("not confirmed") || raw.Contains("email_not_confirmed"))
            {
                UserEmail = email;
                SetMode(Mode.Otp);
                Error("لازم تأكد إيميلك أول. اضغط أرسل الرمز مرة ثانية");
                yield break;
            }
            Error(ErrorToArabic(raw));
            yield break;
        }

        SessionResponse s = JsonUtility.FromJson<SessionResponse>(req.downloadHandler.text);
        UseSession(s);
        SetMode(Mode.Profile);
    }

    void Logout()
    {
        AccessToken = null;
        UserId = null;
        UserEmail = null;
        DisplayName = null;

        PlayerPrefs.DeleteKey("naaqa_token");
        PlayerPrefs.DeleteKey("naaqa_refresh");
        PlayerPrefs.DeleteKey("naaqa_uid");
        PlayerPrefs.DeleteKey("naaqa_email");
        PlayerPrefs.DeleteKey("naaqa_name");
        PlayerPrefs.Save();

        if (OnLoggedOut != null) OnLoggedOut();
        SetMode(Mode.Login);
        Ok("تم تسجيل الخروج");
    }

    IEnumerator CreatePlayerRow()
    {
        PlayerRow row = new PlayerRow { id = UserId, display_name = DisplayName };
        UnityWebRequest req = Post("/rest/v1/players", JsonUtility.ToJson(row), AccessToken);
        req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("ما قدرت أسوي صف اللاعب: " + req.downloadHandler.text);
    }

    void UseSession(SessionResponse s)
    {
        if (s == null || s.user == null) return;

        AccessToken = s.access_token;
        UserId = s.user.id;
        UserEmail = s.user.email;
        if (s.user.user_metadata != null && !string.IsNullOrEmpty(s.user.user_metadata.display_name))
            DisplayName = s.user.user_metadata.display_name;

        PlayerPrefs.SetString("naaqa_token", AccessToken);
        PlayerPrefs.SetString("naaqa_refresh", s.refresh_token);
        PlayerPrefs.SetString("naaqa_uid", UserId);
        PlayerPrefs.SetString("naaqa_email", UserEmail);
        PlayerPrefs.SetString("naaqa_name", DisplayName ?? "");
        PlayerPrefs.Save();

        if (OnLoggedIn != null) OnLoggedIn();
    }

    void LoadSession()
    {
        AccessToken = PlayerPrefs.GetString("naaqa_token", "");
        UserId = PlayerPrefs.GetString("naaqa_uid", "");
        UserEmail = PlayerPrefs.GetString("naaqa_email", "");
        DisplayName = PlayerPrefs.GetString("naaqa_name", "");
        if (AccessToken == "") AccessToken = null;
    }

    // ===== أدوات =====
    UnityWebRequest Post(string path, string json, string token)
    {
        UnityWebRequest req = new UnityWebRequest(supabaseUrl + path, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", supabaseKey);
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token);
        return req;
    }

    string ErrorToArabic(string raw)
    {
        Debug.Log("Supabase: " + raw);
        if (string.IsNullOrEmpty(raw)) return "ما فيه اتصال بالإنترنت";
        if (raw.Contains("Invalid login credentials")) return "الإيميل أو كلمة السر غلط";
        if (raw.Contains("already registered") || raw.Contains("user_already_exists")) return "الإيميل مسجل من قبل، سجل دخول";
        if (raw.Contains("expired") || (raw.Contains("invalid") && raw.Contains("oken"))) return "الرمز غلط أو انتهى";
        if (raw.Contains("weak_password") || raw.Contains("Password should")) return "كلمة السر ضعيفة";
        if (raw.Contains("rate_limit") || raw.Contains("rate limit")) return "انتظر شوي وجرب مرة ثانية";
        if (raw.Contains("email_address_invalid") || raw.Contains("invalid format")) return "الإيميل غلط";
        return "صار خطأ، جرب مرة ثانية";
    }

    string Val(TMP_InputField f) { return f == null ? "" : f.text.Trim(); }
    string RawVal(TMP_InputField f) { return f == null ? "" : f.text; }

    void Show(TMP_InputField f, bool on)
    {
        if (f == null) return;
        f.gameObject.SetActive(on);
        if (on && f == otpInput) f.text = "";
    }

    void SetButton(Button b, string text)
    {
        if (b == null) return;
        RTLTextMeshPro t = b.GetComponentInChildren<RTLTextMeshPro>(true);
        if (t == null) return;
        t.text = text;
        t.ForceMeshUpdate();
    }

    // ===== رسالة الحالة (بدون إشعارات) =====
    void Info(string msg) { SetStatus(msg, infoColor); }
    void Ok(string msg) { SetStatus(msg, okColor); }
    void Error(string msg) { SetStatus(msg, errorColor); }

    void SetStatus(string msg, Color c)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        statusText.color = c;
        statusText.text = msg;
        statusText.ForceMeshUpdate();
    }

    [System.Serializable] class UserMeta { public string display_name; }
    [System.Serializable] class SignUpBody { public string email; public string password; public UserMeta data; }
    [System.Serializable] class VerifyBody { public string type; public string email; public string token; }
    [System.Serializable] class ResendBody { public string type; public string email; }
    [System.Serializable] class LoginBody { public string email; public string password; }
    [System.Serializable] class UserInfo { public string id; public string email; public UserMeta user_metadata; }
    [System.Serializable] class SessionResponse { public string access_token; public string refresh_token; public UserInfo user; }
    [System.Serializable] class PlayerRow { public string id; public string display_name; }
}