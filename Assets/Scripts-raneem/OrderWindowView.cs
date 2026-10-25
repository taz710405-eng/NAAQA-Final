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

    void Awake()
    {
        if (deliverButton != null) deliverButton.onClick.AddListener(Deliver);
    }

    void OnEnable()
    {
        lastHave = -1;
        lastNeed = -1;
        lastDone = -1;
        AutoRefresh();
    }

    void Update()
    {
        AutoRefresh();
    }

    void AutoRefresh()
    {
        if (!autoUpdate || GameManager.Instance == null) return;

        bool delivered = orderManager != null && orderManager.OrderDelivered;
        int done = delivered ? 1 : 0;

        if (done != lastDone)
        {
            lastDone = done;
            SetDoneState(delivered);
            lastHave = -1;
        }

        if (delivered) return;

        int need = orderManager != null ? orderManager.requiredMilk : 2;
        int reward = orderManager != null ? orderManager.rewardCoins : 50;
        int have = GameManager.Instance.milkCount;

        if (have == lastHave && need == lastNeed) return;
        lastHave = have;
        lastNeed = need;

        Show(itemName, have, need, reward);
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