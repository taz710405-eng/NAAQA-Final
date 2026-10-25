using UnityEngine;
using UnityEngine.UI;

public class LockUntilTutorial : MonoBehaviour
{
    [Header("مين يحدد إن الشرح خلص")]
    public OrderManager orderManager;   // آخر مهمة = تطوير العزبة

    [Header("الرسائل")]
    public string lockedMessage = "يفتح المتجر بعد ما تخلص المهام!";
    public string unlockedMessage = "انفتح المتجر!";

    [Header("الشكل وقت القفل")]
    public Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private GameObject blocker;
    private Image image;
    private Color normalColor;
    private bool unlocked = false;

    void Start()
    {
        image = GetComponent<Image>();
        if (image != null) normalColor = image.color;

        if (IsTutorialDone())
        {
            unlocked = true;
            return;
        }

        Lock();
    }

    void Update()
    {
        if (unlocked) return;

        if (IsTutorialDone())
        {
            Unlock();
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.Show(unlockedMessage);
        }
    }

    bool IsTutorialDone()
    {
        return orderManager != null && orderManager.IsUpgraded;
    }

    void Lock()
    {
        if (image != null) image.color = lockedColor;

        // غطاء شفاف فوق الزر يمسك الضغطة
        blocker = new GameObject("LockBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(transform, false);

        RectTransform rt = blocker.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        blocker.transform.SetAsLastSibling();

        Image bi = blocker.GetComponent<Image>();
        bi.color = new Color(0, 0, 0, 0);

        blocker.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowWarning(lockedMessage);
        });
    }

    void Unlock()
    {
        unlocked = true;
        if (image != null) image.color = normalColor;
        if (blocker != null) Destroy(blocker);
    }
}