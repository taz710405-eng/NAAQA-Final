using UnityEngine;
using UnityEngine.UI;

public class SettingsWindowView : MonoBehaviour
{
    public Slider ambientSlider;   // صوت الريح
    public Slider sfxSlider;       // صوت الأزرار

    void Start()
    {
        if (ambientSlider != null)
        {
            ambientSlider.minValue = 0f;
            ambientSlider.maxValue = 1f;
            ambientSlider.onValueChanged.AddListener(v =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetAmbientVolume(v);
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(v =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetSfxVolume(v);
            });
        }
    }

    void OnEnable()
    {
        if (SoundManager.Instance == null) return;
        if (ambientSlider != null) ambientSlider.SetValueWithoutNotify(SoundManager.Instance.ambientVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundManager.Instance.sfxVolume);
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}