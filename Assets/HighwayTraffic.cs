using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ===== سيارات الطريق العام =====
// شاص، وسيارة سير، ولاندكروزر: تمشي رايحة وجاية كل شوي، كل وحدة في حارتها.
// وأحيانًا سيارة توقف على جنب الطريق قريب من الكشك، وينزل منها زبون يمشي للكشك ويطلب،
// ولما يخلص يرجع لها وتكمّل طريقها.
// الصور في Assets/Resources/NaaqaWorld/: car_<نوع>_go (رايحة لتحت يمين) و car_<نوع>_back (جاية لفوق يسار)
public class HighwayTraffic : MonoBehaviour
{
    static readonly string[] Kinds = { "shas", "ceer", "lc" };
    static readonly float[] Lengths = { 2.5f, 2.4f, 2.4f };   // عرض السيارة بالعالم
    const float Speed = 3.2f;
    const int MaxCars = 3;
    const float StopChance = 0.4f;   // كم سيارة من كل ١٠ توقف للكشك (لو فيه كشك)

    static HighwayTraffic instance;
    float next = 4f;
    readonly List<Car> cars = new List<Car>();
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (instance != null) return;
        if (Resources.Load<Texture2D>("NaaqaWorld/car_shas_go") == null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        instance = new GameObject("HighwayTraffic").AddComponent<HighwayTraffic>();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    Sprite Get(string name, float worldWidth)
    {
        Sprite sp;
        if (sprites.TryGetValue(name, out sp)) return sp;
        Texture2D tex = Resources.Load<Texture2D>("NaaqaWorld/" + name);
        if (tex == null) return null;
        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.3f), tex.width / worldWidth);
        sprites[name] = sp;
        return sp;
    }

    void Update()
    {
        cars.RemoveAll(c => c == null);
        Vector3 p, dir, nrm; float half;
        if (!HighwayRoad.Line(out p, out dir, out nrm, out half)) return;

        next -= Time.deltaTime;
        if (next > 0f || cars.Count >= MaxCars) return;
        next = Random.Range(5f, 11f);

        int k = Random.Range(0, Kinds.Length);
        bool go = Random.value < 0.5f;
        Sprite sp = Get("car_" + Kinds[k] + (go ? "_go" : "_back"), Lengths[k]);
        if (sp == null) return;

        // ما نحط سيارة فوق سيارة في نفس الحارة عند البداية
        float far = 34f;
        Vector3 start = p - dir * far * (go ? 1f : -1f);
        foreach (Car c in cars) if (c.go == go && (c.transform.position - start).sqrMagnitude < 25f) return;

        Car car = new GameObject("Car_" + Kinds[k]).AddComponent<Car>();
        car.Setup(sp, go, p, dir, nrm, half, far, PenOrder());
        // توقف للكشك؟
        if (Random.value < StopChance && !AnyStopping()) car.wantStop = true;
        cars.Add(car);
    }

    bool AnyStopping()
    {
        foreach (Car c in cars) if (c != null && c.wantStop) return true;
        return false;
    }

    static int PenOrder()
    {
        if (PenVisual.Instance == null) return 0;
        SpriteRenderer sr = PenVisual.Instance.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sortingOrder : 0;
    }

    static GameObject Kiosk()
    {
        if (PlacementManager.Instance == null) return null;
        foreach (PlacementManager.PlacedInfo pi in PlacementManager.Instance.GetPlaced())
            if (pi.id == StallCustomers.StallId && pi.go != null) return pi.go;
        return null;
    }

    // ===== سيارة وحدة =====
    public class Car : MonoBehaviour
    {
        public bool go, wantStop;
        SpriteRenderer sr;
        Vector3 linePoint, dir, nrm;
        float half, along, endAlong, lane;
        int state;            // ٠ تمشي، ١ تطلع للجنب، ٢ واقفة، ٣ ترجع للحارة
        float stopAlong, side, parkedFor;
        bool planned;
        Vector3 parkPos;

        public void Setup(Sprite sp, bool goingDown, Vector3 p, Vector3 d, Vector3 n, float h, float far, int order)
        {
            go = goingDown;
            linePoint = p; dir = d; nrm = n; half = h;
            float sgn = go ? 1f : -1f;
            along = -far * sgn;
            endAlong = far * sgn;
            // يمشون على اليمين: الرايح تحت، والجاي فوق
            lane = (go ? -1f : 1f) * half * 0.45f;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order + (go ? 2 : 1);
            if (PenVisual.Instance != null)
            {
                SpriteRenderer pen = PenVisual.Instance.GetComponent<SpriteRenderer>();
                if (pen != null) sr.sortingLayerID = pen.sortingLayerID;
            }
            Place(lane);
        }

        void Place(float off)
        {
            transform.position = linePoint + dir * along + nrm * off + Vector3.up * 0.01f;
        }

        void Update()
        {
            float sgn = go ? 1f : -1f;
            switch (state)
            {
                case 0:   // تمشي
                    if (wantStop) TryPlanStop();
                    along += sgn * Speed * Time.deltaTime;
                    Place(lane);
                    if (wantStop && planned && (stopAlong - along) * sgn <= 2.5f) state = 1;
                    if ((along - endAlong) * sgn >= 0f) Destroy(gameObject);
                    break;
                case 1:   // تهدّي وتطلع على جنب الطريق
                {
                    Vector3 to = parkPos;
                    transform.position = Vector3.MoveTowards(transform.position, to, Speed * 0.7f * Time.deltaTime);
                    if ((transform.position - to).sqrMagnitude < 0.01f)
                    {
                        along = stopAlong;
                        state = 2;
                        parkedFor = 0f;
                        Vector3 door = parkPos + nrm * side * 0.9f;
                        if (!StallCustomers.ArriveFromCar(door, () => { if (this != null) state = 3; })) state = 3;
                    }
                    break;
                }
                case 2:   // واقفة تنتظر الزبون
                    parkedFor += Time.deltaTime;
                    if (parkedFor > 3f && !StallCustomers.CarGuestActive) state = 3;   // الزبون راح (أو الكشك انشال)
                    if (parkedFor > 90f) state = 3;
                    break;
                case 3:   // ترجع للحارة وتكمّل
                {
                    wantStop = false;
                    Vector3 to = linePoint + dir * (along + sgn * 2f) + nrm * lane + Vector3.up * 0.01f;
                    transform.position = Vector3.MoveTowards(transform.position, to, Speed * 0.7f * Time.deltaTime);
                    if ((transform.position - to).sqrMagnitude < 0.01f) { along += sgn * 2f; state = 0; }
                    break;
                }
            }
        }

        // وين توقف: أقرب نقطة على الطريق للكشك (لو الكشك قريب من الطريق)
        void TryPlanStop()
        {
            if (planned) return;
            GameObject k = Kiosk();
            if (k == null || !TutorialEnd.CustomersAllowed || TutorialEnd.WantFirstCustomer || StallCustomers.CarGuestActive) { wantStop = false; return; }
            Vector3 kp = k.transform.position; kp.y = linePoint.y;
            float a = Vector3.Dot(kp - linePoint, dir);
            float sgn = go ? 1f : -1f;
            if ((a - along) * sgn < 4f) { wantStop = false; return; }   // الكشك وراها، ما تلحق
            float off = Vector3.Dot(kp - linePoint, nrm);
            if (Mathf.Abs(off) > 18f) { wantStop = false; return; }   // الكشك بعيد عن الطريق
            side = Mathf.Sign(off);
            if (side == 0f) side = 1f;
            // تطلع للجنب اللي فيه الكشك
            stopAlong = a;
            planned = true;
            parkPos = linePoint + dir * a + nrm * side * (half + 0.9f) + Vector3.up * 0.01f;
        }
    }
}
