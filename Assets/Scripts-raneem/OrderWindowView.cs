using RTLTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// نافذة الطلب: "طلب جديد - حليب 2 - المكافأة 50 - [تسليم]"
public class OrderWindowView : MonoBehaviour
{
    public RTLTextMeshPro titleText;    // طلب جديد
    public RTLTextMeshPro itemText;     // حليب 2
    public RTLTextMeshPro haveText;     // عندك 0 من 2
    public RTLTextMeshPro rewardText;   // +50
    public Button deliverButton;        // تسليم

    // رغد تربط هنا دالة التسليم حقتها
    public UnityEvent onDeliver;

    [Header("Auto Update")]
    public bool autoUpdate = true;
    public OrderManager orderManager;
    public string itemName = "حليب";

    [Header("After Delivery")]
    public GameObject emptyText;
    public GameObject[] hideWhenDone;

    [Header("Colors")]
    public Color notEnoughColor = new Color(0.85f, 0.15f, 0.15f);
    public Color enoughColor = new Color(0.2f, 0.65f, 0.2f);

    [Header("Button")]
    public bool disableWhenNotEnough = false;

    private int lastHave = -1;
    private int lastNeed = -1;
    private int lastDone = -1;
    private int lastReward = -1;
    private int lastWait = -1;
    private RTLTextMeshPro waitText;   // «طلب جديد يوصلك بعد...»

    void Awake()
    {
        if (deliverButton != null) deliverButton.onClick.AddListener(Deliver);
    }

    void OnEnable()
    {
        lastHave = -1;
        lastNeed = -1;
        lastDone = -1;
        lastReward = -1;
        AutoRefresh();
    }

    void Update()
    {
        AutoRefresh();
    }

    void AutoRefresh()
    {
        if (!autoUpdate || GameManager.Instance == null) return;

        // بعد الشرح الطلبات تتجدد، فالنافذة ما تصير فاضية
        bool delivered = orderManager != null && !orderManager.HasOpenOrder;
        int done = delivered ? 1 : 0;

        if (done != lastDone)
        {
            lastDone = done;
            SetDoneState(delivered);
            lastHave = -1;
        }

        // ننتظر الطلب الجاي: عداد بدل «ما عندك طلبات»
        UpdateWaitText(delivered && orderManager != null && orderManager.IsWaiting);

        if (delivered) return;

        int need = orderManager != null ? orderManager.requiredMilk : 2;
        int reward = orderManager != null ? orderManager.rewardCoins : 50;
        int have = GameManager.Instance.milkCount;

        if (have == lastHave && need == lastNeed && reward == lastReward) return;
        lastHave = have;
        lastNeed = need;
        lastReward = reward;

        Show(itemName, have, need, reward);
    }

    void UpdateWaitText(bool waiting)
    {
        if (!waiting)
        {
            if (waitText != null && waitText.gameObject.activeSelf) waitText.gameObject.SetActive(false);
            lastWait = -1;
            return;
        }

        if (waitText == null && emptyText != null)
        {
            // نسوي نص عربي بنفس مكان وحجم EmptyText
            GameObject g = new GameObject("WaitText", typeof(RectTransform));
            g.transform.SetParent(emptyText.transform.parent, false);
            RectTransform src = (RectTransform)emptyText.transform, rt = (RectTransform)g.transform;
            rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition; rt.sizeDelta = src.sizeDelta;
            waitText = g.AddComponent<RTLTextMeshPro>();
            TMPro.TMP_Text old = emptyText.GetComponent<TMPro.TMP_Text>();
            waitText.font = old != null ? old.font : NaaqaUI.Font;
            waitText.fontSize = old != null ? old.fontSize : 34;
            waitText.color = old != null ? old.color : Color.black;
            waitText.alignment = TMPro.TextAlignmentOptions.Center;
            waitText.raycastTarget = false;
        }
        if (waitText == null) return;

        if (emptyText.activeSelf) emptyText.SetActive(false);
        if (!waitText.gameObject.activeSelf) waitText.gameObject.SetActive(true);

        int sec = Mathf.CeilToInt(orderManager.WaitLeft);
        if (sec == lastWait) return;
        lastWait = sec;
        waitText.text = "طلب جديد يوصلك بعد " + ToArabicNumbers(sec) + " ثانية";
    }

    void SetDoneState(bool delivered)
    {
        if (hideWhenDone != null)
        {
            foreach (GameObject g in hideWhenDone)
                if (g != null) g.SetActive(!delivered);
        }

        if (emptyText != null) emptyText.SetActive(delivered);
    }

    public void Show(string itemName, int have, int need, int reward)
    {
        if (titleText != null) titleText.text = "طلب جديد";
        if (itemText != null) itemText.text = itemName + " " + ToArabicNumbers(need);

        if (haveText != null)
        {
            haveText.text = "عندك " + ToArabicNumbers(have) + " من " + ToArabicNumbers(need);
            haveText.color = have >= need ? enoughColor : notEnoughColor;
        }

        if (rewardText != null) rewardText.text = "+" + ToArabicNumbers(reward);

        if (deliverButton != null)
            deliverButton.interactable = !disableWhenNotEnough || have >= need;
    }

    void Deliver()
    {
        onDeliver.Invoke();
        if (UIManager.Instance != null) UIManager.Instance.CloseAll();
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