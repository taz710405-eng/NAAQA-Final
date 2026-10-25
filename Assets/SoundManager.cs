using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("الأصوات")]
    public AudioClip ambient;   // desert_wind
    public AudioClip click;     // click
    public AudioClip coins;     // coins

    [Header("مستوى الصوت")]
    [Range(0f, 1f)] public float ambientVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    private AudioSource ambientSource;
    private AudioSource sfxSource;
    private int lastMoney;

    void Awake()
    {
        Instance = this;

        // نرجع آخر مستوى اختاره اللاعب
        ambientVolume = PlayerPrefs.GetFloat("vol_ambient", ambientVolume);
        sfxVolume = PlayerPrefs.GetFloat("vol_sfx", sfxVolume);

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (ambient != null)
        {
            ambientSource.clip = ambient;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include))
            b.onClick.AddListener(PlayClick);

        if (GameManager.Instance != null) lastMoney = GameManager.Instance.money;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        int m = GameManager.Instance.money;
        if (m > lastMoney) Play(coins);
        lastMoney = m;
    }

    // ===== من نافذة الإعدادات =====
    public void SetAmbientVolume(float v)
    {
        ambientVolume = v;
        if (ambientSource != null) ambientSource.volume = v;
        PlayerPrefs.SetFloat("vol_ambient", v);
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = v;
        PlayerPrefs.SetFloat("vol_sfx", v);
    }

    public void PlayClick() { Play(click); }

    public void Play(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }
}