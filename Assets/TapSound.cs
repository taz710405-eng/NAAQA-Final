using UnityEngine;

// عنصر زينة ينضغط: يطلع صوت وكلمة (مثل القعدة: نار وقهوة)
public class TapSound : MonoBehaviour
{
    public string sound = "fire";
    public string message = "حيّ الله من جانا، تفضّل على القهوة";

    void OnMouseDown()
    {
        if (PlacementManager.IsPlacing || EditMode.Active) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        CamelSounds.Play(sound, 2f);
        CamelSounds.Play("pour", 2f);
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(message);
    }
}
