using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public const int ClickCamel = 0;
    public const int FeedCamel = 1;
    public const int CollectMilk = 2;
    public const int DeliverOrder = 3;
    public const int UpgradePen = 4;

    [Header("Checks (same order as tasks)")]
    public Image[] checks = new Image[5];

    [Header("Rewards")]
    public int[] rewards = { 10, 10, 10, 20, 10 };

    [Header("Check Look")]
    public Sprite doneSprite;
    public Color doneColor = new Color(0.3f, 0.75f, 0.3f);

    private bool[] done = new bool[5];

    void Awake()
    {
        Instance = this;
        rewards = new int[] { 5, 5, 5, 5, 5 };   // الشرح يعطي قليل (اللاعب يجمع بنفسه)
    }

    public void CompleteTask(int index)
    {
        if (index < 0 || index >= done.Length) return;
        if (done[index]) return;

        done[index] = true;
        ShowCheck(index);

        int reward = index < rewards.Length ? rewards[index] : 0;

        if (reward > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(reward);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }

        if (TaskBanner.Instance != null)
            TaskBanner.Instance.OnTaskCompleted(index, reward);

        LevelManager.AddXp(LevelManager.XpPerTask);

        Debug.Log("مهمة " + (index + 1) + " خلصت! +" + reward + " قرش");
    }

    void ShowCheck(int index)
    {
        if (index >= checks.Length || checks[index] == null) return;
        checks[index].gameObject.SetActive(true);
        checks[index].enabled = true;
        if (doneSprite != null) checks[index].sprite = doneSprite;
        checks[index].color = doneColor;
    }

    // ===== الحفظ =====
    public bool[] GetDone()
    {
        return (bool[])done.Clone();
    }

    // يرجّع المهام المخلصة بدون مكافأة وبدون احتفال
    public void RestoreDone(bool[] saved)
    {
        if (saved == null) return;

        for (int i = 0; i < done.Length && i < saved.Length; i++)
        {
            if (saved[i] && !done[i])
            {
                done[i] = true;
                ShowCheck(i);
            }
        }

        if (TaskBanner.Instance != null)
            TaskBanner.Instance.RestoreDone(done);
    }
}