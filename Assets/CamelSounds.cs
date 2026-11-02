using UnityEngine;
using System.Collections.Generic;

// ===== أصوات إضافية =====
// أي صوت في Assets/Resources/NaaqaSounds باسمه يشتغل لحاله في مكانه:
//   camel  = صوت الناقة لما تاكل
//   pour   = صب القهوة (فتح السوق)
//   fire   = النار (الضغط على القعدة)
//   crowd  = تشجيع الجماهير (بداية السباق)
//   adhan  = الأذان (وقت الصلاة)
// لو الملف مو موجود، ما يصير شي.
public static class CamelSounds
{
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> last = new Dictionary<string, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { cache.Clear(); last.Clear(); }

    public static void Play(string name, float minGap = 1.2f)
    {
        float lastTime;
        if (last.TryGetValue(name, out lastTime) && Time.unscaledTime - lastTime < minGap) return;

        AudioClip clip;
        if (!cache.TryGetValue(name, out clip))
        {
            clip = Resources.Load<AudioClip>("NaaqaSounds/" + name);
            cache[name] = clip;
        }
        if (clip == null || SoundManager.Instance == null) return;

        last[name] = Time.unscaledTime;
        SoundManager.Instance.Play(clip);
    }
}
