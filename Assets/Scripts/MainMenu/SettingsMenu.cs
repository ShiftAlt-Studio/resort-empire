using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Toggle vibrationToggle;

    const string MusicParam = "MusicVolume";
    const string SfxParam = "SFXVolume";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string VibrationKey = "settings.vibration";

    public static bool VibrationEnabled => PlayerPrefs.GetInt(VibrationKey, 1) == 1;

    void Awake()
    {
        musicSlider.minValue = 0.0001f; musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0.0001f; sfxSlider.maxValue = 1f;

        float music = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        float sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f);

        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        vibrationToggle.SetIsOnWithoutNotify(VibrationEnabled);

        ApplyVolume(MusicParam, music);
        ApplyVolume(SfxParam, sfx);

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }

    void OnMusicChanged(float v)
    {
        ApplyVolume(MusicParam, v);
        PlayerPrefs.SetFloat(MusicKey, v);
    }

    void OnSfxChanged(float v)
    {
        ApplyVolume(SfxParam, v);
        PlayerPrefs.SetFloat(SfxKey, v);
    }

    void OnVibrationChanged(bool on)
    {
        PlayerPrefs.SetInt(VibrationKey, on ? 1 : 0);
    }

    void ApplyVolume(string param, float linear)
    {
        float db = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(param, db);
    }

    void OnDisable() { PlayerPrefs.Save(); }
    void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
}