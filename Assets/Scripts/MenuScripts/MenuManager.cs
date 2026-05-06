using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string arenaSceneName = "Demo3_V3";

    [Header("Tech Tree")]
    [SerializeField] private TechTreeSceneView techTreeView;

    private VisualElement root;
    private VisualElement[] contents;
    private Button[] tabButtons;
    private Slider masterSlider, musicSlider, sfxSlider, sensitivitySlider;
    private DropdownField qualityDropdown;
    private Toggle fullscreenToggle, invertYToggle;
    private Button applyButton, resetButton;
    [SerializeField] private AudioMixer mainAudioMixer;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("GarageManager: Assign UIDocument in Inspector!");
            return;
        }

        root = uiDocument.rootVisualElement;
        SetupUI();
    }

    private void SetupUI()
    {
        // Get all tab buttons
        tabButtons = new Button[5];
        tabButtons[0] = root.Q<Button>("tab-main");
        tabButtons[1] = root.Q<Button>("tab-shop");
        tabButtons[2] = root.Q<Button>("tab-tech");
        tabButtons[3] = root.Q<Button>("tab-settings");
        tabButtons[4] = root.Q<Button>("tab-custom");

        // Get all content panels
        contents = new VisualElement[5];
        contents[0] = root.Q<VisualElement>("content-main");
        contents[1] = root.Q<VisualElement>("content-shop");
        contents[2] = root.Q<VisualElement>("content-tech");
        contents[3] = root.Q<VisualElement>("content-settings");
        contents[4] = root.Q<VisualElement>("content-custom");

        // Hook up tab buttons
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].clicked += () => SwitchToTab(index);
        }

        // Hook up Start Match button
        Button startBtn = root.Q<Button>("StartMatchButton");
        if (startBtn != null)
            startBtn.clicked += StartMatch;

        // Show first tab by default
        SwitchToTab(0);

        SetupSettings();
    }

    private void SwitchToTab(int index)
    {
        // Hide all contents
        foreach (var content in contents)
            if (content != null) content.RemoveFromClassList("active");

        if (index == 3) 
            LoadSettingsIntoUI(); 

        // Show selected content
        if (contents[index] != null)
            contents[index].AddToClassList("active");

        // Update tab button styles
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                if (i == index)
                    tabButtons[i].AddToClassList("active");
                else
                    tabButtons[i].RemoveFromClassList("active");
            }
        }

        if (techTreeView != null)
        {
            bool showTechTree = (index == 2); // Tech Tree is index 2
            techTreeView.gameObject.SetActive(showTechTree);
            
            if (showTechTree)
            {
                // Optional: Refresh the tech tree when tab is opened
                techTreeView.SetData(techTreeView.GetData()); 
            }
        }

        //Debug.Log($"[Garage] Switched to tab {index}");
    }

    private void StartMatch()
    {
        if (string.IsNullOrEmpty(arenaSceneName))
        {
            //Debug.LogError("Arena scene name not set!");
            return;
        }
        //Debug.Log($"Starting match → {arenaSceneName}");
        SceneManager.LoadScene(arenaSceneName);
    }

    private void SetupSettings()
    {
        // Find UI elements
        masterSlider = root.Q<Slider>("slider-master");
        musicSlider = root.Q<Slider>("slider-music");
        sfxSlider = root.Q<Slider>("slider-sfx");
        sensitivitySlider = root.Q<Slider>("slider-sensitivity");

        qualityDropdown = root.Q<DropdownField>("dropdown-quality");
        fullscreenToggle = root.Q<Toggle>("toggle-fullscreen");
        invertYToggle = root.Q<Toggle>("toggle-invert-y");

        applyButton = root.Q<Button>("btn-apply");
        resetButton = root.Q<Button>("btn-reset");

        // Load saved values when opening settings
        LoadSettingsIntoUI();

        // Register listeners
        if (applyButton != null) applyButton.clicked += ApplySettings;
        if (resetButton != null) resetButton.clicked += ResetToDefaults;
    }

    // Load saved settings from PlayerPrefs
    private void LoadSettingsIntoUI()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        if (sensitivitySlider != null) sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 1.5f);

        if (qualityDropdown != null)
            qualityDropdown.value = PlayerPrefs.GetString("QualityPreset", "High");

        if (fullscreenToggle != null)
            fullscreenToggle.value = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        if (invertYToggle != null)
            invertYToggle.value = PlayerPrefs.GetInt("InvertY", 0) == 1;
    }

    // ====================== APPLY SETTINGS ======================
    private void ApplySettings()
    {
        if (SettingsManager.Instance == null) return;

        int qualityIndex = qualityDropdown?.index ?? 2;

        SettingsManager.Instance.SaveAndApplySettings(
            masterSlider.value,
            musicSlider.value,
            sfxSlider.value,
            sensitivitySlider?.value ?? 1.5f,
            qualityIndex,
            fullscreenToggle?.value ?? true,
            invertYToggle?.value ?? false
        );

        Debug.Log("[Garage] Settings Applied");
    }

    private void ResetToDefaults()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ResetToDefaults();

        LoadSettingsIntoUI(); // Refresh UI with new defaults
    }

    // Public methods to open specific tabs from other scripts
    public void OpenMain() => SwitchToTab(0);
    public void OpenShop() => SwitchToTab(1);
    public void OpenTechTree() => SwitchToTab(2);
    public void OpenSettings() => SwitchToTab(3);
    public void OpenCustomization() => SwitchToTab(4);

    public void GetSetupSettings() => SetupSettings();
    public void GetLoadSettings() => LoadSettingsIntoUI();
}