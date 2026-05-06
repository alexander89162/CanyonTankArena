using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainAudioMixer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAndApplyAllSettings();
    }

    public void LoadAndApplyAllSettings()
    {
        // Audio
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.9f);

        AudioListener.volume = master;

        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(music, 0.0001f)) * 20f);
            mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sfx, 0.0001f)) * 20f);
        }

        // Graphics
        int quality = PlayerPrefs.GetInt("QualityLevel", 3); // 3 = High by default
        QualitySettings.SetQualityLevel(quality, true);

        Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        Debug.Log("[SettingsManager] Settings loaded and applied");
    }

    // Called from GarageManager when user clicks Apply
    public void SaveAndApplySettings(float masterVol, float musicVol, float sfxVol, 
                                     float sensitivity, int qualityIndex, bool fullscreen, bool invertY)
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVol);
        PlayerPrefs.SetFloat("MusicVolume", musicVol);
        PlayerPrefs.SetFloat("SFXVolume", sfxVol);
        PlayerPrefs.SetFloat("Sensitivity", sensitivity);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);

        PlayerPrefs.Save();

        LoadAndApplyAllSettings(); // Apply immediately
    }

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey("MasterVolume");
        PlayerPrefs.DeleteKey("MusicVolume");
        PlayerPrefs.DeleteKey("SFXVolume");
        PlayerPrefs.DeleteKey("Sensitivity");
        PlayerPrefs.DeleteKey("QualityLevel");
        PlayerPrefs.DeleteKey("Fullscreen");
        PlayerPrefs.DeleteKey("InvertY");

        LoadAndApplyAllSettings();
    }
}