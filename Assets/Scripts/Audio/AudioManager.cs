using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainAudioMixer;

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float crossfadeDuration = 1.5f;

    [Header("Music Clips")]
    [SerializeField] private AudioClip startMenuMusic;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip arenaMusic;

    private string currentSceneName;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (SettingsManager.Instance == null)
        {
            GameObject settingsGO = new GameObject("SettingsManager");
            settingsGO.AddComponent<SettingsManager>();
        }

        SetupAudioSources();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 1f;

        if (mainAudioMixer != null)
        {
            var groups = mainAudioMixer.FindMatchingGroups("Music");
            if (groups.Length > 0)
                musicSource.outputAudioMixerGroup = groups[0];
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentSceneName) return;

        currentSceneName = scene.name;
        PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string sceneName)
    {
        AudioClip newClip = GetMusicForScene(sceneName);
        if (newClip == null) return;

        if (musicSource.clip == newClip && musicSource.isPlaying)
            return;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(CrossfadeToNewMusic(newClip));
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        string lower = sceneName.ToLower();
        if (lower.Contains("start")) return startMenuMusic;
        if (lower.Contains("garage") || lower.Contains("mainmenu")) return mainMenuMusic;
        if (lower.Contains("arena") || lower.Contains("demo")) return arenaMusic;

        return null;
    }

    private IEnumerator CrossfadeToNewMusic(AudioClip newClip)
    {
        float startVolume = musicSource.volume;

        // Fade Out
        for (float t = 0; t < 1f; t += Time.deltaTime / crossfadeDuration)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.Stop();

        // Play new music
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade In
        for (float t = 0; t < 1f; t += Time.deltaTime / crossfadeDuration)
        {
            musicSource.volume = Mathf.Lerp(0f, startVolume, t);
            yield return null;
        }

        musicSource.volume = startVolume;
        Debug.Log($"[AudioManager] Playing: {newClip.name}");
    }

    // ====================== VOLUME CONTROLS ======================
    public void SetMasterVolume(float volume) => AudioListener.volume = volume;

    public void SetMusicVolume(float volume)
    {
        if (mainAudioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            mainAudioMixer.SetFloat("MusicVolume", dB);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (mainAudioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            mainAudioMixer.SetFloat("SFXVolume", dB);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}