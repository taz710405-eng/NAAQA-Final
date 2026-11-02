using UnityEngine;
using TMPro;

// يخلي شريط اسم الأيقونة على قد الكلمة بالضبط (بعد ما ينرسم النص)
public class CaptionFit : MonoBehaviour
{
    public TMP_Text text;
    float lastW = -1f;

    void LateUpdate()
    {
        if (text == null) return;
        float w = text.preferredWidth;
        if (w <= 1f || Mathf.Abs(w - lastW) < 0.5f) return;
        lastW = w;
        RectTransform rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(w + 22f, text.fontSize + 14f);
    }
}
