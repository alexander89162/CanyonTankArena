using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument pauseDocument;
    [SerializeField] private Canvas hud;

    private VisualElement root;
    private VisualElement pausePanel;

    private bool isPaused = false;

    private InputAction pauseAction;

    void Awake()
    {
        if (pauseDocument == null)
            pauseDocument = GetComponent<UIDocument>();

        root = pauseDocument.rootVisualElement;
        pausePanel = root.Q<VisualElement>("PauseOverlay");

        // Find pause action
        var inputActions = GetComponent<PlayerInput>()?.actions ?? FindFirstObjectByType<PlayerInput>()?.actions;
        if (inputActions != null)
        {
            pauseAction = inputActions.FindActionMap("Tanks")?.FindAction("Pause");
        }

        // Wire buttons
        if (pausePanel != null)
        {
            pausePanel.Q<Button>("ResumeBtn").clicked += OnResumeButton;
            pausePanel.Q<Button>("QuitMenuBtn").clicked += QuitToMainMenu;
        }

        // Hide at start
        if (pausePanel != null)
        {
            pausePanel.style.display = DisplayStyle.None;
        }
    }

    void OnEnable()
    {
        root.focusable = true;
        root.pickingMode = PickingMode.Position;
    }

    void Update()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            TryTogglePause();
        }
    }

    public void TryTogglePause()
    {
        var resultScreen = FindFirstObjectByType<GameResults>(); // Make sure class name matches

        if (resultScreen != null && resultScreen.IsGameOver())
            return;

        isPaused = !isPaused;

        if (isPaused)
            PauseGame();
        else
            ResumeGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;
        AudioListener.volume = 0f;

        if (pausePanel != null)
        {
            pausePanel.style.display = DisplayStyle.Flex;
        }

        if (hud != null)
            hud.enabled = false;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        if (root != null)
        {
            root.focusable = true;
            root.pickingMode = PickingMode.Position;
        }

        if (pausePanel != null)
        {
            pausePanel.focusable = true;
            pausePanel.pickingMode = PickingMode.Position;
        }
        // Switch input to UI
        EnableUIInput();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        if (pausePanel != null)
        {
            pausePanel.style.display = DisplayStyle.None;
        }

        if (hud != null)
            hud.enabled = true;

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        EnablePlayerInput();

        isPaused = false;
    }

    private void EnableUIInput()
    {
        // Disable player actions, enable UI actions
        var playerMap = FindActionMap("Tanks");
        var uiMap = FindActionMap("UI");

        playerMap?.Disable();
        uiMap?.Enable();
    }

    private void EnablePlayerInput()
    {
        var playerMap = FindActionMap("Tanks");
        var uiMap = FindActionMap("UI");

        uiMap?.Disable();
        playerMap?.Enable();
    }

    private InputActionMap FindActionMap(string mapName)
    {
        var playerInput = FindFirstObjectByType<PlayerInput>();
        return playerInput?.actions?.FindActionMap(mapName);
    }

    // Button Callbacks
    public void OnResumeButton() => ResumeGame();

    public void QuitToMainMenu()
    {
        ResumeGame();
        SceneManager.LoadScene("MainMenu");
    }

    public bool IsPaused => isPaused;
}