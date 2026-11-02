using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using Unity.Collections;
using TMPro;
using System.Collections.Generic;

// ===== سلالات الإبل =====
// كل سلالة لها صورة (نفس شكل الناقة الأصلية بالضبط، بس اللون يختلف، عشان العظام تشتغل عليها)
// في Assets/Resources/NaaqaCamels/camel_<id>.png، ولها سعر وميزة:
// الخصائص منفصلة (كل الإبل تنتج حليب، ومو كلها تدخل السباق):
//   MilkProductionTime = وقت الحليب (حسب الندرة: GameRules.MilkSeconds)
//   RacingEligible / RaceSpeed / Stamina = السباق
//   beauty (MazayenTraits) = نجوم المزاين
public static class CamelBreeds
{
    public class Breed
    {
        public string id, name, color, use;
        public int price, beauty;
        public float milk, race;          // (milk قديم وما عاد يتحكم بالوقت)
        public float stamina;             // قدرة التحمل في السباق (٠ لين ١)
        public int rarity;          // ٠ عادية، ١ نادرة، ٢ أسطورية (جامدة)
        public int unlockLevel;     // من أي مستوى تنباع
        public Breed(string id, string name, string color, string use, int price, float milk, float race, int beauty, int rarity, int unlockLevel)
        {
            this.id = id; this.name = name; this.color = color; this.use = use; this.price = price;
            this.milk = milk; this.race = race; this.beauty = beauty; this.rarity = rarity; this.unlockLevel = unlockLevel;
        }

        public string RarityName { get { return rarity == 2 ? "أسطورية" : rarity == 1 ? "نادرة" : "عادية"; } }
        public Color RarityColor { get { return rarity == 2 ? new Color32(255, 196, 64, 255) : rarity == 1 ? new Color32(120, 190, 255, 255) : new Color32(214, 200, 180, 255); } }
        // وقت الحليب حسب الندرة (قابل للتعديل من GameRules)
        public float MilkProductionTime { get { return GameRules.MilkTime(rarity); } }
        public int MilkStars { get { return rarity + 1; } }
        // السباق: مو كل السلالات مؤهلة
        public bool RacingEligible { get { return race >= 0.05f; } }
        public float RaceSpeed { get { return race; } }
        public float Stamina { get { return stamina; } }
        public int RaceStars { get { return !RacingEligible ? 0 : race >= 0.08f ? 3 : race >= 0.06f ? 2 : 1; } }
        // وش هي شاطرة فيه
        public string Best
        {
            get
            {
                if (RacingEligible && RaceStars >= MilkStars && RaceStars >= beauty) return "السباق";
                if (beauty >= MilkStars) return "المزاين";
                return "الحليب";
            }
        }
    }

    // ===== كل السلالات (الأسعار غالية، والنادرة والأسطورية تفتح بمستوى أعلى) =====
    public static readonly Breed[] All =
    {
        //            id            الاسم          اللون                    الاستخدام                           السعر  حليب   سباق   جمال ندرة مستوى
        new Breed("sahiliyat",  "الساحليات",    "تميل للأحمر",          "حليبها وافر، وتصبر على الحر",       120,  0.85f, 0.01f, 1, 0, 1),
        new Breed("humr",       "الحمر",        "أحمر أو بني محمر",     "قوية وحليبها زين",                 140,  0.9f,  0.01f, 2, 0, 1),
        new Breed("awarik",     "العوارك",      "بيضاء أو فاتحة",       "متعددة الاستخدامات، حليب متوسط",   160,  0.85f, 0.02f, 1, 0, 1),
        new Breed("sufr",       "الصفر",        "أصفر ذهبي",            "لونها يشرح الصدر وحليبها زين",     180,  0.8f,  0.00f, 2, 0, 1),
        new Breed("majaheem",   "المجاهيم",     "سوداء أو داكنة",       "مشهورة بالحليب الوافر",            200,  0.6f,  0.00f, 2, 0, 1),
        new Breed("shaqh",      "الشقح",        "بني فاتح أو رملي",     "حليب، وتدخل المزاين",              240,  0.85f, 0.01f, 2, 0, 3),
        new Breed("shaal",      "الشعل",        "بني داكن مائل للرمادي", "حليب، وتدخل المزاين",             260,  0.85f, 0.01f, 2, 0, 3),
        new Breed("hurra",      "الحرة",        "بني متوسط",            "بنية قوية وقدرة على الجري",        500,  0.95f, 0.06f, 2, 1, 8),
        new Breed("wadh",       "الوضح",        "بيضاء أو كريمية",      "مشهورة بالجمال والمزاين",          600,  0.9f,  0.00f, 3, 1, 8),
        new Breed("asayel",     "الأصايل",      "أصفر إلى بني",         "معروفة في سباقات الهجن",           750,  1.0f,  0.08f, 1, 1, 10),
        new Breed("shaqh_race", "شقح السباق",   "فاتحة أو قمحية",       "للسباقات حسب النسب والأداء",       900,  1.0f,  0.05f, 2, 1, 12),
        new Breed("sudani",     "السودانيات",   "رملي رمادي",           "سريعة في الهجن، نادرة جدًا",       1800, 1.0f,  0.07f, 2, 2, 18),
        new Breed("omani",      "العمانيات",    "قمحي فاتح",            "أسرع الهجن وأغلاها، أسطورية",      2200, 0.95f, 0.10f, 3, 2, 22),
    };

