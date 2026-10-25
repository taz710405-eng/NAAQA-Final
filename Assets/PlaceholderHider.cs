using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class PlaceholderHider : MonoBehaviour
{
    private TMP_InputField input;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
    }

    void LateUpdate()
    {
        if (input == null || input.placeholder == null) return;

        // التلميح يطلع بس إذا الخانة فاضية
        bool empty = string.IsNullOrEmpty(input.text);
        if (input.placeholder.gameObject.activeSelf != empty)
            input.placeholder.gameObject.SetActive(empty);
    }
}