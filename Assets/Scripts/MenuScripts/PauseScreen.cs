using UnityEngine;
using UnityEngine.UIElements;

public class PauseScreen : basicUIScreen
{
    [SerializeField] private VisualTreeAsset pauseUXML;

    protected override VisualTreeAsset GetVisualTreeAsset() => pauseUXML;

    protected override void SetupScreen(VisualElement screen)
    {
        BindButton(screen, "ResumeBttn", () => GameUIManager.Instance.TogglePause());
        BindButton(screen, "RestartButton", () => GameUIManager.Instance.RestartBattle());
        BindButton(screen, "QuitMenuBtn", () => GameUIManager.Instance.ReturnToMenu());
    }

    protected override void OnShow()
    {
        ReleaseCursor();
        Time.timeScale = 0f;
    }

    protected override void OnHide()
    {
        LockCursor();
        Time.timeScale = 1f;
    }

    private void ReleaseCursor()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    }

    private void LockCursor()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
    }
}