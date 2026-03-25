using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DroneController;

/*Automatically build drone path.
Note: drone armature needs to be Rotation {-90, 0, 0} for this to work.*/
public class DronePathExporterWindow : EditorWindow
{
    private Transform arrowParent;
    private string jsonFileName = "dronePath1";
    private float defaultVelocity = 200f;
    private bool overrideBrakingManeuvers = false;
    private float brakingRecoilDistance = 20f;
    private float brakingRecoilDuration = 0.5f;
    private float brakingSettleDuration = 0.7f;

    [MenuItem("Tools/Drone/Path Exporter")]
    public static void ShowWindow()
    {
        GetWindow<DronePathExporterWindow>("Drone Path Exporter");
    }

    private void OnGUI()
    {
        if (arrowParent == null)
        {
            GameObject arrows = GameObject.Find("Arrows");
            if (arrows != null)
                arrowParent = arrows.transform;
        }

        GUILayout.Label("Drone Path Exporter", EditorStyles.boldLabel);

        arrowParent = (Transform)EditorGUILayout.ObjectField("Arrow Parent", arrowParent, typeof(Transform), true);
        jsonFileName = EditorGUILayout.TextField("JSON File Name", jsonFileName);
        defaultVelocity = EditorGUILayout.FloatField("Default velocity", defaultVelocity);
        
        GUILayout.Space(8);
        overrideBrakingManeuvers = EditorGUILayout.Toggle("Override Braking Maneuvers", overrideBrakingManeuvers);
        if (overrideBrakingManeuvers)
        {
            EditorGUI.indentLevel++;
            brakingRecoilDistance = EditorGUILayout.FloatField("Recoil Distance", brakingRecoilDistance);
            brakingRecoilDuration = EditorGUILayout.FloatField("Recoil Duration", brakingRecoilDuration);
            brakingSettleDuration = EditorGUILayout.FloatField("Settle Duration", brakingSettleDuration);
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("Export Path"))
        {
            if (arrowParent == null)
                Debug.LogError("Please assign an arrow parent before exporting.");
            else
                ExportPath();
        }
    }

    private void ExportPath()
    {
        List<MoveJson> moves = new List<MoveJson>();

        foreach (Transform child in arrowParent)
        {
            int id = ExtractId(child.name);

            if (id == int.MinValue)
            {
                Debug.LogWarning("Skipping object with invalid name: " + child.name);
                continue;
            }

            moves.Add(new MoveJson
            {
                moveId = id,
                position = child.position,
                rotation = child.rotation.eulerAngles,
                endVelocity = defaultVelocity,
                accelerationType = id < 0 ? "n/a" : "linear",
                rotationType = id < 0 ? "n/a" : "linear"
            });
        }

        moves = moves.OrderBy(m => m.moveId).ToList();

        if (moves.Count > 0)
        {
            var lastNode = moves[moves.Count - 1];
            lastNode.endVelocity *= 0.2f;
            lastNode.accelerationType = "quadraticDecreasing";
            moves[moves.Count - 1] = lastNode;
        }

        string folderPath = Path.Combine(Application.streamingAssetsPath, "DroneActions");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string path = Path.Combine(folderPath, jsonFileName + ".json");

        // Load existing data if file exists, otherwise start fresh
        DroneActions actions = new DroneActions();
        if (File.Exists(path))
            actions = JsonUtility.FromJson<DroneActions>(File.ReadAllText(path));

        // Overwrite only movements
        actions.movements = moves.ToArray();

        if (overrideBrakingManeuvers)
            actions.brakingManeuvers = BuildBrakingManeuvers(moves).ToArray();

        File.WriteAllText(path, JsonUtility.ToJson(actions, true));
        Debug.Log("Drone path exported to: " + path);

            Debug.Log("Drone path exported to: " + path);
        }

    private int ExtractId(string name)
    {
        int start = name.IndexOf('(');
        int end = name.IndexOf(')');

        if (start == -1 || end == -1)
            return int.MinValue;

        string number = name.Substring(start + 1, end - start - 1);

        if (int.TryParse(number, out int id))
            return id;

        return int.MinValue;
    }

    private List<BrakingManeuver> BuildBrakingManeuvers(List<MoveJson> moves)
    {
        var maneuvers = new List<BrakingManeuver>();

        if (moves.Count < 2) return maneuvers;

        // Derive the approach direction from the last two nodes
        Vector3 lastDir = (moves[moves.Count - 1].position - moves[moves.Count - 2].position).normalized;

        // Maneuver 0: recoil backward against travel direction
        maneuvers.Add(new BrakingManeuver
        {
            targetTilt = Vector3.zero,
            duration = brakingRecoilDuration,
            outwardMove = -brakingRecoilDistance  // negative = backward along lastDir
        });

        // Maneuver 1: settle forward back to stop position
        maneuvers.Add(new BrakingManeuver
        {
            targetTilt = Vector3.zero,
            duration = brakingSettleDuration,
            outwardMove = brakingRecoilDistance   // return to where we came from
        });

        return maneuvers;
    }

    [System.Serializable]
    private class DroneActions
    {
        public MoveJson[] movements;
        public BrakingManeuver[] brakingManeuvers;
        public DeploymentAction[] deploymentActions;
    }

    [System.Serializable]
    private class BrakingManeuver
    {
        public Vector3 targetTilt;
        public float duration;
        public float outwardMove;
    }

    [System.Serializable]
    private class DeploymentAction
    {
        public string action;
        public int activationNode;
        public float startDelay;
        public float duration;
    }
}