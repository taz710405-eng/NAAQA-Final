using UnityEngine;
using RTLTMPro;

public class UpgradeWindowDone : MonoBehaviour
{
    public OrderManager orderManager;
    public GameObject doneText;
    public GameObject[] hideWhenDone;

    [Header("Message")]
    [TextArea] public string line1 = "عزبتك وصلت أعلى مستوى!";
    [TextArea] public string line2 = "توسعات أكثر بانتظارك في النسخة الكاملة";

    private int lastState = -1;

    void OnEnable()
    {
        lastState = -1;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        bool upgraded = orderManager != null && orderManager.IsUpgraded;
        int state = upgraded ? 1 : 0;
        if (state == lastState) return;
        lastState = state;

        if (hideWhenDone != null)
        {
            foreach (GameObject g in hideWhenDone)
                if (g != null) g.SetActive(!upgraded);
        }

        if (doneText != null)
        {
            doneText.SetActive(upgraded);

            if (upgraded)
            {
                RTLTextMeshPro rtl = doneText.GetComponent<RTLTextMeshPro>();
                if (rtl != null)
                {
                    rtl.text = line1 + "\n" + line2;
                    rtl.ForceMeshUpdate();
                }
            }
        }
    }
}