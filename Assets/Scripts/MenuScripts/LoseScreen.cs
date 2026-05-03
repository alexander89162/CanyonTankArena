using UnityEngine;
using UnityEngine.UIElements;

public class LoseScreen : basicUIScreen
{
    [SerializeField] private VisualTreeAsset loseUXML;

    protected override VisualTreeAsset GetVisualTreeAsset() => loseUXML;

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
