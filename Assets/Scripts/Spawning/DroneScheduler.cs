using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DroneController;
using static UnitManager;

/*Schedules drone spawning and retreating. Receives drone spawn data 
from UnitManager to know when to spawn and retreat drones.*/
public class DroneScheduler : MonoBehaviour
{
    [SerializeField] private List<GameObject> dronePrefabs;

    private Dictionary<int, DroneConfig> droneConfigs;
    private Dictionary<string, GameObject> prefabLookup;
    private Dictionary<int, GameObject> activeDrones;
    private Queue<DroneConfig> spawnQueue;
    private Queue<DroneEvent> eventQueue;

    public bool debug = false;

    void Awake()
    {
        prefabLookup = dronePrefabs.ToDictionary(p => p.name, p => p);
        activeDrones = new Dictionary<int, GameObject>();
    }

    public void Initialize(DroneConfig[] configs, DroneEvent[] events)
    {
        droneConfigs = configs.ToDictionary(c => c.droneId);

        spawnQueue = new Queue<DroneConfig>(
            configs.OrderBy(c => c.spawnDelay)
        );

        eventQueue = new Queue<DroneEvent>(
        events.OrderBy(e => e.triggerDelay)
        );

        if (debug) Debug.Log($"DroneScheduler initialized with {configs.Length} config entries.");
    }

    /*Called every frame by UnitManager to spawn drones, and swap their 
    droneActions after triggerDelay to ensure events are called only when 
    drones have spawned*/
    public void Tick(float waveTimer)
    {
        while (spawnQueue.Count > 0 && spawnQueue.Peek().spawnDelay <= waveTimer)
            SpawnDrone(spawnQueue.Dequeue());

        while (eventQueue.Count > 0 && eventQueue.Peek().triggerDelay <= waveTimer)
        {
            var evt = eventQueue.Dequeue();

            if (!activeDrones.TryGetValue(evt.droneId, out var drone))
            {
                Debug.Log($"droneId={evt.droneId} is not active but an event was dequeued and depends on it."); // this may be fine if drone was destroyed by player before it retreats
                continue;
            }

            QueuedDroneAction droneActions = new QueuedDroneAction
            {
                actionsFile = evt.actionsFile,
                delay = evt.triggerDelay,
                destroyOnFinish = evt.destroyOnFinish
            };

            activeDrones[evt.droneId]
                .GetComponent<DroneController>()
                .EnqueueActions(droneActions);
        }
    }

    /*Drones are spawned by Tick() based on spawnDelay*/
    private void SpawnDrone(DroneConfig droneConfig)
    {
        int droneId = droneConfig.droneId;
        if (activeDrones.ContainsKey(droneId))
        {
            Debug.LogWarning($"droneId={droneId} already exists. Skipping spawn.");
            return;
        }
        if (!droneConfigs.TryGetValue(droneId, out var config))
        {
            Debug.LogError($"SpawnDrone(): droneId={droneId} not found.");
            return;
        }
        if (!prefabLookup.TryGetValue(config.prefabName, out var prefab))
        {
            Debug.LogError($"SpawnDrone(): prefab '{config.prefabName}' not found.");
            return;
        }

        GameObject drone = Instantiate(prefab);

        activeDrones[droneId] = drone;

        // if (stats != null) // TODO: make data take effect
        // {
        //     stats.Apply(config.team, config.damageMultiplier, config.healthMultiplier);
        // }

        var controller = drone.GetComponent<DroneController>();
        controller.EnterArena(droneConfig.actionsFile);

        if (debug) Debug.Log($"Spawned droneId={droneId} with actionsFile='{droneConfig.actionsFile}'");
    }
}