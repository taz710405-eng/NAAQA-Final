using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;

// ===== ناس السوق والميدان =====
// شخصية صغيرة تمشي داخل منطقة، وكل شوي تقول كلمة نجدية في فقاعة.
public class NpcWalker : MonoBehaviour
{
    float bobPhase;
    public enum Place { Souq, Race }

    public RectTransform area;          // المنطقة اللي تمشي فيها (داخل الخلفية)
    public Rect walk = new Rect(0.15f, 0.2f, 0.7f, 0.5f);   // جزء من المنطقة (٠ إلى ١)
    public Place place = Place.Souq;
    public float speed = 60f;
    public bool standStill = false;     // للبياع أو المتفرجين
    public bool chatty = true;          // يقول كلام عشوائي
    public float bubbleScale = 1f;      // حجم فقاعة الكلام (الجمهور البعيد أصغر)
    System.Action onArrive;             // للزبون: لما يوصل البسطة
    bool scripted = false;
    bool faced = false;                 // اتجاهه محدد (ما نقلبه عشوائي)

    static int talking = 0;             // ما نخلي أكثر من ٢ يتكلمون مع بعض

    RectTransform rt;
    Vector2 target;
    float wait = 0f;
    float talkIn;
    float talkLeft = 0f;
    GameObject bubble;
    RTLTextMeshPro bubbleText;
    Vector3 baseScale;

    static readonly string[] SouqLines =
    {
        "حيّ الله من جانا", "يا هلا ومرحبا", "تفضّل على القهوة", "التمر هذا خلاص، ذوقه",
        "أبشر بالخير", "وش تبي يا الغالي؟", "الله يحييك", "هذا الحليب طازج اليوم",
        "علومك؟ عساك طيب", "حياك الله يا ولد الحلال", "بكم هذا يا خوي؟", "شف هالدلّة ما أزينها",
        "لا تروح بدري، القهوة جاية", "يا زين السوق اليوم",
    };

    static readonly string[] RaceLines =
    {
        "يا حلوها!", "دزّها!", "ما شاء الله!", "يلا يلا!", "قربت!", "يا سلام!", "ما تنلحق!", "عز الله!",
    };

    public static string RandomLine(Place p)
    {
        string[] l = p == Place.Race ? RaceLines : SouqLines;
        return l[Random.Range(0, l.Length)];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { talking = 0; }

    void Start()
    {
        bobPhase = UnityEngine.Random.Range(0f, 100f);
        rt = (RectTransform)transform;
        baseScale = rt.localScale;
        talkIn = Random.Range(2f, 9f);
        // الصور كلها تطالع يسار، فنخلي نصهم يطالعون يمين
        if (!faced && Random.value < 0.5f) rt.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        if (!scripted) PickTarget();
        BuildBubble();
    }

    void OnDisable()
    {
        if (talkLeft > 0f) { talking = Mathf.Max(0, talking - 1); talkLeft = 0f; }
        if (bubble != null) bubble.SetActive(false);
    }

    void BuildBubble()
    {
        Image b = NaaqaUI.SpeechBubble(transform, "Bubble", 23, out bubbleText);
        RectTransform brt = b.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(250, 56);
        brt.anchoredPosition = new Vector2(0, 6);
        bubble = b.gameObject;
        bubble.SetActive(false);
    }

    void PickTarget()
    {
        if (area == null) return;
        Rect r = area.rect;
        float x = r.xMin + r.width * (walk.x + Random.value * walk.width);
        float y = r.yMin + r.height * (walk.y + Random.value * walk.height);
        target = new Vector2(x, y);
    }

    void Update()
    {
        if (rt == null) return;
        float dt = Time.unscaledDeltaTime;

        // المشي
        if (!standStill && area != null)
        {
            if (wait > 0f) wait -= dt;
            else
            {
                Vector2 p = rt.anchoredPosition;
                Vector2 d = target - p;
                if (d.magnitude < 6f)
                {
                    if (scripted)
                    {
                        scripted = false;
                        standStill = true;
                        System.Action a = onArrive;
                        onArrive = null;
                        if (a != null) a();
                    }
                    else { wait = Random.Range(0.5f, 3f); PickTarget(); }
                }
                else
                {
                    Vector2 step = Vector2.ClampMagnitude(d.normalized * speed * dt, d.magnitude);
                    rt.anchoredPosition = p + step;
                    // الصورة تطالع يسار: لو يمشي يمين نقلبها
                    float dir = -Mathf.Sign(step.x);
                    if (Mathf.Abs(step.x) > 0.01f) rt.localScale = new Vector3(Mathf.Abs(baseScale.x) * dir, baseScale.y, baseScale.z);
                }
            }
        }

        // يتمايل وهو يمشي
        bool moving = !standStill && wait <= 0f;
        float bob = moving ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f + bobPhase)) * 5f : 0f;
        if (transform.childCount > 0)
        {
            Transform body = transform.GetChild(0);
            body.localPosition = new Vector3(0f, bob, 0f);
        }

