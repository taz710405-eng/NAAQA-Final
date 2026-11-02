using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// المسجد على الخريطة: يعلّمك وقت الصلاة الجاية، ووقت الأذان يطلع فوقه نور خفيف
public class MosqueItem : MonoBehaviour
{
    private bool placed = false;
    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void OnEnable() { PlacementManager.OnItemPlaced += OnPlaced; }
    void OnDisable() { PlacementManager.OnItemPlaced -= OnPlaced; }
    void OnPlaced(string id, GameObject go) { if (go == gameObject) placed = true; }

    void Update()
    {
        if (!placed || sr == null || PlacementManager.IsPlacing) return;
        // وقت الصلاة: يضوي بهدوء
        float k = PrayerClock.IsPrayerTime ? 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f)) : 0f;
        sr.color = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.75f, baseColor.a), k);
    }

    void OnMouseDown()
    {
        if (!placed || PlacementManager.IsPlacing || EditMode.Active || IsPointerOverUI()) return;

        string msg = PrayerClock.IsPrayerTime
            ? "وقت صلاة " + PrayerClock.CurrentName + ". السوق يفتح بعد " + NaaqaUI.Ar(Mathf.CeilToInt(PrayerClock.SecondsToOpen)) + " ثانية"
            : "صلاة " + PrayerClock.NextName + " بعد " + NaaqaUI.Ar(Mathf.CeilToInt(PrayerClock.SecondsToNext)) + " ثانية";
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
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
