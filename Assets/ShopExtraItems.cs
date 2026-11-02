using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ===== عناصر المتجر الجديدة =====
// رغد تحط صورة كل عنصر (PNG بخلفية شفافة) في:  Assets/Resources/NaaqaItems/
// باسم الـ id بالضبط (مثلًا palm.png)، والعنصر يطلع لحاله:
//   - كرت في المتجر (نسخة من كرت الخيمة)
//   - في الشنطة بعد الشراء
//   - يتركب على الخريطة بالأسهم مثل الخيمة، وينحفظ مكانه
// ما يحتاج تعديل في المشهد. العنصر اللي ما له صورة ما يطلع.
public class ShopExtraItems : MonoBehaviour
{
    public enum Role { Decor, Produce, Mosque }

    public class ItemDef
    {
        public string id, name;
        public int price;
        public float scale;                 // الحجم مقارنة بالخيمة (١ = نفس عرضها)
        public Role role;
        public ItemProducer.Kind kind;      // وش ينتج (لو Produce)
        public int amount;                  // كم كل مرة
        public float every;                 // كل كم ثانية
        public int uses;                    // للبير: كم مرة قبل ما ينضب
        public int unlockLevel;             // يفتح من أي مستوى

        public ItemDef(string id, string name, int price, float scale, int unlockLevel = 1)
        {
            this.id = id; this.name = name; this.price = price; this.scale = scale;
            this.role = Role.Decor; this.unlockLevel = unlockLevel;
        }

        public ItemDef Makes(ItemProducer.Kind k, int amount, float every, int uses = 0)
        {
            role = Role.Produce; kind = k; this.amount = amount; this.every = every; this.uses = uses;
            return this;
        }

        public ItemDef AsMosque() { role = Role.Mosque; once = true; return this; }
        public bool once;                   // تنشرى مرة وحدة بس
        public float worldWidth;            // عرض ثابت بالوحدات (للأشياء الكبيرة مثل الحوش الإضافي)
        public ItemDef Width(float w) { worldWidth = w; return this; }
        public ItemDef Once() { once = true; return this; }
        public bool hidden;                 // ما يطلع في المتجر (مثل كأس السباق: جائزة)
        public ItemDef Hidden() { hidden = true; return this; }
    }

    // ===== القائمة (غيّري الأسعار والأحجام والإنتاج هنا) =====
    // النسخة الأساسية: أغراض قليلة تخدم العزبة وشكلها (المزارع والغنم والبير انشالت)
    public static readonly ItemDef[] Items =
    {
        new ItemDef("feed_sack",   "معلف",            25,  0.22f).Hidden(),   // انشال من المتجر (يبقى لللي عندهم)
        new ItemDef("milk_stall",  "كشك البيع",       80,  0.36f).Once(),   // هدية في الشرح: الزباين يجونه، وتبيع منه مباشرة
        new ItemDef("palm",        "نخلة",            35,  0.32f, GameRules.DecorUnlock),
        new ItemDef("well",        "بير",             GameRules.WellPrice, 0.34f),          // يشيل ٥٠ مويه ويتعبى حبة كل دقيقة (الأول هدية)
        new ItemDef("feed_farm",   "مزرعة علف",       GameRules.ExtraFeedFarmPrice, 0.5f, GameRules.ExtraFeedFarmUnlock),   // الأولى هدية في الشرح
        new ItemDef("date_farm",   "مزرعة تمر",       GameRules.DateFarmPrice, 0.5f, GameRules.DateFarmUnlock),
        new ItemDef("fence",       "سور",             10,   0.28f, GameRules.DecorUnlock),
        // (زاوية السور انشالت من اللعبة، واللي عنده ينرجع له سعرها في SaveManager)
        new ItemDef("dirt_path",   "طريق ترابي",      5,   0.22f).Width(4.4f),   // قطعة = مربعين، وتلتصق باللي جنبها
        new ItemDef("musalla",     "مصلى الطريق",     GameRules.MusallaPrice, 1f, GameRules.MusallaUnlock).Width(21f).Once(),   // جنب الطريق العام
        new ItemDef("trophy",      "كأس السباق",      0,   0.2f).Hidden(),  // جائزة نادرة من السباق (بعد مستوى ٢٠)
        new ItemDef("pen_extra",   "حوش إضافي",      600, 1f).Width(11f),   // السعر والمستوى حسب كم حوش عندك (GameRules.ExtraPenPrice)
    };

    public const string Folder = "NaaqaItems";

    public static ItemDef Get(string id)
    {
        foreach (ItemDef d in Items) if (d.id == id) return d;
        return null;
    }

