// ===== أرقام اللعبة في مكان واحد (نظام الاقتصاد النهائي) =====
// كل الأسعار والأوقات والمكافآت هنا، وتفاصيلها في NAAQA_ECONOMY_SYSTEM_FINAL.md
// العملة الوحيدة: القروش.
public static class GameRules
{
    // ===== متى تفتح الأشياء (مستوى اللاعب) =====
    public const int DecorUnlock = 2;          // الزينة (نخل، سور)
    public const int FarmUpgradeUnlock = 5;    // تطوير المزارع
    public const int DateFarmUnlock = 6;       // مزرعة التمر
    public const int ExtraFeedFarmUnlock = 8;  // مزرعة علف إضافية
    public const int ExtraPen1Unlock = 8;      // الحوش الإضافي الأول
    public const int RaceUnlock = 9;           // سباق الهجن
    public const int MusallaUnlock = 12;       // مصلى الطريق
    public const int WorkersUnlock = 13;       // العمال (الحلابين)
    public const int ExtraPen2Unlock = 14;     // الحوش الإضافي الثاني
    public const int MazayenUnlock = 15;       // مهرجان المزاين
    public const int TrophyLevel = 20;         // كأس السباق

    // نسبة الفوز الأساسية (تتغير حسب خصائص الناقة)
    public const float RaceOdds1 = 0.60f;
    public const float MazayenOdds1 = 0.55f;

    // ===== السباق والمزاين =====
    public const int RaceFee = 5, RaceWin = 12, RaceLose = 3;
    public const int MazayenFee = 8, MazayenWin = 18, MazayenLose = 5;

    // ===== إنتاج الحليب (ثواني) حسب ندرة الناقة: ٠ شائعة، ١ نادرة، ٢ أسطورية =====
    public static readonly float[] MilkSeconds = { 30f, 25f, 20f };
    public static float MilkTime(int rarity) { return MilkSeconds[UnityEngine.Mathf.Clamp(rarity, 0, 2)]; }

    // ===== البيع في الكشك =====
    public const int MilkPrice = 6;       // للحبة
    public const int DatePrice = 4;       // للحبة
    public const int TipMax = 2;          // بخشيش ٠–٢ للطلب المكتمل

    // ===== العلف =====
    public static readonly int[] FeedPackAmount = { 10, 25, 50 };
    public static readonly int[] FeedPackPrice = { 15, 35, 65 };

    // ===== المزارع =====
    public static readonly float[] FeedFarmSeconds = { 180f, 150f, 120f };   // مستوى ١، ٢، ٣
    public const float DateFarmSeconds = 300f;                                // ٥ دقايق
    public const int FarmWaterPerCycle = 5;
    public static int FarmUpgradeCost(int toLevel) { return toLevel == 2 ? 150 : 300; }
    public const int DateFarmPrice = 350;
    public const int ExtraFeedFarmPrice = 250;

    // ===== المويه =====
    public const int WellCapacity = 50;
    public const float WellRefillSeconds = 60f;    // كل بير يعبي حبة كل دقيقة
    public const int WellPrice = 150;
    public static readonly int[] TruckTons = { 10, 25, 50 };
    public static readonly int[] TruckPrices = { 10, 22, 40 };

    // ===== الأحواش =====
    // الحوش الرئيسي: من ١ لين ٥، كل مستوى ناقة
    public const int PenMaxLevel = 5;
    public static int PenUpgradeCost(int toLevel) { return toLevel == 2 ? 100 : toLevel == 3 ? 180 : toLevel == 4 ? 280 : 420; }
    public static int CamelsForPen(int penLevel) { return UnityEngine.Mathf.Clamp(penLevel, 1, PenMaxLevel); }
    // الأحواش الإضافية: يبدأ بناقتين، ويتطور لين ٥ (مستوى ١ = ٢ إبل ... مستوى ٤ = ٥ إبل)
    public const int ExtraPenMaxLevel = 4;
    public const int MaxExtraPens = 2;             // ٣ أحواش كلها
    public static int ExtraPenCapacity(int level) { return UnityEngine.Mathf.Clamp(level, 1, ExtraPenMaxLevel) + 1; }
    public static int ExtraPenUpgradeCost(int toLevel) { return toLevel == 2 ? 200 : toLevel == 3 ? 350 : 550; }
    public static int ExtraPenPrice(int owned) { return owned <= 0 ? 600 : 900; }
    public static int ExtraPenUnlock(int owned) { return owned <= 0 ? ExtraPen1Unlock : ExtraPen2Unlock; }

    // ===== المصلى =====
    public const int MusallaPrice = 1000;
    public const float MusallaCustomerBoost = 1.15f;    // الزباين يجون أكثر بـ١٥٪
    public const float TravelerMin = 900f, TravelerMax = 1200f;   // مسافر كل ١٥–٢٠ دقيقة

    // ===== العمال =====
    public const int WorkerPrice = 900;
    public const int WorkerFeePerMilk = 1;
    public const float WorkerOfflineMaxHours = 8f;

    // ===== المساعدة الطارئة =====
    public const int AidCoinsBelow = 5;
    public const int AidFeed = 5, AidWater = 5, AidCoins = 10;
    public const double AidCooldownHours = 72;

    // ===== المكافأة اليومية =====
    public static readonly int[] DailyRewards = { 15, 20, 25, 30, 35, 45, 80 };

    // رسالة لما يوصل مستوى يفتح فيه شي
    public static string UnlockMessage(int level)
    {
        switch (level)
        {
            case DecorUnlock: return "انفتحت الزينة في السوق الشعبي! نخل وسور لعزبتك";
            case FarmUpgradeUnlock: return "صار تقدر تطوّر مزارعك!";
            case DateFarmUnlock: return "انفتحت مزرعة التمر في السوق الشعبي";
            case ExtraPen1Unlock: return "انفتحت الإبل النادرة، وحوش إضافي، ومزرعة علف ثانية!";
            case RaceUnlock: return "انفتح سباق الهجن! دخّل ناقتك الشوط";
            case MusallaUnlock: return "انفتح مصلى الطريق في السوق الشعبي";
            case WorkersUnlock: return "انفتح التوظيف! وظّف حلاب يشتغل عنك";
            case ExtraPen2Unlock: return "انفتح الحوش الإضافي الثاني!";
            case MazayenUnlock: return "انفتح مهرجان المزاين! ورّهم جمال ناقتك";
            case TrophyLevel: return "صار ممكن تفوز بكأس السباق النادر!";
        }
        return null;
    }
}
