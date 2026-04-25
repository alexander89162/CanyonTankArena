using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string arenaSceneName = "Demo3_V3";

    [Header("Tech Tree")]
    [SerializeField] private TechTreeSceneView techTreeView;

    private VisualElement root;
    private VisualElement[] contents;
    private Button[] tabButtons;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("GarageManager: Assign UIDocument in Inspector!");
            return;
        }

        root = uiDocument.rootVisualElement;
        SetupUI();
    }

    private void SetupUI()
    {
        // Get all tab buttons
        tabButtons = new Button[5];
        tabButtons[0] = root.Q<Button>("tab-main");
        tabButtons[1] = root.Q<Button>("tab-shop");
        tabButtons[2] = root.Q<Button>("tab-tech");
        tabButtons[3] = root.Q<Button>("tab-settings");
        tabButtons[4] = root.Q<Button>("tab-custom");

        // Get all content panels
        contents = new VisualElement[5];
        contents[0] = root.Q<VisualElement>("content-main");
        contents[1] = root.Q<VisualElement>("content-shop");
        contents[2] = root.Q<VisualElement>("content-tech");
        contents[3] = root.Q<VisualElement>("content-settings");
        contents[4] = root.Q<VisualElement>("content-custom");

        // Hook up tab buttons
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].clicked += () => SwitchToTab(index);
        }

        // Hook up Start Match button
        Button startBtn = root.Q<Button>("StartMatchButton");
        if (startBtn != null)
            startBtn.clicked += StartMatch;

        // Show first tab by default
        SwitchToTab(0);
    }

    private void SwitchToTab(int index)
    {
        // Hide all contents
        foreach (var content in contents)
            if (content != null) content.RemoveFromClassList("active");

        // Show selected content
        if (contents[index] != null)
            contents[index].AddToClassList("active");

        // Update tab button styles
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                if (i == index)
                    tabButtons[i].AddToClassList("active");
                else
                    tabButtons[i].RemoveFromClassList("active");
            }
        }

        if (techTreeView != null)
        {
            bool showTechTree = (index == 2); // Tech Tree is index 2
            techTreeView.gameObject.SetActive(showTechTree);
            
            if (showTechTree)
            {
                // Optional: Refresh the tech tree when tab is opened
                techTreeView.SetData(techTreeView.GetData()); 
            }
        }

        //Debug.Log($"[Garage] Switched to tab {index}");
    }

    private void StartMatch()
    {
        if (string.IsNullOrEmpty(arenaSceneName))
        {
            //Debug.LogError("Arena scene name not set!");
            return;
        }
        //Debug.Log($"Starting match → {arenaSceneName}");
        SceneManager.LoadScene(arenaSceneName);
    }

    // Public methods to open specific tabs from other scripts
    public void OpenMain() => SwitchToTab(0);
    public void OpenShop() => SwitchToTab(1);
    public void OpenTechTree() => SwitchToTab(2);
    public void OpenSettings() => SwitchToTab(3);
    public void OpenCustomization() => SwitchToTab(4);
}