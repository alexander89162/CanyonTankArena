using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Text;

public class InvDebugUI : MonoBehaviour
{
    [Header("Debug UI Settings")]
    [SerializeField] private Text debugText;
    [SerializeField] private Font debugFont;
    [SerializeField] private GameObject debugPanel;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference toggleAction;

    public static InvDebugUI Instance;

    private bool isVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnTogglePressed;
        }
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.Disable();
            toggleAction.action.performed -= OnTogglePressed;
        }
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        isVisible = !isVisible;
        SetVisibility(isVisible);
    }

    private void Start()
    {
        if (debugText == null && debugPanel != null)
        {
            debugText = debugPanel.GetComponentInChildren<Text>();
        }

        if (debugText == null)
        {
            CreateDebugText();
        }

        SetVisibility(false); // Start hidden
    }

    private void Update()
    {
        if (!isVisible || SaveManager.Instance == null || !SaveManager.Instance.enableDebugTools)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== INVENTORY DEBUG ===");
        sb.AppendLine("Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        sb.AppendLine("Total Items: " + (PlayerInventory.Instance != null ? PlayerInventory.Instance.items.Count.ToString() : "0"));
        sb.AppendLine("");

        if (PlayerInventory.Instance != null)
        {
            foreach (var item in PlayerInventory.Instance.items)
            {
                ItemSO so = item.GetItemSO();
                string name = so != null ? so.itemName : "Unknown";
                sb.AppendLine("• " + name + " × " + item.quantity);
            }
        }

        sb.AppendLine("");
        sb.AppendLine("Hotkeys:");
        sb.AppendLine("1 = Save");
        sb.AppendLine("2 = Load");
        sb.AppendLine("3 = Clear Inventory");
        sb.AppendLine("Toggle Key = Show/Hide Panel");

        if (debugText != null)
            debugText.text = sb.ToString();
    }

    private void CreateDebugText()
    {
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(transform);

        debugText = textObj.AddComponent<Text>();
        debugText.font = debugFont;
        //debugText.font = Resources.GetBuiltinResource<Font>("LiberationSans-Regular.ttf");
        debugText.fontSize = 30;
        debugText.color = Color.yellow;
        debugText.alignment = TextAnchor.UpperLeft;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(450, 700);
    }

    private void SetVisibility(bool visible)
    {
        isVisible = visible;
        if (debugPanel != null)
            debugPanel.SetActive(visible);
        else if (debugText != null)
            debugText.gameObject.SetActive(visible);
    }
}