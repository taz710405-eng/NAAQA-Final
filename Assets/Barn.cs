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