using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Resources")]
    public int milkCount = 0;
    public int money = 0;
    public int feedCount = 5;

    // القيم اللي تبدأ فيها اللعبة (عشان نرجع لها بعد تسجيل الخروج)
    private int startMilk, startMoney, startFeed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            startMilk = milkCount;
            startMoney = money;
            startFeed = feedCount;
            // سماعة وحدة بس في المشهد (الكاميرا فيها سماعة، فنطفي اللي هنا)
            AudioListener al = GetComponent<AudioListener>();
            if (al != null && Camera.main != null && Camera.main.GetComponent<AudioListener>() != null) al.enabled = false;
            // ما نخليه يبقى بين المشاهد: نفس الكائن فيه مدير التركيب والشنطة والحساب،
            // ولو بقى القديم، الجديد ينمسح ويضيع كل شي متركب في الحوش بعد تسجيل الخروج والدخول
            // (الحفظ يرجّع القيم بعد تحميل المشهد)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetResources()
    {
        milkCount = startMilk;
        money = startMoney;
        feedCount = startFeed;
    }

    public void AddMilk(int amount)
    {
        milkCount += amount;
        Debug.Log("Milk: " + milkCount);
    }

    public void AddMoney(int amount)
    {
        money += amount;
        Debug.Log("Money: " + money);
    }

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            Debug.Log("Money spent: " + amount + " | Remaining: " + money);
            return true;
        }

        Debug.Log("Not enough money!");
        return false;
    }

    public void AddFeed(int amount)
    {
        feedCount += amount;
        Debug.Log("Feed: " + feedCount);
    }

    public bool SpendFeed(int amount)
    {
        if (feedCount >= amount)
        {
            feedCount -= amount;
            Debug.Log("Feed spent: " + amount + " | Remaining: " + feedCount);
            return true;
        }

        Debug.Log("Not enough feed!");
        return false;
    }
}