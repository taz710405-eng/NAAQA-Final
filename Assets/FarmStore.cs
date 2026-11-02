using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;

// ===== مخزن الحوش: المنتجات والماء والتجارة =====
// التمر والترنج (من المزارع، وتنباع في السوق)، والماء (من البير والخزان)،
// ومستوى بسطة اللاعب في السوق، وعدد مرات الفوز في السباقات.
public class FarmStore : MonoBehaviour
{
    public static FarmStore Instance;
    public static System.Action OnChanged;

    public int Dates { get; private set; }
    public int Citron { get; private set; }
    public int Water { get; private set; }
    public int WaterCap { get; private set; }     // يكبر لما يشتري خزان
    public int SouqLevel { get; private set; }    // مستوى البسطة في السوق
    public int RaceWins { get; private set; }
    public int MazayenWins { get; private set; }
    public bool HasTrough { get; private set; }    // حوض الماء (يتركب على الحوش لحاله ويسرّع الحليب)
    public int PenLevelSaved { get; private set; } // مستوى الحوش (٢ بعد تطوير الشرح)

    // مستوى الحوش الحقيقي: قبل تطوير الشرح ١، وبعده ٢ على الأقل
    public int PenLevel
    {
        get
        {
            OrderManager om = FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
            bool tut = om != null && om.IsUpgraded;
            return tut ? Mathf.Clamp(Mathf.Max(2, PenLevelSaved), 2, GameRules.PenMaxLevel) : 1;
        }
    }
    public int CamelCap { get { return GameRules.CamelsForPen(PenLevel) + ExtraPen.TotalCap(); } }

    // الأحواش الإضافية المتركبة
    public int ExtraPens
    {
        get
        {
            if (PlacementManager.Instance == null) return 0;
            int n = 0;
            foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced()) if (p.id == "pen_extra") n++;
            return n;
        }
    }

    public const int TankCapacity = 40;           // كل خزان يزيد السعة
    public const int BaseCapWithWell = 15;        // سعة الماء لو عنده بير بس


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Attach();
        SceneManager.sceneLoaded += (s, m) => Attach();
    }

    static void Attach()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<TaskManager>(FindObjectsInactive.Include) == null) return;
        Instance = new GameObject("FarmStore").AddComponent<FarmStore>();
    }

    void Awake() { SouqLevel = 1; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    // عدادات الذهب والماء صارت في HudSkin (نفس شكل القروش والحليب)
    void Start() { }

    // ===== التعديل =====
    public void AddDates(int n) { Dates = Mathf.Max(0, Dates + n); Changed(); }
    public void AddCitron(int n) { Citron = Mathf.Max(0, Citron + n); Changed(); }

    // يرجع كم ماء انضاف فعلًا (ما يتعدى السعة)
    public int AddWater(int n)
    {
        int before = Water;
        Water = Mathf.Clamp(Water + n, 0, Mathf.Max(WaterCap, 0));
        Changed();
        return Water - before;
    }

    public bool UseWater(int n)
    {
        if (Water < n) return false;
        Water -= n;
        Changed();
        return true;
    }

    public void SetWaterCap(int cap) { WaterCap = Mathf.Max(0, cap); Water = Mathf.Min(Water, WaterCap); Changed(); }
    public void SetSouqLevel(int lvl) { SouqLevel = Mathf.Max(1, lvl); Changed(); }
    public void AddRaceWin() { RaceWins++; Changed(); }
    public void AddMazayenWin() { MazayenWins++; Changed(); }
    public void RestoreMazayen(int wins) { MazayenWins = Mathf.Max(0, wins); }

    // (الذهب انشال: القروش هي العملة الوحيدة)

    public void SetTrough(bool on) { HasTrough = on; Changed(); }

    public void SetPenLevel(int lvl) { PenLevelSaved = Mathf.Clamp(lvl, 1, GameRules.PenMaxLevel); Changed(); }

    public void RestoreExtra(int penLevel, bool trough)
    {
        HasTrough = trough;
        PenLevelSaved = Mathf.Clamp(penLevel, 1, GameRules.PenMaxLevel);
        Changed();
    }

    // سعة المويه = عدد الآبار المتركبة × ٥٠
    public const int WellCapacity = GameRules.WellCapacity;
    public void RecalcWaterCap()
    {
        if (PlacementManager.Instance == null) return;
        int wells = 0;
        foreach (PlacementManager.PlacedInfo p in PlacementManager.Instance.GetPlaced()) if (p.id == "well") wells++;
        int cap = wells * WellCapacity;
        if (cap != WaterCap) SetWaterCap(cap);
    }

    // من الحفظ
    public void Restore(int dates, int citron, int water, int cap, int souq, int wins)
    {
        Dates = Mathf.Max(0, dates);
        Citron = Mathf.Max(0, citron);
        WaterCap = Mathf.Max(0, cap);
        Water = Mathf.Clamp(water, 0, WaterCap);
        SouqLevel = Mathf.Max(1, souq);
        RaceWins = Mathf.Max(0, wins);
        Changed();
    }

    void Changed()
    {
        Refresh();
        if (OnChanged != null) OnChanged();
    }

    void Refresh() { }
}