    // ===== تركيب تلقائي =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Setup();
        SceneManager.sceneLoaded += (s, m) => Setup();
    }

    static void Setup()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) inv = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (inv == null) return;

        // الخيمة هي القالب: شكلها على الخريطة وكرتها في المتجر
        BagItemData tent = inv.GetData("tent");
        ShopItemView tentCard = null;
        foreach (ShopItemView v in FindObjectsByType<ShopItemView>(FindObjectsInactive.Include))
            if (v.itemId == "tent") { tentCard = v; break; }

        if (tent == null || tent.worldPrefab == null || tentCard == null)
        {
            Debug.LogWarning("عناصر المتجر الجديدة: ما لقيت الخيمة (القالب) في المتجر أو الشنطة");
            return;
        }

        SpriteRenderer tentSr = tent.worldPrefab.GetComponentInChildren<SpriteRenderer>(true);
        float tentWidth = tentSr != null && tentSr.sprite != null ? tentSr.sprite.bounds.size.x : 10f;
        Vector2 pivot = tentSr != null && tentSr.sprite != null
            ? new Vector2(tentSr.sprite.pivot.x / tentSr.sprite.rect.width, tentSr.sprite.pivot.y / tentSr.sprite.rect.height)
            : new Vector2(0.5f, 0.5f);

        // مكان مخفي نحط فيه القوالب
        GameObject holder = new GameObject("ExtraItemTemplates");
        holder.SetActive(false);

        int index = tentCard.transform.GetSiblingIndex();
        List<string> missing = new List<string>();

        foreach (ItemDef def in Items)
        {
            if (inv.GetData(def.id) != null) continue;   // موجود من قبل

            Texture2D tex = Resources.Load<Texture2D>(Folder + "/" + def.id);
            if (tex == null) { missing.Add(def.id); continue; }

            float ppu = def.worldWidth > 0f ? tex.width / def.worldWidth : tex.width / Mathf.Max(0.01f, tentWidth * def.scale);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
            sprite.name = def.id;

            // ١. الشكل على الخريطة
            GameObject world = Instantiate(tent.worldPrefab, holder.transform);
            world.name = def.id;
            foreach (TentClick tc in world.GetComponentsInChildren<TentClick>(true)) DestroyImmediate(tc);

            SpriteRenderer sr = world.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Simple;
                sr.sprite = sprite;
            }
            BoxCollider box = world.GetComponentInChildren<BoxCollider>(true);
            if (box != null && sr != null)
            {
                Vector3 size = sprite.bounds.size;
                box.size = new Vector3(size.x, size.y, box.size.z);
                box.center = sprite.bounds.center;
            }
            if (def.role == Role.Produce)
            {
                ItemProducer p = world.AddComponent<ItemProducer>();
                p.kind = def.kind;
                p.itemName = def.name;
                p.amount = def.amount;
                p.every = def.every;
                p.uses = def.uses;
            }
            else if (def.role == Role.Mosque)
            {
                world.AddComponent<MosqueItem>();
            }
            else if (def.id == "majlis")
            {
                world.AddComponent<TapSound>();
            }

            // ٢. الشنطة
            BagItemData data = new BagItemData();
            data.id = def.id;
            data.displayName = def.name;
            data.icon = sprite;
            data.worldPrefab = world;
            inv.catalog.Add(data);

            // ٣. كرت المتجر
            GameObject card = Instantiate(tentCard.gameObject, tentCard.transform.parent);
            card.name = "ShopItem_" + def.id;
            card.transform.SetSiblingIndex(++index);
            ShopItemView view = card.GetComponent<ShopItemView>();
            view.itemId = def.id;
            view.itemName = def.name;
            view.price = def.price;
            view.buyOnce = def.once;
            view.comingSoon = false;
            if (def.unlockLevel > 1)
            {
                LevelLock lk = card.AddComponent<LevelLock>();
                lk.level = def.unlockLevel;
            }

            Transform icon = card.transform.Find("icon");
            if (icon != null)
            {
                Image img = icon.GetComponent<Image>();
                if (img != null) { img.sprite = sprite; img.preserveAspect = true; }
            }
            if (view.buyButton != null)
                view.buyButton.onClick.AddListener(() => { if (SoundManager.Instance != null) SoundManager.Instance.PlayClick(); });
        }

        if (missing.Count > 0)
            Debug.Log("عناصر المتجر بدون صور للحين (حطيها في Assets/Resources/" + Folder + "): " + string.Join(", ", missing));
    }
}
