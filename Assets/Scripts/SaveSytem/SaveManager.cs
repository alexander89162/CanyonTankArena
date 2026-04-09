using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Debug Settings")]
    public bool enableDebugTools = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference saveAction;
    [SerializeField] private InputActionReference loadAction;
    [SerializeField] private InputActionReference clearAction;

    private string saveFilePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Path.Combine(Application.persistentDataPath, "playerSave.json");
        Debug.Log("SaveManager Awake - Save path: " + saveFilePath);
    }

    private void OnEnable()
    {
        if (saveAction?.action != null) 
        {
            saveAction.action.Enable();
            saveAction.action.performed += _ => SaveGame();
        }
        if (loadAction?.action != null) 
        {
            loadAction.action.Enable();
            loadAction.action.performed += _ => LoadGame();
        }
        if (clearAction?.action != null) 
        {
            clearAction.action.Enable();
            clearAction.action.performed += _ => ClearInventory();
        }
    }

    public void SaveGame()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("Cannot save - PlayerInventory.Instance is null");
            return;
        }

        PlayerSaveData data = new PlayerSaveData { inventoryItems = PlayerInventory.Instance.items };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($" SAVED successfully! Items saved: {data.inventoryItems.Count}");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found at: " + saveFilePath);
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.items = data.inventoryItems ?? new List<ItemInstance>();
            PlayerInventory.Instance.TriggerInventoryChanged();
            Debug.Log($" LOADED! Items loaded: {PlayerInventory.Instance.items.Count}");
        }
    }

    public void ClearInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.items.Clear();
            PlayerInventory.Instance.TriggerInventoryChanged();
            Debug.Log(" Inventory Cleared");
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}