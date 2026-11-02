using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ===== شراء العلف بالقروش =====
// يركّب نفسه لحاله على زر ➕ اللي جنب العلف (bar_feed) بدل رسالة «قريبًا».
// ما يحتاج تعديل في المشهد. ولو تبين تغيّرين السعر: ركّبيه بنفسك على الزر وغيّري الأرقام،
// أو غيّري القيم الافتراضية تحت.
public class FeedShopButton : MonoBehaviour
{
    [Header("السعر")]
    public int price = 20;        // كم قرش
    public int amount = 5;        // كم علف ياخذ

    // مكان الزر (للتركيب التلقائي)
    public const string FeedBarName = "bar_feed";

    private Button button;

    // ===== تركيب تلقائي =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        AttachInScene();
        SceneManager.sceneLoaded += (s, m) => AttachInScene();
    }

    static void AttachInScene()
    {
        foreach (ComingSoonButton cs in FindObjectsByType<ComingSoonButton>(FindObjectsInactive.Include))
        {
            Transform p = cs.transform.parent;
            if (p == null || p.name != FeedBarName) continue;
            if (cs.GetComponent<FeedShopButton>() != null) continue;

            cs.gameObject.AddComponent<FeedShopButton>();
        }
    }

    void Awake()
    {
        // رسالة «قريبًا» تسكت لحالها لما يكون هذا السكربت على الزر (شوفي ComingSoonButton)
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Buy);
    }

    public void Buy()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!gm.SpendMoney(price))
        {
            Warn("تحتاج " + ToArabicNumbers(price) + " قرش عشان تشتري " + ToArabicNumbers(amount) + " علف");
            return;
        }

        gm.AddFeed(amount);

        if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();

        Notify("+" + ToArabicNumbers(amount) + " علف (−" + ToArabicNumbers(price) + " قرش)");
    }

    void Notify(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
    }

    void Warn(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(msg);
    }

    string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('٠' + (chars[i] - '0'));
        return new string(chars);
    }
}