    // قدرة التحمل لسلالات السباق
    static CamelBreeds()
    {
        foreach (Breed b in All)
            b.stamina = b.id == "omani" ? 0.9f : b.id == "sudani" ? 0.85f : b.id == "hurra" ? 0.8f : b.id == "shaqh_race" ? 0.75f : b.id == "asayel" ? 0.7f : 0f;
    }

    // كل السلالات تنباع في سوق الإبل (مرتبة من الأرخص للأغلى)
    public static string[] Market
    {
        get
        {
            string[] ids = new string[All.Length];
            for (int i = 0; i < All.Length; i++) ids[i] = All[i].id;
            return ids;
        }
    }

    // كم ناقة عند اللاعب من هالسلالة (الإبل الأصلية في الحوش من الحرة)
    public static int CountOf(string breedId)
    {
        int n = 0;
        extra.RemoveAll(o => o.camel == null);
        foreach (Owned o in extra) if (o.breed == breedId) n++;
        if (breedId == StarterBreed)
            foreach (Camel c in Object.FindObjectsByType<Camel>(FindObjectsInactive.Exclude))
                if (!c.name.StartsWith("Breed_")) n++;
        return n;
    }

    // سلالة ناقة معيّنة
    public static string BreedOf(Camel c)
    {
        if (c == null) return StarterBreed;
        extra.RemoveAll(o => o.camel == null);
        foreach (Owned o in extra) if (o.camel == c) return o.breed;
        return StarterBreed;
    }

    public const string StarterBreed = "hurra";   // الإبل اللي في الحوش من البداية لونها أقرب للحرة
    public const int MaxCamels = GameRules.PenMaxLevel;

    // كم ناقة يشيل الحوش الحين (حسب مستوى الحوش)
    public static int Capacity { get { return FarmStore.Instance != null ? FarmStore.Instance.CamelCap : MaxCamels; } }

    public static Breed Get(string id)
    {
        foreach (Breed b in All) if (b.id == id) return b;
        return null;
    }

    // ===== الإبل اللي في الحوش =====
    public class Owned { public Camel camel; public string breed; }
    static readonly List<Owned> extra = new List<Owned>();   // اللي انشرت من السوق
    static int counter = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { extra.Clear(); counter = 0; }

    public static List<string> ExtraBreeds()
    {
        List<string> l = new List<string>();
        extra.RemoveAll(o => o.camel == null);
        foreach (Owned o in extra) l.Add(o.breed);
        return l;
    }

    // السلالات اللي عند اللاعب (بدون تكرار)، أولها إبل الحوش الأصلية
    public static List<string> OwnedBreeds()
    {
        List<string> l = new List<string> { StarterBreed };
        foreach (string b in ExtraBreeds()) if (!l.Contains(b)) l.Add(b);
        return l;
    }

    public static int CamelCount()
    {
        int n = 0;
        foreach (Camel c in Object.FindObjectsByType<Camel>(FindObjectsInactive.Exclude)) n++;
        return n;
    }

    // أفضل سرعة سباق عند اللاعب
    public static Breed BestRacer()
    {
        Breed best = Get(StarterBreed);
        extra.RemoveAll(o => o.camel == null);
        foreach (Owned o in extra)
        {
            Breed b = Get(o.breed);
            if (b != null && (best == null || b.race > best.race)) best = b;
        }
        return best;
    }

    // ===== شراء =====
    public static string Buy(string breedId)
    {
        Breed b = Get(breedId);
        if (b == null) return "سلالة غير معروفة";
        int lv = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        if (lv < b.unlockLevel)
            return b.name + " تنباع من المستوى " + NaaqaUI.Ar(b.unlockLevel);
        if (CamelCount() >= Capacity)
            return "العزبة ممتلئة! أحواشك تشيل " + NaaqaUI.Ar(Capacity) + " إبل بس، طوّر حوشك أو اشترِ حوش إضافي";

        GameManager gm = GameManager.Instance;
        if (gm == null) return "صار خطأ";
        if (gm.money < b.price) return "ما عندك قروش كافية! تحتاج " + NaaqaUI.Ar(b.price) + " قرش";
        if (!gm.SpendMoney(b.price)) return "ما عندك قروش كافية!";
        if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();

        if (Spawn(breedId) == null)
        {
            gm.AddMoney(b.price);   // نرجّع الفلوس لو ما قدرنا
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
            return "ما قدرنا نضيف الناقة للحوش";
        }
        LevelManager.AddXp(LevelManager.XpPerPurchase);
        Quests.Add("camel");
        return null;
    }

