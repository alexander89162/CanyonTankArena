using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    private UIDocument pauseDocument; 

    // References to the panels and buttons inside your UXML
    private VisualElement root;
    private VisualElement pausePanel;

    private bool isPaused = false;
    public Canvas hud;
    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap player;
    private InputActionMap ui;
    private InputAction pauseAction;

    void Awake()
    {
        pauseDocument = GetComponent<UIDocument>();
        if (pauseDocument == null)
        {
            Debug.LogError("Pause UIDocument is not assigned in the Inspector!");
            return;
        }

        root = pauseDocument.rootVisualElement;

        pausePanel = root.Q<VisualElement>("PauseOverlay");
        player = inputActions != null ? inputActions.FindActionMap("Tanks") : null;
        ui = inputActions != null ? inputActions.FindActionMap("UI") : null;
        pauseAction = inputActions != null ? inputActions.FindActionMap("Tanks")?.FindAction("Pause") : null;

        // Hide everything at start
        if (pausePanel != null) 
        {
            //pausePanel.style.display = DisplayStyle.None;
            pausePanel.visible = false;
        }

        // Wire up all buttons (names must match your UXML button names)
        if (pausePanel != null)
        {
            pausePanel.Q<Button>("ResumeBtn").clicked     += OnResumeButton;
            //pausePanel.Q<Button>("SettingsBtn").clicked   += OpenSettings;
            pausePanel.Q<Button>("QuitMenuBtn").clicked  += QuitToMainMenu;
        }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

void OnEnable()
{
        player?.Enable();
    }

    void OnDisable()
    {
        player?.Disable();
    }

    void OnDestroy()
    {
    }

    void Update()
    {
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool pauseActionPressed = pauseAction != null && pauseAction.WasPressedThisFrame();

        if (escapePressed || pauseActionPressed)
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {   
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.visible = true; // Show the pause panel
            pausePanel.style.display = DisplayStyle.Flex;
        }

        if (hud != null)
            hud.enabled = false;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    
        player?.Disable();
        ui?.Enable();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (pausePanel != null)    
        {
            pausePanel.visible = false; // Hide the pause panel
        }

        if (hud != null)
            hud.enabled = true;

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        ui?.Disable();
        player?.Enable();   
        

        isPaused = false;
    }

    // Button callbacks
    public void OnResumeButton() => ResumeGame(); 

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;
    }

    public void CloseSettings()
    {
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.Flex;
    }

    public void QuitToMainMenu()
    {
        ResumeGame(); // reset time scale before loading
        SceneManager.LoadScene("StartMenu"); 
    }

    public void QuitGame()
    {
        ResumeGame();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public bool IsPaused => isPaused;
}