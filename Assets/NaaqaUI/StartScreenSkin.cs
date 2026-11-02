using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== شكل شاشة البداية =====
// يركّب نفسه لحاله على StartScreen: فيديو الصحراء خلفية، واللوقو في النص، وتحته زر ابدأ.
// وبعد «ابدأ» يفتح شاشة تسجيل الدخول (لو اللاعب مو مسجل دخول).
public class StartScreenSkin : MonoBehaviour
{
    public string tagline = "من تراثنا تبدأ الحكاية";

    private RawImage video;
    private RectTransform logo;
    private CanvasGroup logoGroup;
    private RTLTextMeshPro taglineText;
    private RectTransform startButton;
    private Vector2 logoPos;
    private bool built = false;

    // ===== تركيب تلقائي =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        AttachInScene();
        SceneManager.sceneLoaded += (s, m) => AttachInScene();
    }

    static void AttachInScene()
    {
        GameObject screen = null;
        if (UIManager.Instance != null) screen = UIManager.Instance.startScreen;
        if (screen == null)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "StartScreen" && t.GetComponentInParent<Canvas>(true) != null && t.gameObject.activeSelf)
                { screen = t.gameObject; break; }
        }
        if (screen == null || screen.GetComponent<StartScreenSkin>() != null) return;
        screen.AddComponent<StartScreenSkin>();
    }

    void Awake()
    {
        Build();
    }

    void Build()
    {
        if (built) return;
        built = true;

        // الخلفية القديمة تختفي (وتبقى تمسك الضغطات عشان ما توصل للعبة)
        Image old = GetComponent<Image>();
        if (old != null) old.color = new Color(1, 1, 1, 0);

        // ١. الفيديو
        RectTransform vrt = NaaqaUI.Rect("VideoBG", transform);
        NaaqaUI.Stretch(vrt);
        vrt.SetAsFirstSibling();
        video = vrt.gameObject.AddComponent<RawImage>();
        video.raycastTarget = false;

        // ٢. تظليل خفيف تحت عشان الزر يبان
        Image shade = NaaqaUI.Img(transform, "Shade",
            NaaqaUI.VerticalFade(new Color(0, 0, 0, 0.05f), new Color(0.08f, 0.03f, 0f, 0.55f), "startShade"),
            Color.white);
        NaaqaUI.Stretch(shade.rectTransform);
        shade.transform.SetSiblingIndex(1);

        // ٣. اللوقو
        Image logoImg = NaaqaUI.Img(transform, "NaaqaLogo", NaaqaUI.Load("logo"), Color.white);
        logoImg.preserveAspect = true;
        logo = logoImg.rectTransform;
        NaaqaUI.Center(logo, new Vector2(1040, 472), new Vector2(0, 150));
        logoPos = logo.anchoredPosition;
        logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;

        // ٤. الجملة تحت اللوقو
        taglineText = NaaqaUI.Text(transform, "Tagline", tagline, 44, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Center(taglineText.rectTransform, new Vector2(1000, 70), new Vector2(0, -115));
        Shadow sh = taglineText.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0.15f, 0.06f, 0f, 0.7f);
        sh.effectDistance = new Vector2(2, -3);

        // ٥. زر ابدأ: تحت الجملة، وفوق كل شي
        Transform btn = transform.Find("StartButton");
        if (btn != null)
        {
            startButton = (RectTransform)btn;
            startButton.anchorMin = startButton.anchorMax = new Vector2(0.5f, 0.5f);
            startButton.anchoredPosition = new Vector2(0, -275);
            startButton.SetAsLastSibling();
            // شكل غامق شفاف بحد ذهبي خفيف (بدل الخشب)
            startButton.sizeDelta = new Vector2(440, 104);
            Image bi = btn.GetComponent<Image>();
            if (bi != null) { bi.preserveAspect = false; NaaqaUI.DarkPill(bi, 40f); }
            foreach (Image ci in btn.GetComponentsInChildren<Image>(true))
                if (ci != bi && ci.name != "PillLine") ci.enabled = false;
            foreach (TMPro.TMP_Text tt in btn.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                tt.color = NaaqaUI.Cream;
                tt.fontSize = 44;
                tt.fontStyle = TMPro.FontStyles.Bold;
                tt.enableAutoSizing = false;
                tt.rectTransform.anchorMin = Vector2.zero; tt.rectTransform.anchorMax = Vector2.one;
                tt.rectTransform.offsetMin = Vector2.zero; tt.rectTransform.offsetMax = new Vector2(0, -4);
                tt.alignment = TMPro.TextAlignmentOptions.Center;
            }
            if (btn.GetComponent<NaaqaButtonFx>() == null) btn.gameObject.AddComponent<NaaqaButtonFx>();

            Button b = btn.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(OnStartPressed);
        }
    }

    void OnEnable()
    {
        if (video != null) MenuVideo.Attach(video);
        StartCoroutine(Intro());
    }

    void OnDisable()
    {
        if (video != null) MenuVideo.Detach(video);
    }

    IEnumerator Intro()
    {
        if (logoGroup == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.9f;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            logoGroup.alpha = k;
            logo.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
            if (taglineText != null) taglineText.alpha = Mathf.Clamp01(t * 1.4f - 0.4f);
            yield return null;
        }
    }

    void Update()
    {
        // اللوقو يطفو بهدوء
        if (logo != null)
            logo.anchoredPosition = logoPos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 1.1f) * 8f);
    }

    void OnStartPressed()
    {
        LoginScreen.ShowIfNeeded();
    }
}
