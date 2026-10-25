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
    }

    public void Show(string message)
    {
        Spawn(message, false);
    }

    public void ShowWarning(string message)
    {
        Spawn(message, true);
    }

    void Spawn(string message, bool warning)
    {
        if (template == null)
        {
            Debug.LogError("Template غير مربوط في NotificationManager!", this);
            return;
        }

        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("NotificationPanel كان مطفي، شغلته", this);
            gameObject.SetActive(true);
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("NotificationPanel داخل كائن مطفي، ما أقدر أعرض الإشعار", this);
            return;
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

        StartCoroutine(Life(item, group, content as RectTransform, warning));
    }

    IEnumerator Life(GameObject item, CanvasGroup group, RectTransform content, bool warning)
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

        float wait = showTime - fadeTime * 2f - (warning ? shakeDuration : 0f);
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