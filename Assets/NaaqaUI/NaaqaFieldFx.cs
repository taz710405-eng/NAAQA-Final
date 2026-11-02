using UnityEngine;
using UnityEngine.UI;
using TMPro;

// إطار خانة الكتابة يصير برتقالي لما تكون مختارة
public class NaaqaFieldFx : MonoBehaviour
{
    public Image border;

    private TMP_InputField input;
    private Color idle;
    private float k = 0f;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
        if (border != null) idle = border.color;
    }

    void Update()
    {
        if (border == null || input == null) return;
        float t = input.isFocused ? 1f : 0f;
        k = Mathf.MoveTowards(k, t, Time.unscaledDeltaTime * 6f);
        Color on = NaaqaUI.Orange;
        on.a = 0.95f;
        border.color = Color.Lerp(idle, on, k);
    }
}
