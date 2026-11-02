using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TentClick : MonoBehaviour
{
    void OnMouseDown()
    {
        // وقت التركيب ما تنفتح
        if (PlacementManager.IsPlacing || EditMode.Active) return;

        // إذا فيه نافذة فوق الخيمة، الضغطة لها
        if (IsPointerOverUI()) return;

        // الخيمة صارت ملف اللاعب
        ProfileScreen.Show();
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (RaycastResult r in results)
        {
            if (r.gameObject.GetComponentInParent<TutorialSpotlight>() != null) continue;
            return true;
        }
        return false;
    }
}