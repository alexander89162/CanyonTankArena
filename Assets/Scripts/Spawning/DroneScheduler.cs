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
    private Queue<DroneEvent> eventQueue;

    public bool debug = false;

    void Awake()
    {
        prefabLookup = dronePrefabs.ToDictionary(p => p.name, p => p);
    }

    public void Initialize(DroneConfig[] configs, DroneEvent[] events)
    {
        activeDrones = new Dictionary<int, GameObject>();

        droneConfigs = configs.ToDictionary(c => c.droneId);

        eventQueue = new Queue<DroneEvent>(
        events.OrderBy(e => e.triggerDelay)
        );

        if (debug) Debug.Log($"DroneScheduler initialized with {configs.Length} entries.");
    }

    public void Tick(float waveTimer)
    {
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
                delay = evt.triggerDelay
            };

            activeDrones[evt.droneId]
                .GetComponent<DroneController>()
                .EnqueueActions(droneActions);
        }
    }

    public void SpawnDrone(DroneConfig droneConfig)
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
        drone.SetActive(false);

        activeDrones[droneId] = drone;

        // if (stats != null) // TODO: make data take effect
        // {
        //     stats.Apply(config.team, config.damageMultiplier, config.healthMultiplier);
        // }

        var controller = drone.GetComponent<DroneController>();
        drone.SetActive(true);
        controller.EnterArena(droneConfig.actionsFile);
    }
}