using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static UnitManager;

/*Given a series of instructions via json, this component automatically guides 
the drone's movement, rotation, attack pattern, and troop deployment*/
public class DroneController : MonoBehaviour
{
    public string droneActions; // the file containing the actions this drone will follow
    private List<Move> moves;
    private List<BrakingManeuver> brakingManeuvers;
    private List<DeploymentAction> deploymentActions;
    private int currentNodeIndex = 1;
    private ControllerState currentState = ControllerState.Forward;
    private float timeIdle = 0f; // time since drone became idle
    private float segmentTimer = 0f; // reset on move to next segment
    private float segmentDuration = 0f;
    private int brakingIndex = 0;
    private Vector3 maneuverStartPos;
    private Quaternion maneuverStartRot;
    private Vector3 maneuverEndPos;
    private Quaternion maneuverEndRot;

    [Header("Idle Hover")]
    public float hoverAmplitude = 10.0f;   // vertical travel in world units
    public float hoverFrequency = 0.35f;   // oscillations per second
    private Vector3 hoverOrigin;

    [Header("Rigging")]
    public Transform[] propellers;
    public float propellerSpeed = 1800f;

    private enum ControllerState
    {
        InitializingController, // not ready to move or animate yet
        Forward, // leans the drone forward and tilts based on next path node
        StabilizingFromStop, // rock back and forth to simulate realistic stopping
        Idle, // Rock up and down, small sideways noise which should always return to original position and tilt gently while doing so
    }
    [System.Serializable]
    public class DroneActions
    {
        public MoveJson[] movements;
        public BrakingManeuver[] brakingManeuvers;
        public DeploymentAction[] deploymentActions;
    }
    public struct Move
    {
        public int moveId;
        public Vector3 position;
        public Quaternion rotation;
        public float endVelocity;
        public AccelerationType accelerationType;
        public RotationType rotationType;
        public Move(MoveJson json)
        {
            moveId = json.moveId;
            position = json.position;
            rotation = Quaternion.Euler(json.rotation);
            endVelocity = json.endVelocity;

            accelerationType =
                System.Enum.TryParse(json.accelerationType, true, out AccelerationType a)
                ? a : AccelerationType.Unknown;

            rotationType =
                System.Enum.TryParse(json.rotationType, true, out RotationType r)
                ? r : RotationType.Unknown;
        }
    }
    [System.Serializable]
    public struct MoveJson
    {
        public int moveId;
        public Vector3 position;
        public Vector3 rotation;
        public float endVelocity;
        public string accelerationType;
        public string rotationType;
    }
    [System.Serializable]
    public struct BrakingManeuver
    {
        public Vector3 targetTilt;
        public float duration;
        public float outwardMove;

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(targetTilt);
        }
    }
    [System.Serializable]
    public struct DeploymentAction
    {
        public string action;
        public int activationNode;
        public float startDelay;
        public float duration;
    }
    public enum AccelerationType
    {
        Unknown, // fails on validation
        Linear, // constant acceleration
        QuadraticIncreasing, // increasingly fast shift
        QuadraticDecreasing // decreasingly fast shift
    }
    public enum RotationType
    {
        Unknown,
        Linear,
        Slerp
    }

    public bool debug = false;

    void Awake()
    {
        SetState(ControllerState.InitializingController);
        Initialize(droneActions); // TODO: remove this and UnitManager calls it
    }

    // Drone will begin moving from here
    public void Initialize(string actionsFile)
    {
        droneActions = actionsFile;
        StartCoroutine(InitializeDroneActions());
    }

    // drone will leave the arena from here
    public void FlyAway(string exitActionsFile)
    {
        StopAllCoroutines();
        Initialize(exitActionsFile);
    }

    void Update()
    {
        // 1) Rotate propellers
        foreach (Transform p in propellers)
        p.Rotate(Vector3.up, propellerSpeed * Time.deltaTime, Space.Self);

        // 2) Movement and rotation of drone as a whole
        switch (currentState)
        {
            case ControllerState.InitializingController: return;
            case ControllerState.Forward:
                segmentTimer += Time.deltaTime;

                float t = Mathf.Clamp01(segmentTimer / segmentDuration);
                t = ApplyAcceleration(t, moves[currentNodeIndex].accelerationType);
                if (debug) Debug.Log($"at nodeId={currentNodeIndex}, we have t={t} before clamp");
                t = Mathf.Clamp01(t);

                // position interpolation
                Vector3 p0 = moves[Mathf.Max(currentNodeIndex - 2, 0)].position;
                Vector3 p1 = moves[currentNodeIndex - 1].position;
                Vector3 p2 = moves[currentNodeIndex].position;
                Vector3 p3 = moves[Mathf.Min(currentNodeIndex + 1, moves.Count - 1)].position;
                transform.position = CatmullRom(p0, p1, p2, p3, t);

                // rotation interpolation
                transform.rotation = Quaternion.Slerp(moves[currentNodeIndex - 1].rotation, moves[currentNodeIndex].rotation, t);

                if (t < 0f)
                {
                    if (debug) Debug.Log($"'t' was negative at nodeId={currentNodeIndex}. Ending interpolation phase immediately.");
                    SetState(ControllerState.StabilizingFromStop);
                }
                else if (t >= 1)
                {
                    if (debug) Debug.Log($"t>=1 so we snap to current position and rotation at currentNodeIndex={currentNodeIndex}");
                    segmentTimer = 0;
                    transform.position = moves[currentNodeIndex].position;
                    transform.rotation = moves[currentNodeIndex].rotation;

                    if (currentNodeIndex + 1 < moves.Count)
                    {
                        currentNodeIndex++;
                        if (debug) Debug.Log($"Incremented currentNodeIndex to currentNodeIndex={currentNodeIndex}");
                        segmentDuration = ComputeSegmentDuration(
                            moves[currentNodeIndex - 1],
                            moves[currentNodeIndex]
                        );
                    }
                    else
                    {
                        if (debug) Debug.Log($"(passed last node) segmentTimer/segmentDuration={segmentTimer/segmentDuration}");
                        SetState(ControllerState.StabilizingFromStop);
                    }

                }
                
                break;
            case ControllerState.StabilizingFromStop:
                segmentTimer += Time.deltaTime;

                t = segmentTimer / segmentDuration;
                t = Mathf.Clamp01(t);

                transform.position = Vector3.Lerp(maneuverStartPos, maneuverEndPos, t);
                transform.rotation = Quaternion.Slerp(maneuverStartRot, maneuverEndRot, t);

                if (t >= 1)
                {
                    if (brakingIndex + 1 < brakingManeuvers.Count)
                    {
                        brakingIndex++;
                        UpdateBrakingManeuverValues(brakingIndex);
                    }
                    else
                        SetState(ControllerState.Idle);
                } 

                break;
            case ControllerState.Idle:
                timeIdle += Time.deltaTime;
                segmentTimer+= Time.deltaTime;

                float hoverOffset = Mathf.Sin(segmentTimer * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
                transform.position = hoverOrigin + Vector3.up * hoverOffset;
                break;
        }
    }

    private IEnumerator InitializeDroneActions()
    /*Extract drone path data from JSON*/
    {
        // 1) Extract data from json
        string path = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "DroneActions",
            droneActions.Trim() + ".json"
        );

        path = "file://" + path;
        if (debug) Debug.Log($"droneActions evaluated to {droneActions}.\nAttempted to read from path {path}");

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load drone actions: " + request.error);
            Destroy(gameObject);
            yield break;
        }

        DroneActions actions =
            JsonUtility.FromJson<DroneActions>(
                request.downloadHandler.text
            );
        if (debug) Debug.Log("DroneController finished parsing JSON");

        // 2) Validation: fail = delete this drone to avoid crashes
        moves = new List<Move>(actions.movements.Length);
        brakingManeuvers = new List<BrakingManeuver>(actions.brakingManeuvers);
        deploymentActions = new List<DeploymentAction>(actions.deploymentActions);
        if (!ValidateDroneActions(actions))
        {
            Debug.LogError($"DroneController on {name} received invalid action data. Destroying drone.");
            Destroy(gameObject);
            yield break;
        }

        // 3) Cache the data we need
        foreach (var m in actions.movements)
            moves.Add(new Move(m));

        // 4) Final initialization work goes here
        if (moves.Count >= 2)
            segmentDuration = ComputeSegmentDuration(moves[0], moves[1]);

        // 5) Done initializing: reset state
        currentNodeIndex = 1;
        brakingIndex = 0;
        timeIdle = 0f;
        segmentTimer = 0f;

        if (debug) Debug.Log("DroneController finished initialization");
        if (moves.Count >= 2)
            SetState(ControllerState.Forward);
        else
            SetState(ControllerState.Idle);
    }

    private bool ValidateDroneActions(DroneActions actions)
    {
        // 1) There should be no nulls
        if (actions.movements == null)
            Debug.LogError("movements is NULL in actions");
        if (actions.brakingManeuvers == null)
            Debug.LogError("brakingManeuvers is NULL in actions");
        if (actions.deploymentActions == null)
            Debug.LogError("deploymentActions is NULL in actions");

        // 2) Interpolation methods must be valid
        for (int i = 0; i < actions.movements.Length; i++)
        {
            var move = actions.movements[i];
            if (!System.Enum.TryParse(move.accelerationType, true, out AccelerationType a) || a == AccelerationType.Unknown)
            {
                Debug.LogError("Acceleration type is invalid");
                return false;
            }

            if (!System.Enum.TryParse(move.rotationType, true, out RotationType r) || r == RotationType.Unknown)
            {
                Debug.LogError("Rotation type is invalid");
                return false;
            }
        }

        if (debug) Debug.Log("DroneController passed droneActions validation");
        return true;
    }

    ///<summary>Returns the duration of a segment based on its initial and final velocities.</summary>
    private float ComputeSegmentDuration(Move a, Move b)
    {
        float distance = Vector3.Distance(a.position, b.position);
        float avgVelocity = Mathf.Max(0.01f, (a.endVelocity + b.endVelocity) * 0.5f);

        return distance / avgVelocity;
    }

    ///<summary>Given the interpolation factor t, return the new position at the given frame</summary>
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f*p0 - 5f*p1 + 4f*p2 - p3) * t2 +
            (-p0 + 3f*p1 - 3f*p2 + p3) * t3
        );
    }

    ///<summary>Skews the interpolation factor t to simulate accelerated movement. Keeps the segment's time duration the same.</summary>
    private float ApplyAcceleration(float t, AccelerationType type)
    {
        switch (type)
        {
            case AccelerationType.Linear: return t;
            case AccelerationType.QuadraticIncreasing: return t*t;
            case AccelerationType.QuadraticDecreasing: return t * (2 - t);
            default: 
                Debug.LogWarning($"ApplyAcceleration defaulted to returning 't' at nodeId={moves[currentNodeIndex].moveId} because accelerationType did not match any enum entry");
                return t;
        }
    }

    ///<summary>Update state of all values related to braking maneuvers to match current index</summary>
    void UpdateBrakingManeuverValues(int index)
    {
        var maneuver = brakingManeuvers[index];

        segmentTimer = 0f;
        segmentDuration = Mathf.Max(0.0001f, maneuver.duration);

        maneuverStartPos = transform.position;
        maneuverStartRot = transform.rotation;

        Vector3 baseDirection = Vector3.forward;
        baseDirection = (moves[moves.Count - 1].position - moves[moves.Count - 2].position).normalized;

        maneuverEndPos = maneuverStartPos + baseDirection * maneuver.outwardMove;
        maneuverEndRot = maneuverStartRot * Quaternion.Euler(maneuver.targetTilt);
    }

    public void EnterArena()
    {
        // Reset state
        currentNodeIndex = 0;
        // TODO

        // Load new drone actions
        SetState(ControllerState.InitializingController);
        Initialize(droneActions);

        PerformNextEvent();
    }

    public void ExitArena()
    {
        StopAllCoroutines();
        SetState(ControllerState.Idle);
        PerformNextEvent();
    }

    public void PerformNextEvent()
    {
        //
    }

    private void SetState(ControllerState newState)
    {
        if (debug) Debug.Log($"DroneController went from {currentState} to {newState}");
        currentState = newState;

        segmentTimer = 0f;

        if (newState == ControllerState.StabilizingFromStop)
        {
            brakingIndex = 0;
            if (brakingManeuvers.Count > 0)
                UpdateBrakingManeuverValues(0);
            else
                SetState(ControllerState.Idle);
        }
        else if (newState == ControllerState.Idle)
        {
            hoverOrigin = transform.position;
        }
    }
}