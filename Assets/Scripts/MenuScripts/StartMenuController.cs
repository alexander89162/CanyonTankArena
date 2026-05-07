using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;

public class StartMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;

    // Panels
    private VisualElement settingsPanel;
    private VisualElement creditsPanel;

    // Settings UI
    private Slider masterSlider, musicSlider, sfxSlider;
    private Button applyButton, resetButton, closeSettingsButton;

    [SerializeField] private GameObject loadingScreen;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Main Buttons
        var playButton = root.Q<Button>("Play_Btn");
        var settingsButton = root.Q<Button>("Settings_Btn");
        var creditsButton = root.Q<Button>("Credits_Btn");
        var quitButton = root.Q<Button>("Quit_Btn");

        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked += OnSettingsClicked;
        if (creditsButton != null) creditsButton.clicked += OnCreditsClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;

        SetupSettingsPanel();
        SetupCreditsPanel();
    }

    private IEnumerator FadeIn(VisualElement panel, float duration = 0.3f)
    {
        panel.style.display = DisplayStyle.Flex;
        panel.style.opacity = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            panel.style.opacity = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        panel.style.opacity = 1f;
    }

    private IEnumerator FadeOut(VisualElement panel, float duration = 0.25f)
    {
        float elapsed = 0f;
        float startOpacity = panel.style.opacity.value;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            panel.style.opacity = Mathf.Lerp(startOpacity, 0f, t);
            yield return null;
        }

        panel.style.display = DisplayStyle.None;
        panel.style.opacity = 0f;
    }

    private void SetupSettingsPanel()
    {
        settingsPanel = root.Q<VisualElement>("content-settings");

        masterSlider = root.Q<Slider>("slider-master");
        musicSlider = root.Q<Slider>("slider-music");
        sfxSlider = root.Q<Slider>("slider-sfx");

        applyButton = root.Q<Button>("btn-apply");
        resetButton = root.Q<Button>("btn-reset");
        closeSettingsButton = root.Q<Button>("btn-close");

        if (applyButton != null) applyButton.clicked += ApplySettings;
        if (resetButton != null) resetButton.clicked += ResetToDefaults;
        if (closeSettingsButton != null) closeSettingsButton.clicked += () => StartCoroutine(FadeOut(settingsPanel));

        LoadSettingsIntoUI();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            StartCoroutine(FadeIn(settingsPanel));
            LoadSettingsIntoUI();
        }
    }

    private void CloseSettings() => StartCoroutine(FadeOut(settingsPanel));

    private void SetupCreditsPanel()
    {
        creditsPanel = root.Q<VisualElement>("content-credits");
        var closeBtn = root.Q<Button>("btn-close-credits");

        if (closeBtn != null)
            closeBtn.clicked += () => StartCoroutine(FadeOut(creditsPanel));
    }

    private void OnCreditsClicked()
    {
        if (creditsPanel != null)
            StartCoroutine(FadeIn(creditsPanel));
    }

    private void LoadSettingsIntoUI()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
    }

    private void ApplySettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveAndApplySettings(
                masterSlider.value, musicSlider.value, sfxSlider.value,
                1.5f, 3, true, false);
        }
    }

    private void ResetToDefaults()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ResetToDefaults();

        LoadSettingsIntoUI();
    }

    private void OnPlayClicked() => StartCoroutine(LoadGameWithScreen());

    private System.Collections.IEnumerator LoadGameWithScreen()
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        SaveManager.Instance.LoadGame();
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}