using UnityEngine;
using UnityEngine.UIElements;

public class WinScreen : basicUIScreen
{
    [SerializeField] private VisualTreeAsset winUXML;

    protected override VisualTreeAsset GetVisualTreeAsset() => winUXML;

    protected override void SetupScreen(VisualElement screen)
    {
        //BindButton(screen, "RestartButton", () => GameUIManager.Instance.RestartBattle());
        //BindButton(screen, "GarageButton", () => GameUIManager.Instance.ReturnToMenu());
    }

    protected override void OnShow()
    {
        ReleaseCursor();
        Time.timeScale = 0f;
    }

    private void ReleaseCursor()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    }
}