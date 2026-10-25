using System.Collections;
using UnityEngine;

public class PopupWindow : MonoBehaviour
{
    public float animTime = 0.15f;

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    IEnumerator Pop()
    {
        float t = 0f;
        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0.8f, 1f, t / animTime);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
