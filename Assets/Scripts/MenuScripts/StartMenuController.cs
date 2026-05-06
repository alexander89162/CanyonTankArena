using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StartMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement settingsPanel;

    // Settings UI elements
    private Slider masterSlider, musicSlider, sfxSlider;
    private Button applyButton, resetButton, closeButton;

    [SerializeField] private GameObject loadingScreen;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Main Menu Buttons
        var playButton = root.Q<Button>("Play_Btn");
        var settingsButton = root.Q<Button>("Settings_Btn");
        var creditsButton = root.Q<Button>("Credits_Btn");
        var quitButton = root.Q<Button>("Quit_Btn");

        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked += OnSettingsClicked;
        if (creditsButton != null) creditsButton.clicked += OnCreditsClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;

        // Setup Settings Panel
        SetupSettingsPanel();
    }

    private void SetupSettingsPanel()
    {
        settingsPanel = root.Q<VisualElement>("content-settings");

        masterSlider = root.Q<Slider>("slider-master");
        musicSlider = root.Q<Slider>("slider-music");
        sfxSlider = root.Q<Slider>("slider-sfx");

        applyButton = root.Q<Button>("btn-apply");
        resetButton = root.Q<Button>("btn-reset");
        closeButton = root.Q<Button>("btn-close");

        if (applyButton != null) applyButton.clicked += ApplySettings;
        if (resetButton != null) resetButton.clicked += ResetToDefaults;
        if (closeButton != null) closeButton.clicked += CloseSettings;

        // Load current settings when opening
        LoadSettingsIntoUI();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.style.display = DisplayStyle.Flex;
            LoadSettingsIntoUI(); // Refresh values
        }
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;
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
                masterSlider.value,
                musicSlider.value,
                sfxSlider.value,
                1.5f,           // sensitivity (can expand later)
                3,              // Quality High
                true,           // Fullscreen
                false           // Invert Y
            );
        }

        Debug.Log("[StartMenu] Settings Applied");
    }

    private void ResetToDefaults()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ResetToDefaults();

        LoadSettingsIntoUI();
    }

    private void OnPlayClicked()
    {
        StartCoroutine(LoadGameWithScreen());
    }

    private System.Collections.IEnumerator LoadGameWithScreen()
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

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

    private void OnCreditsClicked()
    {
        Debug.Log("Credits clicked - TODO: Implement Credits Panel");
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