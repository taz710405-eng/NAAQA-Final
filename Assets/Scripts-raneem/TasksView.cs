using UnityEngine;

// نافذة المهام: يشغّل علامة ✓ للمهمة اللي انتهت
// الترتيب: 0 اضغط على الإبل، 1 أطعم الإبل، 2 اجمع الحليب، 3 سلّم الطلب، 4 طوّر العزبة
public class TasksView : MonoBehaviour
{
    public static TasksView Instance { get; private set; }

    public GameObject[] checks;   // علامات Check بنفس ترتيب المهام

    void Awake()
    {
        Instance = this;
        foreach (GameObject c in checks)
            if (c != null) c.SetActive(false);
    }

    // الكود حق البنات يناديها لما تخلص مهمة، مثلًا: TasksView.Instance.CompleteTask(1);
    public void CompleteTask(int index)
    {
        if (index >= 0 && index < checks.Length && checks[index] != null)
            checks[index].SetActive(true);
    }
}