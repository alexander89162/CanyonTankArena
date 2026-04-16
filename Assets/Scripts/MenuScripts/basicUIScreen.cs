using UnityEngine;
using UnityEngine.UIElements;

public abstract class basicUIScreen : MonoBehaviour
{
    protected VisualElement root;
    protected UIDocument uiDocument;

    public virtual void Initialize(UIDocument document)
    {
        uiDocument = document;
        root = document.rootVisualElement;
    }

    public virtual void Show()
    {
        if (root == null) return;
        root.Clear();
        var screen = GetVisualTreeAsset().Instantiate();
        SetupScreen(screen);
        root.Add(screen);

        OnShow();
    }

    public virtual void Hide()
    {
        if (root != null) root.Clear();
        OnHide();
    }

    protected abstract VisualTreeAsset GetVisualTreeAsset();

    protected virtual void SetupScreen(VisualElement screen) { }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }

    // Helper to easily find and bind buttons
    protected void BindButton(VisualElement screen, string buttonName, System.Action onClick)
    {
        var btn = screen.Q<Button>(buttonName);
        if (btn != null) btn.clicked += onClick;
    }
}