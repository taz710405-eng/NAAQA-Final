using System.Collections;
using RTLTMPro;
using UnityEngine;

public class ToastView : MonoBehaviour
{
    public static ToastView Instance { get; private set; }

    public GameObject panel;
    public RTLTextMeshPro messageText;

    void Awake()
    {
        Instance = this;
        if (panel != null) NaaqaUI.DarkPill(panel.GetComponent<UnityEngine.UI.Image>(), 30f);
        if (messageText != null) messageText.color = NaaqaUI.Cream;
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string message, float seconds = 2.5f)
    {
        // كل الإشعارات صارت بمكان واحد (تحت المستوى) عشان ما تركب فوق شريط المهمة
        if (NotificationManager.Instance != null) { NotificationManager.Instance.Show(message); return; }
        if (!string.IsNullOrEmpty(message) && message.Length > 14) MailBox.Add(message);
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, seconds));
    }

    public void ShowFinalMessage()
    {
        Show("أحسنت! بدأت رحلتك في ناقة", 3f);
    }

    IEnumerator ShowRoutine(string message, float seconds)
    {
        // ما تطلع فوق إشعار ثاني: ننتظره يخلص (بحد أقصى ٤ ثواني)
        float wait = 0f;
        while (NotificationManager.Instance != null && NotificationManager.Instance.Showing && wait < 4f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }
        messageText.text = message;
        panel.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        panel.SetActive(false);
    }
}