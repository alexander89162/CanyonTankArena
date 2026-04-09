using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.Networking;

/*Allocates/Deallocates memory, spawns units from JSON, and stores 
data about the current enemies such as enemiesRemaining*/
public class UnitManager : MonoBehaviour
{
    public enum WaveState
    {
        InitializingUnitManager, // don't do anything in Update() yet, we are still loading configs (avoid crashes)
        Allocating, // Fills unitHandles with instantiated units (disabled until WaveRunning)
        BuildingQueue, // Fills SpawnQueue by first-to-last spawnDelay
        WaveReady, // We allocated and filled queue, now just wait for minimum wait time between waves so waves never start too quickly
        WaveRunning, // Gradually empty the SpawnQueue, then transition to next wave when all enemies or player has died
        Deallocating // Destroys all units, empty unitHandles
    }
    [SerializeField] private List<GameObject> prefabList;
    [SerializeField] private float minInterWaveTime = 10f; // min time to wait between waves
    public int batchSize = 20; // how many units to allocate per frame during allocation state
    public string currentMap;
    public int enemiesRemaining;
    public LayerMask defaultLayerMask;
    public float defaultRequiredOpenSpace = 30f;

    private Dictionary<string, GameObject> prefabLookup;
    private List<UnitData> unitHandles; // list of all units in the current wave
    private WaveState currentState = WaveState.InitializingUnitManager;
    private string currentWave;
    private string waveMode;
    private float interWaveTimer = 0f;
    private float waveTimer = 0;
    private Queue<UnitData> spawnQueue;
    private Dictionary<int, SpawnPoint> spawnPointLookup;
    private bool busy = false; // set True while allocating or deallocating memory
    private string waveContentsPath;
    private Dictionary<string, string> waveTransitions;

    [System.Serializable]
    public class WaveConfigFile
    {
        public string waveMode;
        public string startWave;
        public Transition[] transitions;
    }

    [System.Serializable]
    public class Transition
    {
        public string from;
        public string to;
    }

    [System.Serializable]
    public struct UnitConfig // data to be used by SpawnQueue, ensuring we place units with proper configurations
    {
        public string prefabName;
        public float spawnDelay;
        public int spawnPointId;
        public int team;
        public float damageMultiplier;
        public float healthMultiplier;
        public float movementSpeed;
    }

    [System.Serializable]
    public struct WaveDefinition
    {
        public string spawnMethod;
        public UnitConfig[] unitConfigs;
    }

    public event Action OnVictory; // when we win
    public event Action OnBattleExit; // when the player leaves, or edge case breaks the waves
    public bool paused = false; // only can unpause via event from UI
    public bool debug = false;

    void Awake()
    {
        SetState(WaveState.InitializingUnitManager);
        unitHandles = new List<UnitData>();
        prefabLookup = prefabList.ToDictionary(p => p.name, p => p);
        spawnQueue = new Queue<UnitData>();
        waveTransitions = new Dictionary<string, string>(); // this is filled inside of LoadWaveConfigurations()
        SetWaveContentsPath(currentMap);
        BuildSpawnPointsMap();
        StartCoroutine(LoadWaveConfigurations());
    }

    void Update()
    /*Based on the state, we either allocate/ deallocate memory, pause, 
    or follow spawn queue for the current wave*/
    {
        if (paused) return;

        switch (currentState)
        {
            case WaveState.InitializingUnitManager: break;
            case WaveState.WaveRunning:
                UpdateWave(); // Uses queue to gradually spawn everything, then waits for all enemies (or player) to die to end wave
                break;

            case WaveState.Allocating:
                interWaveTimer += Time.deltaTime;
                if (!busy)
                {
                    busy = true;
                    StartCoroutine(StartAllocating());
                }
                break;

            case WaveState.BuildingQueue:
                interWaveTimer += Time.deltaTime;
                if (!busy)
                {
                    busy = true;
                    BuildSpawnQueue(); // Build the queue to read from during WaveRunning state
                }
                break;

            case WaveState.WaveReady:
                interWaveTimer += Time.deltaTime;
                if (interWaveTimer > minInterWaveTime)
                    SetState(WaveState.WaveRunning);
                break;
            
            case WaveState.Deallocating:
                if (!busy)
                {
                    busy = true;
                    DeallocateWave();
                }
                interWaveTimer = 0f;
                break;
        }
    }