        // الفقاعة ما تنقلب مع الشخصية
        if (bubble != null)
            bubble.transform.localScale = new Vector3(Mathf.Sign(rt.localScale.x) * bubbleScale, bubbleScale, 1f);

        // الكلام
        if (talkLeft > 0f)
        {
            talkLeft -= dt;
            if (talkLeft <= 0f) { bubble.SetActive(false); talking = Mathf.Max(0, talking - 1); talkIn = Random.Range(6f, 15f); }
        }
        else
        {
            talkIn -= dt;
            if (talkIn <= 0f && chatty)
            {
                if (talking < 2) Say(RandomLine(place), 3f);
                else talkIn = Random.Range(2f, 5f);
            }
        }
    }

    public void Say(string line, float seconds)
    {
        if (bubble == null) return;
        if (talkLeft <= 0f) talking++;
        bubble.SetActive(true);
        NaaqaUI.FitBubble((RectTransform)bubble.transform, bubbleText, line, 380f);
        bubble.transform.SetAsLastSibling();
        talkLeft = seconds;
    }

    // يمشي لمكان معيّن (الزبون للبسطة، أو يطلع من السوق)
    public void GoTo(Vector2 pos, System.Action arrive)
    {
        target = pos;
        onArrive = arrive;
        scripted = true;
        standStill = false;
        wait = 0f;
    }

    // يطالع جهة معيّنة (١ = يمين، -١ = يسار)
    public void Face(float dir)
    {
        if (rt == null) rt = (RectTransform)transform;
        if (baseScale == Vector3.zero) baseScale = rt.localScale;
        faced = true;
        rt.localScale = new Vector3(-Mathf.Abs(baseScale.x) * Mathf.Sign(dir), baseScale.y, baseScale.z);
    }

    public void Hush()
    {
        if (bubble != null) bubble.SetActive(false);
        if (talkLeft > 0f) { talking = Mathf.Max(0, talking - 1); talkLeft = 0f; }
    }

    // ===== صنع شخصية =====
    public static NpcWalker Spawn(RectTransform parent, string sprite, Vector2 pos, float height, Place place, Rect walk, bool still = false)
    {
        RectTransform root = NaaqaUI.Rect("Npc_" + sprite, parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        Sprite s = NaaqaUI.Load(sprite);
        float aspect = s != null ? s.rect.width / s.rect.height : 0.5f;
        root.sizeDelta = new Vector2(height * aspect, height);
        root.anchoredPosition = pos;

        Image body = NaaqaUI.Img(root, "Body", s, Color.white);
        NaaqaUI.Stretch(body.rectTransform);
        body.preserveAspect = true;

        NpcWalker w = root.gameObject.AddComponent<NpcWalker>();
        w.area = parent;
        w.walk = walk;
        w.place = place;
        w.standStill = still;
        w.speed = Random.Range(40f, 75f);
        return w;
    }
}
