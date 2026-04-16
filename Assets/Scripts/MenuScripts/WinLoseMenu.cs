using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class WinLoseMenu : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Screen UXMLs")]
    [SerializeField] private VisualTreeAsset winScreenUXML;
    [SerializeField] private VisualTreeAsset loseScreenUXML;

    private VisualElement rootVisualElement;

    private UnitManager unitManager;

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        rootVisualElement = uiDocument.rootVisualElement;

        // Find UnitManager and subscribe to events
        unitManager = FindFirstObjectByType<UnitManager>();
        if (unitManager != null)
        {
            unitManager.OnVictory += ShowWinScreen;
            unitManager.OnBattleExit += ShowLoseScreen;
        }
        else
        {
            Debug.LogWarning("UnitManager not found! Win/Lose screens won't trigger automatically.");
        }
    }

    private void ShowWinScreen()
    {
        ShowScreen(winScreenUXML);
    }

    private void ShowLoseScreen()
    {
        ShowScreen(loseScreenUXML);
    }

    private void ShowScreen(VisualTreeAsset screenUXML)
    {
        if (screenUXML == null || rootVisualElement == null) return;

        rootVisualElement.Clear();                    // Remove any existing UI
        var screen = screenUXML.Instantiate();

        // Hook up buttons
        var restartBtn = screen.Q<Button>("RestartButton");
        var garageBtn = screen.Q<Button>("GarageButton");

        if (restartBtn != null)
            restartBtn.clicked += RestartBattle;

        if (garageBtn != null)
            garageBtn.clicked += ReturnToMenu;

        rootVisualElement.Add(screen);

        Time.timeScale = 0f;        // Pause the game
    }

    public void RestartBattle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");   // ← Change this to your actual garage scene name
    }

    void OnDisable()
    {
        if (unitManager != null)
        {
            unitManager.OnVictory -= ShowWinScreen;
            unitManager.OnBattleExit -= ShowLoseScreen;
        }
    }
}