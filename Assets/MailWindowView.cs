using UnityEngine;
using RTLTMPro;

public class MailWindowView : MonoBehaviour
{
    public RTLTextMeshPro emptyText;
    [TextArea] public string message = "ما عندك رسائل جديدة";

    void OnEnable()
    {
        if (emptyText == null)
        {
            Debug.LogError("Empty Text غير مربوط في MailWindowView!", this);
            return;
        }

        emptyText.gameObject.SetActive(true);
        emptyText.text = message;
        emptyText.ForceMeshUpdate();
    }
}