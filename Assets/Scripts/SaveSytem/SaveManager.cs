using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Debug Settings")]
    public bool enableDebugTools = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference saveAction;
    [SerializeField] private InputActionReference loadAction;
    [SerializeField] private InputActionReference clearAction;

    private const string GAME_ID = "tank_arena_save_v1";
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

        saveFilePath = GetSaveFilePath();
        Debug.Log("[SaveManager] Save path: " + saveFilePath);

#if UNITY_WEBGL && !UNITY_EDITOR
        MountIDBFS();
        StartCoroutine(LoadAfterMount());
#else
        LoadGame();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private System.Collections.IEnumerator LoadAfterMount()
    {
        // Wait for syncfs(true) to finish before reading files
        yield return new WaitForSeconds(0.5f);
        LoadGame();
    }
#endif

    private string GetSaveFilePath()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return "/idbfs/" + GAME_ID + "/playerSave.json";
#else
        string folder = Application.persistentDataPath;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return Path.Combine(folder, "playerSave.json");
#endif
    }

    private void OnEnable()
    {
        if (saveAction != null && saveAction.action != null)
        {
            saveAction.action.Enable();
            saveAction.action.performed += OnSavePressed;
        }
        if (loadAction != null && loadAction.action != null)
        {
            loadAction.action.Enable();
            loadAction.action.performed += OnLoadPressed;
        }
        if (clearAction != null && clearAction.action != null)
        {
            clearAction.action.Enable();
            clearAction.action.performed += OnClearPressed;
        }
    }

    private void OnSavePressed(InputAction.CallbackContext ctx) { SaveGame(); }
    private void OnLoadPressed(InputAction.CallbackContext ctx) { LoadGame(); }
    private void OnClearPressed(InputAction.CallbackContext ctx) { ClearInventory(); }

    public void SaveGame()
    {
        if (PlayerInventory.Instance == null) return;

        PlayerSaveData data = new PlayerSaveData();

        data.inventoryItems = PlayerInventory.Instance.items != null ?
                             PlayerInventory.Instance.items : new List<ItemInstance>();

        if (ScoreManager.Instance != null)
            data.highScore = ScoreManager.Instance.highScore;

        if (TechTreeManager.Instance != null)
        {
            data.techData = new PlayerTechData();
            data.techData.unlockedNodeIDs = TechTreeManager.Instance.GetUnlockedNodeIDs();
        }
        else
        {
            data.techData = new PlayerTechData();
        }

        string json = JsonUtility.ToJson(data, true);
        string path = GetSaveFilePath();

        try
        {
            File.WriteAllText(path, json);
            SyncFileSystem();
            Debug.Log("✅ Full Game Saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public void SaveHighScore(int newHighScore)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.highScore = newHighScore;

        SaveGame();
    }

    public int GetHighScore()
    {
        if (ScoreManager.Instance != null)
            return ScoreManager.Instance.highScore;

        string path = GetSaveFilePath();
        if (!File.Exists(path)) return 0;

        try
        {
            string json = File.ReadAllText(path);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
            return data != null ? data.highScore : 0;
        }
        catch
        {
            return 0;
        }
    }

    public void SaveTechData(PlayerTechData techData)
    {
        PlayerSaveData data = LoadFullSaveData();
        data.techData = techData;
        SaveFullData(data);
    }

    public PlayerTechData LoadTechData()
    {
        PlayerSaveData data = LoadFullSaveData();
        return data != null && data.techData != null ? data.techData : new PlayerTechData();
    }

    private PlayerSaveData LoadFullSaveData()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) return new PlayerSaveData();

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
        }
        catch
        {
            return new PlayerSaveData();
        }
    }

    private void SaveFullData(PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetSaveFilePath();

        try
        {
            File.WriteAllText(path, json);
            SyncFileSystem();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Tech save failed: " + e.Message);
        }
    }

    public void LoadGame()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path))
        {
            Debug.Log("No save data found yet.");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

            if (data == null) return;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.items = data.inventoryItems != null ?
                    data.inventoryItems : new List<ItemInstance>();
                PlayerInventory.Instance.TriggerInventoryChanged();
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.highScore = data.highScore;

            if (TechTreeManager.Instance != null && data.techData != null)
                TechTreeManager.Instance.LoadFromData(data.techData);

            Debug.Log("✅ Game Loaded Successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
        }
    }

    public void ClearInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.items.Clear();
            PlayerInventory.Instance.TriggerInventoryChanged();
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.highScore = 0;

        if (TechTreeManager.Instance != null)
            TechTreeManager.Instance.LoadFromData(new PlayerTechData());

        DeleteSave();
        Debug.Log("🗑️ All progress cleared.");
    }

    public void DeleteSave()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
            File.Delete(path);

        SyncFileSystem();
        Debug.Log("Save file deleted");
    }

    // ====================== WEBGL SYNC ======================
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MountIDBFS();
    [DllImport("__Internal")] private static extern void SyncFiles();
#endif

    private void SyncFileSystem()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}