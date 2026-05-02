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
    private const string SAVEKEY = "PlayerSaveData";

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
        //Debug.Log("SaveManager Awake - Save path: " + saveFilePath);
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

    //func to save the game
    public void SaveGame()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("Cannot save - PlayerInventory.Instance is null");
            return;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogError("Cannot save - ScoreManager.Instance is null");
            return;
        }

        ScoreManager.Instance?.SaveHighScore();

        PlayerSaveData data = new PlayerSaveData { 
            inventoryItems = PlayerInventory.Instance.items, 
            highScore = ScoreManager.Instance.highScore,
            techData = new PlayerTechData{
                unlockedNodeIDs = TechTreeManager.Instance.GetUnlockedNodeIDs(),
            }
            };

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(saveFilePath, json);
        //Debug.Log($" SAVED successfully! Items saved: {data.inventoryItems.Count}");

#if UNITY_WEBGL && !UNITY_EDITOR
        PlayerPrefs.SetString(SAVEKEY, json);
        PlayerPrefs.Save();
        Debug.Log("💾 Saved to PlayerPrefs (WebGL)");
#else
        File.WriteAllText(saveFilePath, json);
        Debug.Log("💾 Saved to file: " + saveFilePath);
#endif
 
    }

    //func to load the game
    public void LoadGame()
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!PlayerPrefs.HasKey(SAVEKEY))
        {
            Debug.Log("No save data found (WebGL)");
            return;
        }
        string json = PlayerPrefs.GetString(SAVEKEY);
#else
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found at: " + saveFilePath);
            return;
        }
        string json = File.ReadAllText(saveFilePath);
#endif
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.items = data.inventoryItems ?? new List<ItemInstance>();
            PlayerInventory.Instance.TriggerInventoryChanged();
            Debug.Log($" LOADED! Items loaded: {PlayerInventory.Instance.items.Count}");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.highScore = data.highScore;
            Debug.Log($" High Score Loaded: {data.highScore}");
        }

        if (TechTreeManager.Instance != null)
        {
            TechTreeManager.Instance.LoadFromData(data.techData);
            Debug.Log($" Tech Data Loaded: Unlocked Nodes: {data.techData.unlockedNodeIDs.Count}");
        }
    }

    //func to clear the inventory, highscore, and tech tree, used for testing and debugging
    public void ClearInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.items.Clear();
            PlayerInventory.Instance.TriggerInventoryChanged();
            Debug.Log(" Inventory Cleared");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.currentScore = 0;
            ScoreManager.Instance.highScore = 0;
            Debug.Log("  Score Reset");
        }

        if (TechTreeManager.Instance != null)
        {
            TechTreeManager.Instance.LoadFromData(new PlayerTechData());
            Debug.Log("  Tech Tree Reset");
        }

        DeleteSave();
    }

    public void DeleteSave()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PlayerPrefs.DeleteKey(SAVEKEY);
        PlayerPrefs.Save();
#else
        if (File.Exists(saveFilePath))
            File.Delete(saveFilePath);
#endif
        Debug.Log("🗑️ Save data deleted");
    }
 
    //func to save the highscore
    public void SaveHighScore(int newHighScore)
    {
        // We'll load existing data, update high score, and save
        PlayerSaveData data;

        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            data = JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
        }
        else
        {
            data = new PlayerSaveData();
        }

        data.highScore = newHighScore;

        string newJson = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, newJson);
    }

    //func to get the highscore
    public int GetHighScore()
    {
        if (!File.Exists(saveFilePath)) return 0;

        string json = File.ReadAllText(saveFilePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
        return data != null ? data.highScore : 0;
    }

    //when the game quits, or crash save game, needs to test crash save still
    private void OnApplicationQuit()
    {
        SaveGame();
        TechTreeManager.Instance?.SaveTechTree();
        ScoreManager.Instance?.SaveHighScore();
        ScoreManager.Instance.currentScore = 0;
    }

    //func to save the tech tree data
    public void SaveTechData(PlayerTechData techData)
    {
        PlayerSaveData data;

        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            data = JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
        }
        else
        {
            data = new PlayerSaveData();
        }

        data.techData = techData;

        string jsonToSave = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonToSave);
    }

    //func to load the tech tree data
    public PlayerTechData LoadTechData()
    {
        if (!File.Exists(saveFilePath))
            return new PlayerTechData();

        string json = File.ReadAllText(saveFilePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        return data?.techData ?? new PlayerTechData();
    }
}