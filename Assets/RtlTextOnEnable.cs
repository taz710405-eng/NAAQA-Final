using UnityEngine;
using RTLTMPro;

[RequireComponent(typeof(RTLTextMeshPro))]
public class RtlTextOnEnable : MonoBehaviour
{
    [TextArea] public string text;

    void OnEnable()
    {
        RTLTextMeshPro rtl = GetComponent<RTLTextMeshPro>();
        if (rtl == null || string.IsNullOrEmpty(text)) return;

        rtl.text = text;
        rtl.ForceMeshUpdate();
    }
}