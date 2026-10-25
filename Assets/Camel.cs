using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Camel : MonoBehaviour
{
    [Header("Optional")]
    public GameObject milkReadyIcon;

    [Header("Timer")]
    public GameObject timerBadge;
    public TMP_Text timerText;
    public bool arabicDigits = true;
    public Vector2 timerScreenOffset = new Vector2(0f, 120f);

    [Header("Settings")]
    public float productionTime = 10f;

    private bool isProducing = false;
    private bool milkReady = false;
    private float remaining = 0f;

    GameObject TimerObject
    {
        get
        {
            if (timerBadge != null) return timerBadge;
            if (timerText != null) return timerText.gameObject;
            return null;
        }
    }

    void Start()
    {
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);
        if (TimerObject != null) TimerObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (isProducing && TimerObject != null)
            FollowCamel();
    }

    void OnMouseDown()
    {
        // وقت التركيب الناقة ما تنضغط
        if (PlacementManager.IsPlacing)
        {
            Debug.Log("الناقة ما تنضغط: وضع التركيب شغال");
            return;
        }

        // إذا فيه نافذة أو زر فوق الناقة، الضغطة له مو للناقة
        if (IsPointerOverBlockingUI()) return;

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.ClickCamel);

        if (milkReady)
            CollectMilk();
        else if (!isProducing)
            FeedCamel();
        else
            Notify("الناقة تنتج الحين، انتظر شوي");
    }

    // يتأكد إن فيه واجهة فوق الماوس (ويتجاهل ظل الشرح)
    bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (RaycastResult r in results)
        {
            if (r.gameObject.GetComponentInParent<TutorialSpotlight>() != null) continue;
            Debug.Log("الضغطة انمسكت من: " + r.gameObject.name, r.gameObject);
            return true;
        }
        return false;
    }

    void FeedCamel()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!", this);
            return;
        }

        if (!GameManager.Instance.SpendFeed(1))
        {
            NotifyWarning("ما فيه علف!");
            return;
        }

        if (FeedManager.Instance != null)
            FeedManager.Instance.UpdateFeedText();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.FeedCamel);

        Notify("أكلت الناقة، بدأ الإنتاج");
        StartCoroutine(ProduceMilk());
    }

    IEnumerator ProduceMilk()
    {
        isProducing = true;
        remaining = productionTime;

        if (TimerObject != null)
        {
            TimerObject.SetActive(true);
            FollowCamel();
        }

        while (remaining > 0f)
        {
            if (timerText != null)
                timerText.text = FormatTime(remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }

        isProducing = false;
        milkReady = true;

        if (TimerObject != null) TimerObject.SetActive(false);
        if (milkReadyIcon != null) milkReadyIcon.SetActive(true);

        Notify("الحليب جاهز! اضغط على الناقة");
    }

    void CollectMilk()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!", this);
            return;
        }

        GameManager.Instance.AddMilk(1);

        if (MilkManager.Instance != null)
            MilkManager.Instance.UpdateMilkText();

        if (TaskManager.Instance != null)
            TaskManager.Instance.CompleteTask(TaskManager.CollectMilk);

        milkReady = false;
        if (milkReadyIcon != null) milkReadyIcon.SetActive(false);

        Notify("+" + ToArabicNumbers("1") + " حليب");
    }

    void Notify(string message)
    {
        Debug.Log(message);
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show(message);
    }

    void NotifyWarning(string message)
    {
        Debug.Log(message);
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(message);
    }

    string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        int m = total / 60;
        int s = total % 60;

        string result = m.ToString("00") + ":" + s.ToString("00");
        return arabicDigits ? ToArabicNumbers(result) : result;
    }

    void FollowCamel()
    {
        Camera cam = Camera.main;
        GameObject target = TimerObject;
        if (cam == null || target == null) return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null) return;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        RectTransform parent = rect.parent as RectTransform;
        if (canvas == null || parent == null) return;

        Vector2 screenPos = (Vector2)cam.WorldToScreenPoint(transform.position) + timerScreenOffset;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCam, out localPos))
            rect.localPosition = localPos;
    }

    string ToArabicNumbers(string text)
    {
        char[] chars = text.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }

        return new string(chars);
    }
}