using UnityEngine;
using TMPro;
using RTLTMPro;

[RequireComponent(typeof(TMP_InputField))]
public class RtlInputMirror : MonoBehaviour
{
    [Tooltip("نص RTL يعرض الكتابة موصولة")]
    public RTLTextMeshPro display;

    private TMP_InputField input;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();

        // نخفي الكتابة الأصلية (المؤشر يبقى ظاهر)
        if (input.textComponent != null)
        {
            Color c = input.textComponent.color;
            c.a = 0f;
            input.textComponent.color = c;
        }

        input.onValueChanged.AddListener(UpdateDisplay);
    }

    void OnEnable()
    {
        if (input != null) UpdateDisplay(input.text);
    }

    void LateUpdate()
    {
        // التلميح يطلع بس إذا الخانة فاضية
        if (input != null && input.placeholder != null)
        {
            bool empty = string.IsNullOrEmpty(input.text);
            if (input.placeholder.gameObject.activeSelf != empty)
                input.placeholder.gameObject.SetActive(empty);
        }
    }

    void UpdateDisplay(string value)
    {
        if (display == null) return;

        display.gameObject.SetActive(!string.IsNullOrEmpty(value));
        display.text = value;
        display.ForceMeshUpdate();
    }
}