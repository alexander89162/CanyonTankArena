using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameResults : MonoBehaviour
{
    public static GameResults Instance { get; private set; }

    [SerializeField] private UIDocument resultDocument;
    [SerializeField] private Canvas hudCanvas;

    private VisualElement root;
    private VisualElement pausePanel;
    private VisualElement winPanel;
    private VisualElement losePanel;

    private bool isPaused = false;
    private bool isGameOver = false;

    private InputAction pauseAction;
    [SerializeField] private AimWeapons aimWeapons;

    void Awake()
    {
        if (resultDocument == null)
            resultDocument = GetComponent<UIDocument>();

        root = resultDocument.rootVisualElement;

        // Find all panels
        pausePanel = root.Q<VisualElement>("PausePanel");
        winPanel = root.Q<VisualElement>("win-panel");
        losePanel = root.Q<VisualElement>("lose-panel");

        // Initial hide
        if(root != null) root.style.display = DisplayStyle.None;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;
        if (winPanel != null) winPanel.style.display = DisplayStyle.None;
        if (losePanel != null) losePanel.style.display = DisplayStyle.None;

        // Find Pause Input Action
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            pauseAction = playerInput.actions.FindActionMap("Tanks")?.FindAction("Pause");
    }

    void OnEnable()
    {
        root.focusable = true;
        root.pickingMode = PickingMode.Position;

        // Wire Win/Lose buttons
        root.Q<Button>("retryW-btn").clicked += OnRetry;
        root.Q<Button>("retryL-btn").clicked += OnRetry;
        root.Q<Button>("menuW-btn").clicked += OnMainMenu;
        root.Q<Button>("menuL-btn").clicked += OnMainMenu;

        // Wire Pause buttons
        if (pausePanel != null)
        {
            pausePanel.Q<Button>("ResumeBtn").clicked += ResumeGame;
            pausePanel.Q<Button>("QuitBtn").clicked += OnMainMenu;
        }
    }

    void Update()
    {
        if (isGameOver) return;

        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            TryTogglePause();
        }
    }

    public void TryTogglePause()
    {
        if (isGameOver) return;

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


        ShowCursor();

        if (root != null) root.style.display = DisplayStyle.Flex;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.Flex;
        if (winPanel != null) winPanel.style.display = DisplayStyle.None;
        if (losePanel != null) losePanel.style.display = DisplayStyle.None;

        if (hudCanvas != null) hudCanvas.enabled = false;

        if (aimWeapons != null)
            aimWeapons.SetAllowedToFire(false);
        //Debug.Log(aimWeapons != null ? "AimWeapons instance found, firing disabled" : "AimWeapons instance not found");

    }

    public bool GetPauseState()
    {
        return isPaused;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        HideCursor();

        if(root != null) root.style.display = DisplayStyle.None;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;
        if (hudCanvas != null) hudCanvas.enabled = true;

        if (aimWeapons != null)
            aimWeapons.SetAllowedToFire(true);
    }

    public void ShowWinScreen(float timeSurvived, int enemiesDefeated)
    {
        isGameOver = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        AudioListener.volume = 0f;
        ShowCursor();

        if (hudCanvas != null) hudCanvas.enabled = false;
        if (root != null) root.style.display = DisplayStyle.Flex;
        if (winPanel != null)
        {
            winPanel.style.display = DisplayStyle.Flex;
            winPanel.Q<Label>("win-time").text = $"Time Survived: {timeSurvived:F2}s";
            winPanel.Q<Label>("enemies-defeated").text = $"Enemies Defeated: {enemiesDefeated}";
        }
        if (aimWeapons != null)
            aimWeapons.SetAllowedToFire(false);
    }

    public void ShowLoseScreen()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        AudioListener.volume = 0f;
        ShowCursor();

        if (hudCanvas != null) hudCanvas.enabled = false;
        if (root != null) root.style.display = DisplayStyle.Flex;
        if (losePanel != null) losePanel.style.display = DisplayStyle.Flex;
        if (aimWeapons != null)
            aimWeapons.SetAllowedToFire(false);
    }

    private void ShowCursor()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnRetry()
    {
        ResetAudioAndTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenu()
    {
        ResetAudioAndTime();
        SceneManager.LoadScene("MainMenu");   // Change to "StartMenu" if needed
    }

    private void ResetAudioAndTime()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        HideCursor();
    }

    public bool IsGameOver() => isGameOver;
}