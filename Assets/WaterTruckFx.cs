using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// الوايت يمر تحت الشاشة لما يجي يعبّي الخزان
public class WaterTruckFx : MonoBehaviour
{
    public static void Play()
    {
        GameObject g = new GameObject("WaterTruckFx");
        g.AddComponent<WaterTruckFx>();
    }

    IEnumerator Start()
    {
        Canvas c = NaaqaUI.MakeCanvas("TruckCanvas", 600);
        c.transform.SetParent(transform, false);
        Destroy(c.GetComponent<GraphicRaycaster>());   // ما يمسك الضغطات

        Image truck = NaaqaUI.Img(c.transform, "Truck", NaaqaUI.Load("water_truck"), Color.white);
        truck.preserveAspect = true;
        RectTransform rt = truck.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(300, 200);

        RectTransform canvasRect = (RectTransform)c.transform;
        float w = canvasRect.rect.width;
        float t = 0f, dur = 4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            // يدخل، يوقف شوي في النص، ويطلع
            float x = k < 0.4f ? Mathf.Lerp(-200f, w * 0.5f, k / 0.4f)
                    : k < 0.6f ? w * 0.5f
                    : Mathf.Lerp(w * 0.5f, w + 200f, (k - 0.6f) / 0.4f);
            float bob = Mathf.Abs(Mathf.Sin(t * 14f)) * 3f;
            rt.anchoredPosition = new Vector2(x, 20f + bob);
            yield return null;
        }
        Destroy(gameObject);
    }
}
