using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Setup")]
    public GameObject template;

    [Header("Settings")]
    public float showTime = 3f;
    public float fadeTime = 0.3f;
    public int maxCount = 4;

    [Header("Warning Shake")]
    public float shakeDuration = 0.5f;
    public float shakeStrength = 12f;
    public float shakeSpeed = 50f;

    private readonly List<GameObject> active = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (template != null) template.SetActive(false);
        // نفس ستايل اللعبة: خلفية غامقة شفافة بدل الخشب
        if (template != null)
        {
            Transform content = template.transform.Find("Content");
            if (content != null) NaaqaUI.DarkPill(content.GetComponent<UnityEngine.UI.Image>(), 26f);
            Transform msg = template.transform.Find("Content/Message");
            TMP_Text mt = msg != null ? msg.GetComponent<TMP_Text>() : null;
            if (mt != null) { mt.color = NaaqaUI.Cream; NaaqaUI.BigText(mt, 90f, 19f, 25f); }
            // الإشعار كان صغير مره: نعرّض اللوحة ونطوّل الشريط
            RectTransform panel = template.transform.parent as RectTransform;
            if (panel != null) panel.sizeDelta = new Vector2(540f, panel.sizeDelta.y);
            // تحت شريط المهمة (لا يركبون فوق بعض)
            // تحت المستوى على طول (فوق، يمين)، ما يغطي الأزرار
            if (panel != null) panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -128f);
            RectTransform it = template.transform as RectTransform;
            if (it != null) it.sizeDelta = new Vector2(it.sizeDelta.x, 66f);
        }
    }

    // ===== طابور: إشعار واحد بس يطلع، واللي بعده ينتظر لين يخلص =====
    class Pending { public string msg; public bool warning; }
    private readonly Queue<Pending> queue = new Queue<Pending>();
    private bool busy = false;
    public bool Busy { get { return busy || queue.Count > 0; } }

    // إشعارات ما لها داعي (العداد والشريط فوق يوضحونها)
    static readonly string[] NoiseParts =
    {
        "الحليب جاهز", "أكلت", "تنتج الحين", "حصدت",
        "بدأت رحلتك", "كفو! صار عندك", "جاتك مزرعة علف هدية", "جاك بير هدية"
    };
    static bool Noise(string m)
    {
        if (m.StartsWith("+")) return true;   // «+١ حليب» وأمثالها
        foreach (string n in NoiseParts) if (m.Contains(n)) return true;
        return false;
    }

    public void Show(string message)
    {
        Enqueue(message, false);
    }

    public void ShowWarning(string message)
    {
        Enqueue(message, true);
    }

    void Enqueue(string message, bool warning)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (Noise(message)) return;
        // نفس الرسالة تنتظر من قبل؟ ما نكررها
        foreach (Pending p in queue) if (p.msg == message) return;
        if (queue.Count >= 3) queue.Dequeue();   // ما نخلي الطابور يطول
        queue.Enqueue(new Pending { msg = message, warning = warning });

        // الرسائل المهمة تروح للبريد (التحذيرات والرسائل القصيرة لا)
        if (!warning && message.Length > 14 && !message.StartsWith("+") && !message.StartsWith("تم ") && !message.StartsWith("رجع"))
            MailBox.Add(message);

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!busy && gameObject.activeInHierarchy) StartCoroutine(Pump());
    }

    // فيه إشعار ظاهر الحين؟
    public bool Showing
    {
        get { active.RemoveAll(x => x == null); return active.Count > 0; }
    }

    void Update()
    {
        if (!busy && queue.Count > 0 && gameObject.activeInHierarchy) StartCoroutine(Pump());
    }

    IEnumerator Pump()
    {
        busy = true;
        while (queue.Count > 0)
        {
            // لو فيه رسالة كبيرة بالنص (ToastView) ننتظرها تخلص
            while (ToastView.Instance != null && ToastView.Instance.panel != null && ToastView.Instance.panel.activeInHierarchy)
                yield return null;

            Pending p = queue.Dequeue();
            GameObject item = Spawn(p.msg, p.warning);
            float time = queue.Count > 0 ? Mathf.Min(showTime, 2.2f) : showTime;
            float t = 0f;
            while (item != null && t < time + 0.1f) { t += Time.unscaledDeltaTime; yield return null; }
        }
        busy = false;
    }

    void OnDisable() { busy = false; }

    GameObject Spawn(string message, bool warning)
    {
        if (template == null)
        {
            Debug.LogError("Template غير مربوط في NotificationManager!", this);
            return null;
        }

        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("NotificationPanel كان مطفي، شغلته", this);
            gameObject.SetActive(true);
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("NotificationPanel داخل كائن مطفي، ما أقدر أعرض الإشعار", this);
            return null;
        }

        active.RemoveAll(x => x == null);
        while (active.Count >= maxCount)
        {
            if (active[0] != null) Destroy(active[0]);
            active.RemoveAt(0);
        }

        GameObject item = Instantiate(template, template.transform.parent);
        item.SetActive(true);
        active.Add(item);

        Transform content = item.transform.Find("Content");
        Transform messageT = item.transform.Find("Content/Message");
        Transform iconT = item.transform.Find("Content/WarningIcon");

        if (messageT != null)
        {
            TMP_Text text = messageT.GetComponent<TMP_Text>();
            if (text != null) text.text = message;
        }
        else
        {
            Debug.LogError("ما لقيت Content/Message داخل القالب!", this);
        }

        if (iconT != null) iconT.gameObject.SetActive(warning);

        CanvasGroup group = item.GetComponent<CanvasGroup>();
        if (group == null) group = item.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        StartCoroutine(Life(item, group, content as RectTransform, warning, queue.Count > 0 ? Mathf.Min(showTime, 2.2f) : showTime));
        return item;
    }

    IEnumerator Life(GameObject item, CanvasGroup group, RectTransform content, bool warning, float life)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            if (item == null) yield break;
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        if (item == null) yield break;
        group.alpha = 1f;

        if (warning && content != null)
        {
            Vector2 start = content.anchoredPosition;
            t = 0f;
            while (t < shakeDuration)
            {
                if (item == null) yield break;
                t += Time.unscaledDeltaTime;
                float fade = 1f - (t / shakeDuration);
                content.anchoredPosition = start + new Vector2(Mathf.Sin(t * shakeSpeed) * shakeStrength * fade, 0f);
                yield return null;
            }
            if (content != null) content.anchoredPosition = start;
        }

        float wait = life - fadeTime * 2f - (warning ? shakeDuration : 0f);
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        t = 0f;
        while (t < fadeTime)
        {
            if (item == null) yield break;
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeTime);
            yield return null;
        }

        active.Remove(item);
        if (item != null) Destroy(item);
    }

    [ContextMenu("Test Normal")]
    void TestNormal()
    {
        Show("أكلت الناقة، بدأ الإنتاج");
    }

    [ContextMenu("Test Warning")]
    void TestWarning()
    {
        ShowWarning("ما فيه علف!");
    }
}