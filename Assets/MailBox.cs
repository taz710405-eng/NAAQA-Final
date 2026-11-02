using UnityEngine;
using System.Collections.Generic;

// ===== البريد =====
// للهدايا بس: الهدية اليومية وأي هدية قروش أو ذهب، يستلمها اللاعب من هنا
// (ويجيه إشعار إن فيه هدية).
public static class MailBox
{
    [System.Serializable]
    public class Letter
    {
        public string text;
        public long time;
        public int giftCoins;     // هدية (قروش) تنستلم مرة وحدة
        public int giftGold;
        public bool claimed;
        public bool read;
    }

    public const int Max = 40;
    static readonly List<Letter> letters = new List<Letter>();
    public static long LastGiftDay = 0;     // آخر يوم وصلت فيه الهدية اليومية
    public static System.Action OnChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { letters.Clear(); LastGiftDay = 0; }

    public static List<Letter> All { get { return letters; } }

    public static int Unread
    {
        get
        {
            int n = 0;
            foreach (Letter l in letters) if (!l.read || (HasGift(l) && !l.claimed)) n++;
            return n;
        }
    }

    public static bool HasGift(Letter l) { return l.giftCoins > 0; }   // القروش بس (الذهب انشال)

    public static void Add(string text, int giftCoins = 0, int giftGold = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        // البريد صار للهدايا بس (الإشعارات العادية تطلع فوق وتمشي)
        if (giftCoins <= 0) return;
        // نفس الرسالة توها وصلت؟ لا نكررها
        if (letters.Count > 0 && letters[0].text == text && giftCoins == 0 && giftGold == 0 && Now() - letters[0].time < 30) return;

        letters.Insert(0, new Letter { text = text, time = Now(), giftCoins = giftCoins, giftGold = giftGold });
        // نشيل القديم (بس ما نشيل هدية ما انستلمت)
        for (int i = letters.Count - 1; i >= 0 && letters.Count > Max; i--)
            if (!HasGift(letters[i]) || letters[i].claimed) letters.RemoveAt(i);
        Changed();
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.Show("جاتك هدية في البريد! افتحه واستلمها");
    }

    public static void MarkAllRead()
    {
        bool any = false;
        foreach (Letter l in letters) if (!l.read) { l.read = true; any = true; }
        if (any) Changed();
    }

    public static bool Claim(Letter l)
    {
        if (l == null || l.claimed || !HasGift(l)) return false;
        l.claimed = true;
        l.read = true;
        GameManager gm = GameManager.Instance;
        if (gm != null && l.giftCoins > 0)
        {
            gm.AddMoney(l.giftCoins);
            if (MoneyManager.Instance != null) MoneyManager.Instance.UpdateMoneyText();
        }
        Changed();
        return true;
    }

    // الهدية اليومية (تنادى بعد تحميل الحفظ)
    public static void CheckDailyGift()
    {
        // صارت مكافأة الدخول اليومي (DailyReward) بدالها
    }

    // ===== الحفظ =====
    public static List<Letter> Save() { return new List<Letter>(letters); }

    public static void Restore(List<Letter> saved, long lastGiftDay)
    {
        letters.Clear();
        if (saved != null)
            foreach (Letter l in saved) if (l != null && HasGift(l)) letters.Add(l);   // الرسائل القديمة العادية تنشال
        LastGiftDay = lastGiftDay;
        Changed();
    }

    public static string Ago(long t)
    {
        long s = Mathf.Max(0, (int)(Now() - t));
        if (s < 60) return "الحين";
        if (s < 3600) return "قبل " + NaaqaUI.Ar((int)(s / 60)) + " دقيقة";
        if (s < 86400) return "قبل " + NaaqaUI.Ar((int)(s / 3600)) + " ساعة";
        return "قبل " + NaaqaUI.Ar((int)(s / 86400)) + " يوم";
    }

    static void Changed() { if (OnChanged != null) OnChanged(); }
    static long Now() { return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
}
