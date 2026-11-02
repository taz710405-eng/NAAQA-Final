using UnityEngine;
using System.Collections.Generic;

// ===== أسماء الإبل وجنسها =====
// كل ناقة لها اسم (اللاعب يقدر يغيّره) وينحفظ مع الحساب. (نظام ذكر وأنثى انشال: كلها نوق)
public static class CamelProfiles
{
    [System.Serializable] public class Entry { public string key; public string name; public bool male; }

    static readonly Dictionary<string, Entry> map = new Dictionary<string, Entry>();

    static readonly string[] FemaleNames = { "غزالة", "مزنة", "وضحى", "شعلة", "نوره", "ريم", "هيفاء", "الجازي", "سمحة", "عفرا", "نجلا", "شيخة", "دانة", "مهرة" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { map.Clear(); }

    static string Key(Camel c) { return c != null ? c.gameObject.name : ""; }

    public static Entry Get(Camel c)
    {
        string k = Key(c);
        Entry e;
        if (map.TryGetValue(k, out e)) return e;
        // أول مرة: جنس واسم عشوائي (أغلبها نوق)
        e = new Entry { key = k, male = false };
        string[] pool = FemaleNames;
        e.name = pool[Random.Range(0, pool.Length)];
        map[k] = e;
        return e;
    }

    public static void Rename(Camel c, string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return;
        if (name.Length > 16) name = name.Substring(0, 16);
        Get(c).name = name;
    }

    // ===== الحفظ =====
    public static List<Entry> Save() { return new List<Entry>(map.Values); }

    public static void Restore(List<Entry> saved)
    {
        map.Clear();
        if (saved == null) return;
        foreach (Entry e in saved) if (e != null && !string.IsNullOrEmpty(e.key)) map[e.key] = e;
    }
}
