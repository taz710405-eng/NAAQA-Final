using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TentClick : MonoBehaviour
{
    void OnMouseDown()
    {
        // وقت التركيب ما تنفتح
        if (PlacementManager.IsPlacing) return;

        // إذا فيه نافذة فوق الخيمة، الضغطة لها
        if (IsPointerOverUI()) return;

        if (AccountManager.Instance != null)
            AccountManager.Instance.Open();
        else
            Debug.LogError("AccountManager مو موجود!");
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