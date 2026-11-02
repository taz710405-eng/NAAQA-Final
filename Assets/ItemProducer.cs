using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

// ===== عنصر منتج على الخريطة =====
// المزارع تنتج (علف، تمر، ترنج، حليب غنم)، والبير يعطي ماء محدود لين ينضب،
// والخزان يتعبى بوايت ماء من المدينة. اللاعب يضغط عليه ويجمع.
// ShopExtraItems يركّبه لحاله حسب نوع العنصر.
public class ItemProducer : MonoBehaviour
{
    public enum Kind { Feed, Milk, Dates, Citron, Well, Tank }

    public Kind kind = Kind.Feed;
    public string itemName = "مزرعة علف";
    public int amount = 3;
    public float every = 60f;
    public int uses = 0;                 // للبير: كم مرة يعطي قبل ما ينضب (٠ = بلا حد)

    [Header("الخزان")]
    public int truckCost = 40;
    public float truckTime = 20f;

    public bool IsPlaced { get { return placed; } }

    private bool placed = false;
    private bool ready = false;
    private bool truckComing = false;
    private float left;
    private int usesLeft;
    private Vector3 baseScale;
    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    void Awake()
    {
        baseScale = transform.localScale;
        left = every;
        usesLeft = uses;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void OnEnable() { PlacementManager.OnItemPlaced += OnPlaced; }
    void OnDisable() { PlacementManager.OnItemPlaced -= OnPlaced; }

    void OnPlaced(string id, GameObject go)
    {
        if (go != gameObject) return;
        placed = true;
        ready = false;
        left = every;
        if (FarmStore.Instance != null) FarmStore.Instance.RecalcWaterCap();
    }

    bool Dry { get { return kind == Kind.Well && uses > 0 && usesLeft <= 0; } }

    void Update()
    {
        if (!placed) return;

        if (kind != Kind.Tank && !ready && !Dry)
        {
            left -= Time.deltaTime;
            if (left <= 0f)
            {
                ready = true;
                Notify(itemName + " " + ReadyWord() + "! اضغط عليها");
            }
        }

        // جاهز = نبضة خفيفة، والبير الناضب يصير باهت
        float k = ready ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.04f : 1f;
        transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        if (sr != null && !PlacementManager.IsPlacing)
            sr.color = Dry ? new Color(baseColor.r * 0.75f, baseColor.g * 0.65f, baseColor.b * 0.55f, baseColor.a) : baseColor;
    }

    string ReadyWord()
    {
        return kind == Kind.Well ? "فيه ماء" : "جاهزة";
    }

    void OnMouseDown()
    {
        if (!placed || PlacementManager.IsPlacing || EditMode.Active || IsPointerOverUI()) return;

        if (kind == Kind.Tank) { OrderTruck(); return; }

        if (Dry)
        {
            Notify("البير نضب، ما بقى فيه ماء. اشترِ خزان وعبّه من الوايت");
            return;
        }

        if (!ready)
        {
            Notify(itemName + ": باقي " + NaaqaUI.Ar(Mathf.CeilToInt(left)) + " ثانية");
            return;
        }

        if (!Collect()) return;

        ready = false;
        left = every;
        if (kind == Kind.Well && uses > 0)
        {
            usesLeft--;
            if (usesLeft <= 0) Notify("هذي آخر موية في البير، نضب الحين");
        }
        LevelManager.AddXp(LevelManager.XpPerMilk);
    }

    bool Collect()
    {
        GameManager gm = GameManager.Instance;
        FarmStore store = FarmStore.Instance;
        switch (kind)
        {
            case Kind.Feed:
                if (gm == null) return false;
                gm.AddFeed(amount);
                if (FeedManager.Instance != null) FeedManager.Instance.UpdateFeedText();
                Notify("+" + NaaqaUI.Ar(amount) + " علف من " + itemName);
                return true;
            case Kind.Milk:
                if (gm == null) return false;
                gm.AddMilk(amount);
                if (MilkManager.Instance != null) MilkManager.Instance.UpdateMilkText();
                Notify("+" + NaaqaUI.Ar(amount) + " حليب من " + itemName);
                return true;
            case Kind.Dates:
                if (store == null) return false;
                store.AddDates(amount);
                Notify("+" + NaaqaUI.Ar(amount) + " تمر. بعه في السوق الشعبي");
                return true;
            case Kind.Citron:
                if (store == null) return false;
                store.AddCitron(amount);
                Notify("+" + NaaqaUI.Ar(amount) + " ترنج. بعه في السوق الشعبي");
                return true;
            case Kind.Well:
                if (store == null) return false;
                int added = store.AddWater(amount);
                if (added <= 0) { Notify("الماء مليان، اسقِ الإبل أول"); return false; }
                Notify("+" + NaaqaUI.Ar(added) + " ماء من البير");
                return true;
        }
        return false;
    }

    // ===== الخزان: طلب وايت من المدينة =====
    void OrderTruck()
    {
        FarmStore store = FarmStore.Instance;
        if (store == null) return;
        if (truckComing) { Notify("الوايت جاي في الطريق، اصبر شوي"); return; }
        if (store.Water >= store.WaterCap) { Notify("الخزان مليان"); return; }

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.SpendMoney(truckCost))
        {
            Warn("طلب الوايت يبي " + NaaqaUI.Ar(truckCost) + " قرش");
            return;
        }
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();

        truckComing = true;
        Notify("طلبت وايت ماء من المدينة. يوصلك بعد " + NaaqaUI.Ar(Mathf.RoundToInt(truckTime)) + " ثانية");
        StartCoroutine(Truck());
    }

    IEnumerator Truck()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, truckTime - 4f));
        WaterTruckFx.Play();      // الوايت يمر على الشاشة
        yield return new WaitForSeconds(4f);
        truckComing = false;
        if (FarmStore.Instance != null)
        {
            int added = FarmStore.Instance.AddWater(FarmStore.Instance.WaterCap);
            Notify("وصل الوايت! +" + NaaqaUI.Ar(added) + " ماء في الخزان");
        }
    }

    // ===== الحفظ =====
    public string SaveState()
    {
        return Mathf.CeilToInt(left) + "|" + usesLeft + "|" + (ready ? 1 : 0);
    }

    public void LoadState(string s, float away)
    {
        if (string.IsNullOrEmpty(s)) return;
        string[] p = s.Split('|');
        float l; int u, r;
        if (p.Length > 0 && float.TryParse(p[0], out l)) left = l - away;
        if (p.Length > 1 && int.TryParse(p[1], out u)) usesLeft = u;
        if (p.Length > 2 && int.TryParse(p[2], out r)) ready = r == 1;
        if (left <= 0f && !Dry && kind != Kind.Tank) ready = true;
    }

    // ===== أدوات =====
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

    void Notify(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.Show(msg);
    }

    void Warn(string msg)
    {
        if (NotificationManager.Instance != null) NotificationManager.Instance.ShowWarning(msg);
    }
}