    // ناقة جديدة بنفس الناقة الأولى، وبلون السلالة
    public static Camel Spawn(string breedId)
    {
        Camel template = null;
        foreach (Camel c in Object.FindObjectsByType<Camel>(FindObjectsInactive.Exclude))
            if (!c.name.StartsWith("Breed_")) { template = c; break; }
        if (template == null) return null;

        Vector3 pos = template.transform.position + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-1.5f, 1.5f));
        GameObject go = Object.Instantiate(template.gameObject, pos, template.transform.rotation, template.transform.parent);
        counter++;
        go.name = "Breed_" + breedId + "_" + counter;
        Camel camel = go.GetComponent<Camel>();

        // كل ناقة لها عدّاد خاص فوقها
        if (template.timerBadge != null)
        {
            GameObject badge = Object.Instantiate(template.timerBadge, template.timerBadge.transform.parent);
            badge.name = go.name + "_Timer";
            badge.SetActive(false);
            camel.timerBadge = badge;
            camel.timerText = badge.GetComponentInChildren<TMP_Text>(true);
        }
        else if (template.timerText != null)
        {
            GameObject t = Object.Instantiate(template.timerText.gameObject, template.timerText.transform.parent);
            t.SetActive(false);
            camel.timerText = t.GetComponent<TMP_Text>();
        }

        ApplyBreed(camel, breedId);
        extra.Add(new Owned { camel = camel, breed = breedId });
        return camel;
    }

    public static void ClearExtra()
    {
        foreach (Owned o in extra)
        {
            if (o.camel == null) continue;
            if (o.camel.timerBadge != null) Object.Destroy(o.camel.timerBadge);
            o.camel.gameObject.name = "_removed";
            o.camel.gameObject.SetActive(false);
            Object.Destroy(o.camel.gameObject);
        }
        extra.Clear();
        counter = 0;
    }

    // ===== اللون والميزة =====
    public static void ApplyBreed(Camel camel, string breedId)
    {
        Breed b = Get(breedId);
        if (camel == null || b == null) return;
        camel.productionTime = b.MilkProductionTime;

        Texture2D tex = Resources.Load<Texture2D>("NaaqaCamels/camel_" + breedId);
        if (tex == null) { Debug.LogWarning("ما لقيت صورة السلالة " + breedId); return; }

        foreach (SpriteRenderer sr in camel.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Sprite o = sr.sprite;
            if (o == null || o.texture == null) continue;
            if (o.texture.width != tex.width || o.texture.height != tex.height) continue;   // جسم الناقة بس
            Sprite s = CloneWithBones(o, tex);
            if (s != null)
            {
                sr.sprite = s;
                Behaviour skin = sr.GetComponent("SpriteSkin") as Behaviour;
                if (skin != null) { skin.enabled = false; skin.enabled = true; }
            }
        }
    }

    // نسخة من صورة الناقة بنفس العظام والأوزان، بس بصورة السلالة
    static Sprite CloneWithBones(Sprite o, Texture2D tex)
    {
        try
        {
            Vector2 pivot = new Vector2(o.pivot.x / o.rect.width, o.pivot.y / o.rect.height);
            Sprite s = Sprite.Create(tex, o.rect, pivot, o.pixelsPerUnit, 0, SpriteMeshType.FullRect, o.border);
            s.name = o.name + "_breed";

            int n = o.GetVertexCount();
            if (n <= 0) return s;
            s.SetVertexCount(n);

            Vector3[] pos = o.GetVertexAttribute<Vector3>(VertexAttribute.Position).ToArray();
            using (NativeArray<Vector3> a = new NativeArray<Vector3>(pos, Allocator.Temp))
                s.SetVertexAttribute(VertexAttribute.Position, a);

            if (o.HasVertexAttribute(VertexAttribute.TexCoord0))
            {
                Vector2[] uv = o.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0).ToArray();
                using (NativeArray<Vector2> a = new NativeArray<Vector2>(uv, Allocator.Temp))
                    s.SetVertexAttribute(VertexAttribute.TexCoord0, a);
            }

            if (o.HasVertexAttribute(VertexAttribute.BlendWeight))
            {
                BoneWeight[] w = o.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight).ToArray();
                using (NativeArray<BoneWeight> a = new NativeArray<BoneWeight>(w, Allocator.Temp))
                    s.SetVertexAttribute(VertexAttribute.BlendWeight, a);
            }

            ushort[] idx = o.GetIndices().ToArray();
            using (NativeArray<ushort> a = new NativeArray<ushort>(idx, Allocator.Temp))
                s.SetIndices(a);

            s.SetBones(o.GetBones());
            Matrix4x4[] bp = o.GetBindPoses().ToArray();
            using (NativeArray<Matrix4x4> a = new NativeArray<Matrix4x4>(bp, Allocator.Temp))
                s.SetBindPoses(a);

            return s;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ما قدرنا ننسخ عظام الناقة للسلالة الجديدة: " + e.Message);
            return null;
        }
    }
}
