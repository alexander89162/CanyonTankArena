using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Documents")]
    [SerializeField] private UIDocument pauseDocument;
    [SerializeField] private UIDocument gameResultDocument;

    private VisualElement pauseRoot;
    private VisualElement resultRoot;

    private bool isGameOver = false;

    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (pauseDocument != null)
            pauseRoot = pauseDocument.rootVisualElement;

        if (gameResultDocument != null)
            resultRoot = gameResultDocument.rootVisualElement;
    }

    // ====================== GAME RESULT ======================
    public void ShowWinScreen(float timeSurvived, int enemiesDefeated)
    {
        isGameOver = true;
        Time.timeScale = 0f;

        if (resultRoot != null)
        {
            resultRoot.Q<VisualElement>("win-panel").style.display = DisplayStyle.Flex;
            resultRoot.Q<VisualElement>("lose-panel").style.display = DisplayStyle.None;

            resultRoot.Q<Label>("win-time").text = $"Survival Time: {FormatTime(timeSurvived)}";
            resultRoot.Q<Label>("enemies-defeated").text = $"Enemies Defeated: {enemiesDefeated}";
        }

        // Disable pause menu
        SetPauseMenuEnabled(false);
    }

    public void ShowLoseScreen()
    {
        isGameOver = true;
        Time.timeScale = 0f;

        if (resultRoot != null)
        {
            resultRoot.Q<VisualElement>("win-panel").style.display = DisplayStyle.None;
            resultRoot.Q<VisualElement>("lose-panel").style.display = DisplayStyle.Flex;
        }

        SetPauseMenuEnabled(false);
    }

    // ====================== PAUSE ======================
    public void TogglePause()
    {
        if (isGameOver) return; // ← This is the key fix

        bool isPaused = Time.timeScale == 0f;

        if (!isPaused)
            PauseGame();
        else
            ResumeGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        if (pauseRoot != null)
            pauseRoot.style.display = DisplayStyle.Flex;
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        Time.timeScale = 1f;
        if (pauseRoot != null)
            pauseRoot.style.display = DisplayStyle.None;
    }

    private void SetPauseMenuEnabled(bool enabled)
    {
        if (pauseRoot != null)
            pauseRoot.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private string FormatTime(float time)
    {
        int min = Mathf.FloorToInt(time / 60);
        int sec = Mathf.FloorToInt(time % 60);
        return $"{min:00}:{sec:00}";
    }

    // Optional: Force close everything
    public void CloseAll()
    {
        if (pauseRoot != null) pauseRoot.style.display = DisplayStyle.None;
        if (resultRoot != null)
        {
            resultRoot.Q<VisualElement>("win-panel").style.display = DisplayStyle.None;
            resultRoot.Q<VisualElement>("lose-panel").style.display = DisplayStyle.None;
        }
    }
}