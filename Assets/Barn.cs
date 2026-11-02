using UnityEngine;
using UnityEngine.EventSystems;

public class Barn : MonoBehaviour
{
    public GameObject upgradeWindow;
    public GameObject dimBackground;

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (PlacementManager.IsPlacing || EditMode.Active) return;

        // بعد الشرح: نافذة تطوير الحوش الجديدة (من ١ لين ٥)
        OrderManager om = FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
        if (om != null && om.IsUpgraded)
        {
            PenScreen.Show();
            return;
        }

        if (upgradeWindow == null)
        {
            Debug.LogError("Upgrade Window غير مربوط في Barn!", this);
            return;
        }

        if (dimBackground != null) dimBackground.SetActive(true);
        upgradeWindow.SetActive(true);

        Debug.Log("فتحت نافذة التطوير");
    }
}