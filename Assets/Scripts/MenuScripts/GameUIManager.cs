using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;


public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Screens")]
    [SerializeField] private WinScreen winScreen;
    [SerializeField] private LoseScreen loseScreen;
    [SerializeField] private PauseScreen pauseScreen;

    private UnitManager unitManager;
    private bool isPaused = false;

    [SerializeField] private InputActionReference pauseAction;

    void Awake()
    {
        Instance = this;

        // Initialize all screens
        winScreen?.Initialize(uiDocument);
        loseScreen?.Initialize(uiDocument);
        pauseScreen?.Initialize(uiDocument);
    }

    void OnEnable()
    {
        unitManager = FindFirstObjectByType<UnitManager>();
        if (unitManager != null)
        {
            unitManager.OnVictory += winScreen.Show;
            unitManager.OnBattleExit += loseScreen.Show;
        }

        if (pauseAction?.action != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += ctx => TogglePause();
        }
    }

    public void TogglePause()
    {
        if (Time.timeScale <= 0f && !isPaused) return; // Don't pause during win/lose

        isPaused = !isPaused;

        if (isPaused)
            pauseScreen.Show();
        else
            pauseScreen.Hide();
    }

    public void RestartBattle()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }

    void OnDisable()
    {
        if (unitManager != null)
        {
            unitManager.OnVictory -= winScreen.Show;
            unitManager.OnBattleExit -= loseScreen.Show;
        }

        if (pauseAction?.action != null)
            pauseAction.action.performed -= ctx => TogglePause();
    }
}