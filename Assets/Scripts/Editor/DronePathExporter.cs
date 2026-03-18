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

        if (GUILayout.Button("Export Path"))
        {
            if (arrowParent == null)
            {
                Debug.LogError("Please assign an arrow parent before exporting.");
            }
            else
            {
                ExportPath();
            }
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
                endVelocity = 40f,
                accelerationType = id < 0 ? "n/a" : "linear",
                rotationType = id < 0 ? "n/a" : "linear"
            });
        }

        moves = moves.OrderBy(m => m.moveId).ToList();

        string folderPath = Path.Combine(Application.streamingAssetsPath, "DroneActions");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string path = Path.Combine(folderPath, jsonFileName + ".json");

        // Load existing data if file exists, otherwise start fresh
        DroneActions actions = new DroneActions();
        if (File.Exists(path))
            actions = JsonUtility.FromJson<DroneActions>(File.ReadAllText(path));

        // Overwrite only movements
        actions.movements = moves.ToArray();

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
        public Vector3 rotation;
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