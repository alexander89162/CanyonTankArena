using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/*Given a series of Move instructions, this component automatically moves 
and animates the drone
Note: drone armature needs to be Rotation {-90, 180, 0} for this to work.*/
public class DroneController : MonoBehaviour
{
    public string droneActions; // the file containing the specific actions this drone will follow
    private List<Move> moves;
    private List<BrakingManeuver> brakingManeuvers;
    private List<DeploymentAction> deploymentActions;
    private int currentNodeIndex = 1;
    ControllerState currentState = ControllerState.Forward;
    private float elapsedTime = 0f;
    private float segmentDuration = 3f;
    private float segmentTimer = 0f;
    private Quaternion startRot;

    private enum ControllerState
    {
        InitializingController, // not ready to move or animate yet
        Forward, // leans the drone forward and tilts based on next path node
        StabilizingFromStop, // rock back and forth to simulate realistic stopping
        Idle, // Rock up and down, small sideways noise which should always return to original position and tilt gently while doing so
    }

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
        public AccelerationType accelerationType; // "linear" or "quadratic"
        public RotationType rotationType; // "linear" or "Slerp"
        public Move(MoveJson json, Quaternion rotOffset)
        {
            moveId = json.moveId;
            position = json.position;
            Quaternion jsonRot = Quaternion.Euler(json.rotation);
            rotation = jsonRot * rotOffset;
            //rotation = Quaternion.Euler(json.rotation);
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
        public Quaternion rotation;
        public float duration;
        public float outwardMove;
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
        Unknown,
        Linear,
        Quadratic
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
        droneActions = "temp"; // hard-coded during testing
        startRot = Quaternion.Euler(-90f, 180f, 0f);

        SetState(ControllerState.InitializingController);
        StartCoroutine(InitializeDroneActions());
    }

    void Update()
    {
        switch (currentState)
        {
            case ControllerState.InitializingController: break;
            case ControllerState.Forward:
                elapsedTime += Time.deltaTime;
                segmentTimer += Time.deltaTime;

                float posFac = segmentTimer / segmentDuration;
                posFac = Mathf.Clamp01(posFac);
                float rotFac = segmentTimer / segmentDuration;
                rotFac = Mathf.Clamp01(rotFac);

                transform.position = Vector3.Lerp(moves[currentNodeIndex - 1].position, moves[currentNodeIndex].position, posFac);
                transform.rotation = Quaternion.Lerp(moves[currentNodeIndex - 1].rotation, moves[currentNodeIndex].rotation, rotFac);
                
                if (posFac >= 1)
                {
                    posFac = 0; segmentTimer = 0;
                    if (currentNodeIndex + 1 < moves.Count)
                        currentNodeIndex++;
                    else
                        SetState(ControllerState.StabilizingFromStop);
                }
                
                break;
            case ControllerState.StabilizingFromStop:
                elapsedTime += Time.deltaTime;
                break;
            case ControllerState.Idle:
                elapsedTime += Time.deltaTime;
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
            droneActions + ".json"
        );

        path = "file://" + path;

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
        if (!ValidateDroneActions(actions)) // TODO: fix validation, it does nothing right now
        {
            Debug.LogError($"DroneController on {name} received invalid action data. Destroying drone.");
            Destroy(gameObject);
            yield break;
        }

        // 3) Cache the data we need
        foreach (var m in actions.movements)
            moves.Add(new Move(m, startRot));
        brakingManeuvers = new List<BrakingManeuver>(actions.brakingManeuvers);
        deploymentActions = new List<DeploymentAction>(actions.deploymentActions);

        // 4) Done initializing
        if (debug) Debug.Log("DroneController finished initialization");
        SetState(ControllerState.Forward);
    }

    private bool ValidateDroneActions(DroneActions actions)
    {
        // 1) Interpolation methods must be valid
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            if (!System.Enum.IsDefined(typeof(AccelerationType), move.accelerationType) || move.accelerationType == AccelerationType.Unknown)
            {
                Debug.LogError($"Acceleration type is invalid.");
                return false;   
            }
            if (!System.Enum.IsDefined(typeof(RotationType), move.rotationType) || move.rotationType == RotationType.Unknown)
            {
                Debug.LogError($"Rotation type is invalid.");
                return false;   
            }
        }

        if (debug) Debug.Log("DroneController passed droneActions validation");
        return true;
    }

    private float ApplyAcceleration(float t, AccelerationType type)
    {
        switch (type)
        {
            case AccelerationType.Linear: return t;
            case AccelerationType.Quadratic: return t * t;
            default: return t;
        }
    }

    private void SetState(ControllerState newState){ if (debug) Debug.Log($"DroneController went from {currentState} to {newState}"); currentState = newState; }
}