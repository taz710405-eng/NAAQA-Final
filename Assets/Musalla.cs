using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ===== مصلى الطريق =====
// ينبني جنب الطريق العام (مستوى ١٢، ١٠٠٠ قرش، مرة وحدة).
// يزيد زباين الكشك ١٥٪، وكل ١٥–٢٠ دقيقة يوقف مسافر يصلي ويعطيك ٤ خبرة، و٣٥٪ يتبرع ١–٣ قروش.
public class Musalla : MonoBehaviour
{
    public const string Id = "musalla";
    static readonly string[] Travelers = { "npc_traveler", "npc_man", "npc_phone", "npc_oldbisht", "npc_bisht", "npc_elder", "npc_delivery" };

    static Musalla instance;
    float next = 120f;   // أول مسافر بعد دقيقتين، وبعدها كل ١٥–٢٠ دقيقة
    readonly List<Walker> walkers = new List<Walker>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (instance != null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        instance = new GameObject("Musalla").AddComponent<Musalla>();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    public static GameObject Placed()
    {
        if (PlacementManager.Instance == null) return null;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced())
            if (p.id == Id && p.go != null) return p.go;
        return null;
    }

    void Update()
    {
        walkers.RemoveAll(w => w == null);
        GameObject m = Placed();
        if (m == null || PlacementManager.IsPlacing) return;
        next -= Time.deltaTime;
        if (next > 0f || walkers.Count >= 2) return;
        next = Random.Range(GameRules.TravelerMin, GameRules.TravelerMax);   // مسافر كل ١٥–٢٠ دقيقة

        Vector3 p, dir, nrm; float half;
        if (!HighwayRoad.Line(out p, out dir, out nrm, out half)) return;
        Renderer r = m.GetComponentInChildren<Renderer>();
        Bounds b = r != null ? r.bounds : new Bounds(m.transform.position, Vector3.one);
        Vector3 door = new Vector3(b.center.x - b.size.x * 0.05f, m.transform.position.y + 0.05f, b.min.z + b.size.z * 0.22f);
        // نقطة على طرف الطريق قريبة من المصلى
        Vector3 flat = new Vector3(door.x, p.y, door.z);
        float along = Vector3.Dot(flat - p, dir);
        float side = Mathf.Sign(Vector3.Dot(flat - p, nrm));
        Vector3 roadside = p + dir * along + nrm * side * (half + 0.4f);
        roadside.y = door.y;

        string who = Travelers[Random.Range(0, Travelers.Length)];
        Texture2D tex = NaaqaUI.LoadTexture(who);
        if (tex == null) return;
        Walker w = new GameObject("Traveler_" + who).AddComponent<Walker>();
        w.Setup(tex, roadside, door, m);
        walkers.Add(w);
    }

    // مسافر: يجي من الطريق، يوقف عند المصلى شوي، ويرجع
    public class Walker : MonoBehaviour
    {
        SpriteRenderer sr;
        Vector3 from, to;
        int stage;          // ٠ رايح، ١ يصلي، ٢ راجع
        float wait;

        public void Setup(Texture2D tex, Vector3 a, Vector3 b, GameObject musalla)
        {
            from = a; to = b;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            transform.position = a;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.03f), tex.height / 1.7f);
            SpriteRenderer ms = musalla.GetComponentInChildren<SpriteRenderer>();
            if (ms != null) { sr.sortingLayerID = ms.sortingLayerID; sr.sortingOrder = ms.sortingOrder + 3; }
            sr.flipX = b.x < a.x;
        }

        void Update()
        {
            if (sr == null) return;
            sr.enabled = !PlacementManager.IsPlacing;
            if (stage == 1)
            {
                wait -= Time.deltaTime;
                if (wait <= 0f) { stage = 2; sr.flipX = from.x < to.x; }
                return;
            }
            Vector3 t = stage == 0 ? to : from;
            transform.position = Vector3.MoveTowards(transform.position, t, 1.8f * Time.deltaTime);
            if ((transform.position - t).sqrMagnitude > 0.01f) return;
            if (stage == 0)
            {
                stage = 1;
                wait = 5f;
                // يعطيك خبرة، وأحيانًا قروش (صدقة)
                LevelManager.AddXp(LevelManager.XpPerTraveler);
                if (Random.value < 0.35f && GameManager.Instance != null)
                {
                    int c = Random.Range(1, 4);
                    GameManager.Instance.AddMoney(c);
                    if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
                    if (NotificationManager.Instance != null) NotificationManager.Instance.Show("مسافر صلى في مصلاك وقال: الله يجزاك خير +" + NaaqaUI.Ar(c) + " قرش");
                }
            }
            else Destroy(gameObject);
        }
    }
}
