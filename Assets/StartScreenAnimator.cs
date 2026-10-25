using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StartScreenAnimator : MonoBehaviour
{
    [Header("العناصر")]
    public RectTransform background;   // صورة الخلفية
    public RectTransform logo;         // اللوقو
    public RectTransform startButton;  // زر ابدأ

    [Header("الخلفية")]
    public float bgZoom = 0.06f;       // كم تقرّب
    public float bgMove = 25f;         // كم تتحرك
    public float bgSpeed = 0.08f;

    [Header("اللوقو")]
    public float logoFloat = 12f;
    public float logoSpeed = 1.2f;

    [Header("زر ابدأ")]
    public float pulse = 0.06f;
    public float pulseSpeed = 2.5f;

    [Header("ذرات الرمل")]
    public int dustCount = 25;
    public Color dustColor = new Color(1f, 0.92f, 0.75f, 0.55f);

    private Vector2 bgPos, logoPos;
    private Vector3 bgScale, btnScale;
    private readonly List<RectTransform> dust = new List<RectTransform>();
    private readonly List<Vector2> dustVel = new List<Vector2>();
    private RectTransform area;

    void Start()
    {
        area = GetComponent<RectTransform>();
        if (background != null) { bgPos = background.anchoredPosition; bgScale = background.localScale; }
        if (logo != null) logoPos = logo.anchoredPosition;
        if (startButton != null) btnScale = startButton.localScale;

        Sprite dot = MakeDot();
        for (int i = 0; i < dustCount; i++)
        {
            GameObject g = new GameObject("Dust", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(transform, false);
            if (background != null) g.transform.SetSiblingIndex(background.GetSiblingIndex() + 1);

            Image img = g.GetComponent<Image>();
            img.sprite = dot;
            img.raycastTarget = false;
            Color c = dustColor; c.a *= Random.Range(0.4f, 1f);
            img.color = c;

            RectTransform rt = g.GetComponent<RectTransform>();
            float s = Random.Range(4f, 10f);
            rt.sizeDelta = new Vector2(s, s);
            rt.anchoredPosition = RandomPoint();

            dust.Add(rt);
            dustVel.Add(new Vector2(Random.Range(15f, 45f), Random.Range(-6f, 6f)));
        }
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (background != null)
        {
            float z = 1f + bgZoom * (0.5f + 0.5f * Mathf.Sin(t * bgSpeed * Mathf.PI * 2f));
            background.localScale = bgScale * z;
            background.anchoredPosition = bgPos + new Vector2(
                Mathf.Sin(t * bgSpeed * 3f) * bgMove,
                Mathf.Cos(t * bgSpeed * 2f) * bgMove * 0.5f);
        }

        if (logo != null)
            logo.anchoredPosition = logoPos + new Vector2(0f, Mathf.Sin(t * logoSpeed) * logoFloat);

        if (startButton != null)
            startButton.localScale = btnScale * (1f + Mathf.Sin(t * pulseSpeed) * pulse);

        Rect r = area.rect;
        for (int i = 0; i < dust.Count; i++)
        {
            Vector2 p = dust[i].anchoredPosition + (dustVel[i] + new Vector2(0f, Mathf.Sin(t + i) * 8f)) * Time.unscaledDeltaTime;
            if (p.x > r.xMax + 20f) p = new Vector2(r.xMin - 20f, Random.Range(r.yMin, r.yMax));
            dust[i].anchoredPosition = p;
        }
    }

    Vector2 RandomPoint()
    {
        Rect r = area.rect;
        return new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
    }

    Sprite MakeDot()
    {
        int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}