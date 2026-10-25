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
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string message, float seconds = 2.5f)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, seconds));
    }

    public void ShowFinalMessage()
    {
        Show("أحسنت! بدأت رحلتك في ناقة", 3f);
    }

    IEnumerator ShowRoutine(string message, float seconds)
    {
        messageText.text = message;
        panel.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        panel.SetActive(false);
    }
}