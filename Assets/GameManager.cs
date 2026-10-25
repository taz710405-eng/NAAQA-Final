using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Resources")]
    public int milkCount = 0;
    public int money = 0;
    public int feedCount = 5;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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