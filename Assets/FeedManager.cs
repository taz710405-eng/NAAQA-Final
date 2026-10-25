using UnityEngine;
using RTLTMPro;

public class FeedManager : MonoBehaviour
{
    public static FeedManager Instance;
    public RTLTextMeshPro feedText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateFeedText();
    }

    public void UpdateFeedText()
    {
        if (feedText == null)
        {
            Debug.LogError("FeedText غير مربوط في Inspector!", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!");
            return;
        }

        feedText.text = ToArabicNumbers(GameManager.Instance.feedCount);
        feedText.ForceMeshUpdate();
    }

    private string ToArabicNumbers(int number)
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