    private void SetWaveContentsPath(string mapName)
    {
    waveContentsPath = System.IO.Path.Combine(
        Application.streamingAssetsPath,
        "Waves",
        mapName
        );

        if (debug) Debug.Log("waveContentsPath was set to " + waveContentsPath);
    }

    private void BuildSpawnPointsMap()
    {
        spawnPointLookup = new Dictionary<int, SpawnPoint>();

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();

        foreach (var sp in spawnPoints)
        {
            if (spawnPointLookup.ContainsKey(sp.id))
            {
                Debug.LogError($"Duplicate SpawnPoint ID: {sp.id}");
                continue;
            }

            sp.Configure(defaultLayerMask, defaultRequiredOpenSpace, this);
            spawnPointLookup[sp.id] = sp;
        }
    }

    public void UpdateWave()
    /*Read from the SpawnQueue, spawning and popping until there are no more 
    valid units to spawn in the current frame*/
    {
        waveTimer += Time.deltaTime;
        while (spawnQueue.Count > 0 && spawnQueue.Peek().config.spawnDelay <= waveTimer)
        {
            EnqueueForSpawn();
        }

        bool anyPending = spawnQueue.Count > 0 ||
            spawnPointLookup.Values.Any(sp => sp.HasQueuedUnits());

        if (!anyPending && enemiesRemaining == 0)
        {
            SetState(WaveState.Deallocating);
        }

        foreach (var sp in spawnPointLookup.Values)
        {
            sp.TryFlush();
        }
    }

    public void EnqueueForSpawn()
    {
        if (debug) Debug.Log("EnqueueForSpawn() invoked on " + currentWave);
        
        UnitData data = spawnQueue.Dequeue();
        UnitConfig config = data.config;

        if (!spawnPointLookup.TryGetValue(config.spawnPointId, out SpawnPoint sp))
        {
            Debug.LogError($"SpawnPoint ID {config.spawnPointId} not found.");
            return;
        }

        sp.Enqueue(data);
    }

    private IEnumerator StartAllocating()
    /*Start coroutine to allocate memory for next wave's units*/
    {
        if (debug) Debug.Log("StartAllocating() invoked on " + currentWave);

        yield return StartCoroutine(LoadWave()); // Before allocation, unitHandles will be up-to-date with wave info from the current wave JSON
        yield return StartCoroutine(AllocateUnits()); // here we actually create the next wave's units
    }

    private IEnumerator LoadWave()
    {
        if (debug) Debug.Log("LoadWave() invoked on " + currentWave);

        string path = System.IO.Path.Combine(waveContentsPath, String.Concat(currentWave, ".json"));

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load wave JSON: " + request.error);
            yield break;
        }

        string jsonString = request.downloadHandler.text;

        WaveDefinition wave =
            JsonUtility.FromJson<WaveDefinition>(jsonString);

