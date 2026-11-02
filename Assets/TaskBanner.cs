using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TaskBanner : MonoBehaviour
{
    public static TaskBanner Instance;

    [Header("UI")]
    public TMP_Text taskText;
    public GameObject checkIcon;

    [Header("Tasks (same order as TaskManager)")]
    public string[] taskNames =
    {
        "اضغط على الإبل",
        "أطعم الإبل",
        "اجمع الحليب",
        "سلّم الطلب",
        "طوّر العزبة"
    };

    [Header("Animation")]
    public float celebrateTime = 1.8f;
    public float bounceHeight = 25f;
    public float fadeOutTime = 0.6f;

    private bool[] done;
    private readonly Queue<int[]> queue = new Queue<int[]>();
    private bool busy = false;
    private CanvasGroup group;
    private Vector2 checkStartPos;

    void Awake()
    {
        Instance = this;
        done = new bool[taskNames.Length];

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        if (checkIcon != null)
        {
            RectTransform r = checkIcon.GetComponent<RectTransform>();
            if (r != null) checkStartPos = r.anchoredPosition;
            checkIcon.SetActive(false);
        }
    }

    void Start()
    {
        ShowCurrent();
    }

    public void OnTaskCompleted(int index, int reward)
    {
        if (index < 0 || index >= done.Length) return;
        if (done[index]) return;

        done[index] = true;
        queue.Enqueue(new int[] { index, reward });

        if (!busy && gameObject.activeInHierarchy)
            StartCoroutine(Process());
    }

    // من الحفظ: يعلّم المهام المخلصة ويعرض اللي بعدها، بدون احتفال
    public void RestoreDone(bool[] saved)
    {
        if (saved == null || done == null) return;

        bool changed = false;
        for (int i = 0; i < done.Length && i < saved.Length; i++)
        {
            if (saved[i] && !done[i]) { done[i] = true; changed = true; }
        }

        if (changed && !busy && gameObject.activeInHierarchy)
            ShowCurrent();
    }

    IEnumerator Process()
    {
        busy = true;

        if (TutorialSpotlight.Instance != null)
            TutorialSpotlight.Instance.Hide();

        while (queue.Count > 0)
        {
            int[] item = queue.Dequeue();
            int reward = item[1];

            if (taskText != null)
                taskText.text = reward > 0
                    ? "كفو! +" + ToArabicNumbers(reward) + " قروش"
                    : "كفو!";

            yield return Bounce();
            yield return new WaitForSecondsRealtime(celebrateTime);

            if (checkIcon != null) checkIcon.SetActive(false);
        }

        busy = false;
        ShowCurrent();
    }

    IEnumerator Bounce()
    {
        if (checkIcon == null) yield break;

        RectTransform r = checkIcon.GetComponent<RectTransform>();
        checkIcon.SetActive(true);
        if (r == null) yield break;

        float t = 0f;
        float duration = 0.6f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            float hop = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * bounceHeight * (1f - k);
            r.anchoredPosition = checkStartPos + new Vector2(0f, hop);

            float scale = k < 0.3f ? Mathf.Lerp(0f, 1.3f, k / 0.3f) : Mathf.Lerp(1.3f, 1f, (k - 0.3f) / 0.7f);
            r.localScale = Vector3.one * scale;

            yield return null;
        }

        r.anchoredPosition = checkStartPos;
        r.localScale = Vector3.one;
    }

    void ShowCurrent()
    {
        for (int i = 0; i < done.Length; i++)
        {
            if (!done[i])
            {
                if (taskText != null) taskText.text = taskNames[i];
                group.alpha = 1f;

                if (TutorialSpotlight.Instance != null)
                    TutorialSpotlight.Instance.ShowStep(i);

                return;
            }
        }

        if (TutorialSpotlight.Instance != null)
            TutorialSpotlight.Instance.Hide();

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeOutTime);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }

        return new string(chars);
    }
}