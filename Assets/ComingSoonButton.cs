using UnityEngine;
using UnityEngine.UI;

public class ComingSoonButton : MonoBehaviour
{
    [TextArea] public string message = "قريبًا في النسخة الكاملة!";
    public bool asWarning = false;

    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(Show);
    }

    void Show()
    {
        // الميزة صارت جاهزة (مثل شراء العلف)، فما نطلع «قريبًا»
        if (GetComponent<FeedShopButton>() != null) return;

        if (NotificationManager.Instance == null)
        {
            Debug.Log(message);
            return;
        }

        if (asWarning)
            NotificationManager.Instance.ShowWarning(message);
        else
            NotificationManager.Instance.Show(message);
    }
}