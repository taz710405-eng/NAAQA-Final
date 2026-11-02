using UnityEngine;
using TMPro;

// حدود غامقة حول النص (تنحط بعد ما يشتغل النص، عشان ما يصير خطأ لو كان مطفي)
public class CaptionOutline : MonoBehaviour
{
    public float width = 0.22f;
    public Color32 color = new Color32(30, 16, 6, 255);

    void Start()
    {
        TMP_Text t = GetComponent<TMP_Text>();
        if (t != null)
        {
            t.outlineWidth = width;
            t.outlineColor = color;
        }
        Destroy(this);
    }
}