        FillUnitHandles(wave);
    }

    private IEnumerator LoadWaveConfigurations()
    /*Load the waveconfig.json and set WaveMode. Fill the transitions dictionary
    based on the JSON if wave mode is procedural*/
    {
        if (debug) Debug.Log("LoadWaveConfigurations() invoked on " + currentWave);

        string path = System.IO.Path.Combine(
            waveContentsPath,
            "waveconfig.json"
        );

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load wave config: " + request.error);
            yield break;
        }

        WaveConfigFile config =
            JsonUtility.FromJson<WaveConfigFile>(
                request.downloadHandler.text
            );

        waveMode = config.waveMode;

        waveTransitions.Clear();

        if (waveMode.ToLower() == "procedural")
        {
            foreach (var t in config.transitions)
            {
                waveTransitions[t.from] = t.to;
            }
            currentWave = config.startWave;
        } 
        else
        {
            currentWave = "wave1";
        }
        SetState(WaveState.Allocating);
    }

    public void FillUnitHandles(WaveDefinition wave)
    /*Fill unitHandles based on JSON file of current wave*/
    {
        if (debug) Debug.Log("FillUnitHandles() invoked on " + currentWave);

        // Fill unitHandles with new data
        unitHandles.Clear();
        for (int i = 0; i < wave.unitConfigs.Length; i++)
        {
            UnitData handle = new UnitData
            {
                config = wave.unitConfigs[i]
            };

            unitHandles.Add(handle);
        }
    }

    private IEnumerator AllocateUnits()
    /* Create all the GameObjects needed for the next wave. This is done in
    small batches to avoid freezing the UI while allocation is in progress*/
    {
        if (debug) Debug.Log("AllocateUnits() invoked on " + currentWave);

        for (int i = 0; i < unitHandles.Count; i++)
        {
            UnitConfig config = unitHandles[i].config;
            if (!prefabLookup.TryGetValue(config.prefabName, out GameObject unitPrefab))
            {
                Debug.LogError("The prefab \"" + config.prefabName + "\" was skipped because it was not found.");
                unitHandles.RemoveAt(i); i--; continue;
            }
            GameObject unit = Instantiate(unitPrefab);
            unitHandles[i].gameObject = unit;
            unit.SetActive(false);

            if (i % batchSize == 0) // yield on first iteration is OK here and on purpose
                yield return null;
        }

        // Sort unitHandles by spawnDelay so it's ready for building the spawn queue
        unitHandles.Sort((a, b) =>
            a.config.spawnDelay.CompareTo(b.config.spawnDelay)
        );

        SetState(WaveState.BuildingQueue);
        busy = false;
    }

    private void BuildSpawnQueue()
    /*Fill the spawn queue so it's ready for the wave to use*/
    {
        if (debug) Debug.Log("BuildSpawnQueue() invoked on " + currentWave);

        spawnQueue.Clear();

        foreach (var unit in unitHandles)
        {
            spawnQueue.Enqueue(unit);
        }

        waveTimer = 0f;
        enemiesRemaining = unitHandles.Count;

        busy = false;
        SetState(WaveState.WaveReady);
    }

    public void DeallocateWave()
    /*Destroy all the GameObjects in unitHandles, then clear list*/
    {
        if (debug) Debug.Log("DeallocateWave() invoked on " + currentWave);

        foreach (var unit in unitHandles)
        {
            Destroy(unit.gameObject);
        }

        unitHandles.Clear();

        // try to find the next wave. If it does not exist, we can terminate this
        // UnitManager instance and send message to UI about Victory
        string next;
        int result = FindNextWave(currentWave, out next);

        if (result == 0)
        {
            currentWave = next;
            SetState(WaveState.Allocating);
        }
        else if (result == 1)
        {
            OnVictory?.Invoke();
            Destroy(this);
        }
        else
        {
            OnBattleExit?.Invoke();
            Destroy(this);
        }
        busy = false;
    }

    public int FindNextWave(string previous, out string nextWave)
    {
        if (debug) Debug.Log("FindNextWave() invoked on " + currentWave);

        nextWave = null;

        switch (waveMode.ToLower())
        {
            case "sequential":
                if (!previous.StartsWith("wave") || !int.TryParse(previous.Substring(4), out int lastWaveNum))
                {
                    Debug.LogError("Invalid wave name: " + previous);
                    OnBattleExit?.Invoke(); Destroy(this); return -1;
                }

                nextWave = "wave" + (lastWaveNum + 1);
                string path = System.IO.Path.Combine(
                    waveContentsPath,
                    nextWave + ".json"
                );
                return System.IO.File.Exists(path) ? 0 : 1;
                
            case "procedural": // endless mode
                if (debug) Debug.Log("Previous wave: " + (previous ?? "NULL"));
                if (waveTransitions != null && waveTransitions.TryGetValue(previous, out string next))
                {
                    nextWave = next;
                    return 0;
                }
                Debug.LogWarning("Error: No procedural transition was found for " + currentWave);
                return -1;
            default:
                Debug.Log("Error: Spawn method was not specified");
                return -1;
        }
    }
    
    public void SetState(WaveState newState)
    {
        if (debug) Debug.Log("UnitManager changed state from " + currentState + " to " + newState);
        currentState = newState;
    }
}
