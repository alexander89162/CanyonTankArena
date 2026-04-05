using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TechTreeEditorWindow : EditorWindow
{
    const float NodeWidth = 180f;
    const float NodeHeight = 80f;
    const float ColumnSpacing = 260f;
    const float RowSpacing = 140f;
    const float GridSpacing = 32f;
    const float SnapSpacing = 32f;
    const float CanvasMargin = 60f;

    TechTreeData data;
    Vector2 scroll;
    TechTreeNode selectedNode;

    enum Mode { Select, Add, Connect, Pan }
    Mode mode = Mode.Select;

    // runtime window rects per node id
    Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();

    // connection state
    string connectSourceId = null;
    string connectTargetId = null;

    [MenuItem("Tools/TechTree/Open Editor")]
    public static void ShowWindow()
    {
        GetWindow<TechTreeEditorWindow>("Tech Tree Editor");
    }

    [MenuItem("Tools/TechTree/Setup Current Scene")]
    public static void SetupCurrentScene()
    {
        SetupCurrentScene(GetDefaultDataAsset());
    }

    public static void SetupCurrentScene(TechTreeData data)
    {
        if (data == null)
        {
            EditorUtility.DisplayDialog("Tech Tree Setup", "Assign or create a TechTreeData asset first.", "OK");
            return;
        }

        TechTreeSceneView[] sceneViews = Object.FindObjectsByType<TechTreeSceneView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TechTreeSceneView sceneView = sceneViews.Length > 0 ? sceneViews[0] : null;
        if (sceneView == null)
        {
            GameObject sceneViewObject = new GameObject("TechTreeSceneView");
            sceneView = sceneViewObject.AddComponent<TechTreeSceneView>();
        }

        sceneView.SetData(data);
        EditorUtility.SetDirty(sceneView);
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = sceneView.gameObject;
        Debug.Log("TechTree setup complete in the active scene.");
    }

    static TechTreeData GetDefaultDataAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:TechTreeData");
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<TechTreeData>(path);
    }

    void OnEnable()
    {
        nodeRects = new Dictionary<string, Rect>();
    }

    void EnsureNodeIds()
    {
        if (data == null)
            return;

        foreach (var node in data.nodes)
        {
            if (node == null)
                continue;

            if (string.IsNullOrEmpty(node.id))
            {
                node.id = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(node);
            }
        }
    }

    void SaveData()
    {
        if (data == null)
            return;

        EditorUtility.SetDirty(data);
        foreach (var node in data.nodes)
        {
            if (node != null)
                EditorUtility.SetDirty(node);
        }
        AssetDatabase.SaveAssets();
    }

    Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(
            Mathf.Round(position.x / SnapSpacing) * SnapSpacing,
            Mathf.Round(position.y / SnapSpacing) * SnapSpacing);
    }

    void EnsureStarterTree()
    {
        if (data == null || data.nodes.Count > 0)
            return;

        float startX = 120f;
        float startY = 120f;

        for (int i = 0; i < 3; i++)
        {
            var node = CreateNodeAssetAt(new Vector2(startX, startY + (i * RowSpacing)));
            node.nodeName = $"Node_{i + 1}";
            data.nodes.Add(node);
        }

        SaveData();
        Repaint();
    }

    Rect GetNodeRect(TechTreeNode node, Rect canvas)
    {
        if (node == null)
            return new Rect();

        if (!nodeRects.TryGetValue(node.id, out Rect rect))
        {
            rect = new Rect(canvas.x + node.position.x, canvas.y + node.position.y, NodeWidth, NodeHeight);
            nodeRects[node.id] = rect;
        }

        return rect;
    }

    void UpdateNodeRect(TechTreeNode node, Rect rect)
    {
        if (node == null || string.IsNullOrEmpty(node.id))
            return;

        nodeRects[node.id] = rect;
    }

    bool TryCreateEdge(string fromId, string toId)
    {
        if (data == null || string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId) || fromId == toId)
            return false;

        if (data.edges.Exists(edge => edge.fromId == fromId && edge.toId == toId))
            return false;

        var fromNode = data.GetNodeById(fromId);
        var toNode = data.GetNodeById(toId);
        if (fromNode == null || toNode == null)
            return false;

        if (toNode.position.x <= fromNode.position.x)
        {
            EditorUtility.DisplayDialog("Invalid Connection", "Connections must go from left to right.", "OK");
            return false;
        }

        data.edges.Add(new TechTreeData.Edge { fromId = fromId, toId = toId });
        EditorUtility.SetDirty(data);
        return true;
    }

    TechTreeNode CreateNodeToRightOf(TechTreeNode anchorNode)
    {
        Vector2 position = new Vector2(120f, 120f);

        if (anchorNode != null)
        {
            position = anchorNode.position + new Vector2(ColumnSpacing, 0f);
            int childCount = data.edges.FindAll(edge => edge.fromId == anchorNode.id).Count;
            position.y = anchorNode.position.y + (childCount * 24f);
        }

        position = SnapToGrid(position);

        var node = CreateNodeAssetAt(position);
        data.nodes.Add(node);

        if (anchorNode != null)
            TryCreateEdge(anchorNode.id, node.id);

        return node;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tech Tree Data", GUILayout.Width(110));
        var newData = (TechTreeData)EditorGUILayout.ObjectField(data, typeof(TechTreeData), false);
        if (newData != data)
        {
            data = newData;
            selectedNode = null;
            nodeRects.Clear();
        }

        if (data == null)
        {
            if (GUILayout.Button("Create New Data", GUILayout.Width(140)))
            {
                string path = EditorUtility.SaveFilePanelInProject("Create TechTreeData", "TechTreeData", "asset", "Create tech tree data asset");
                if (!string.IsNullOrEmpty(path))
                {
                    var dt = ScriptableObject.CreateInstance<TechTreeData>();
                    AssetDatabase.CreateAsset(dt, path);
                    AssetDatabase.SaveAssets();
                    data = dt;
                    selectedNode = null;
                    nodeRects.Clear();
                    EnsureStarterTree();
                }
            }
        }

        if (GUILayout.Button("Save Data", GUILayout.Width(100)))
        {
            SaveData();
        }
        EditorGUI.BeginDisabledGroup(data == null);
        if (GUILayout.Button("Auto Layout", GUILayout.Width(100)))
        {
            AutoLayoutNodes();
            SaveData();
        }
        if (GUILayout.Button("Seed Starter Tree", GUILayout.Width(130)))
        {
            EnsureStarterTree();
        }
        if (GUILayout.Button("Setup Current Scene", GUILayout.Width(150)))
        {
            SetupCurrentScene(data);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (data == null)
        {
            EditorGUILayout.HelpBox("No TechTreeData selected. Create or assign an asset above.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // toolbar mode
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(mode == Mode.Select, "Select", "Button")) mode = Mode.Select;
        if (GUILayout.Toggle(mode == Mode.Add, "Add Node", "Button")) mode = Mode.Add;
        if (GUILayout.Toggle(mode == Mode.Connect, "Connect", "Button")) mode = Mode.Connect;
        if (GUILayout.Toggle(mode == Mode.Pan, "Pan", "Button")) mode = Mode.Pan;
        GUILayout.Space(10f);
        GUILayout.Label("Grid:", GUILayout.Width(30f));
        GUILayout.Label(SnapSpacing + " px", GUILayout.Width(52f));
        if (GUILayout.Button("Center View", GUILayout.Width(90))) scroll = Vector2.zero;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // canvas area
        scroll = EditorGUILayout.BeginScrollView(scroll);

        Rect canvas = GUILayoutUtility.GetRect(Mathf.Max(1200, position.width - 20), Mathf.Max(800, position.height - 150));
        GUI.Box(canvas, "");

        EnsureNodeIds();

        // draw grid
        DrawGrid(canvas, GridSpacing, new Color(0.18f, 0.18f, 0.18f));

        // ensure rects exist for nodes
        foreach (var n in data.nodes)
        {
            if (n == null) continue;
            if (!nodeRects.ContainsKey(n.id)) nodeRects[n.id] = new Rect(canvas.x + n.position.x, canvas.y + n.position.y, NodeWidth, NodeHeight);
        }

        if (!string.IsNullOrEmpty(connectSourceId) && nodeRects.TryGetValue(connectSourceId, out Rect sourceRect))
        {
            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(sourceRect.center, Vector3.forward, 7f);
            Handles.EndGUI();
        }

        // draw edges first
        Handles.BeginGUI();
        foreach (var e in data.edges)
        {
            var a = data.GetNodeById(e.fromId);
            var b = data.GetNodeById(e.toId);
            if (a == null || b == null) continue;
            if (!nodeRects.ContainsKey(a.id) || !nodeRects.ContainsKey(b.id)) continue;
            Rect ra = nodeRects[a.id];
            Rect rb = nodeRects[b.id];
            Vector3 start = new Vector3(ra.xMax, ra.center.y, 0);
            Vector3 end = new Vector3(rb.xMin, rb.center.y, 0);
            Vector3 startTangent = start + Vector3.right * 50;
            Vector3 endTangent = end + Vector3.left * 50;
            Handles.DrawBezier(start, end, startTangent, endTangent, Color.cyan, null, 3f);

            // draw arrowhead at end
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float arrowSize = 12f;
            Vector3 p1 = end;
            Vector3 p2 = end - (Vector3)(dir * arrowSize) + (Vector3)(perp * (arrowSize * 0.6f));
            Vector3 p3 = end - (Vector3)(dir * arrowSize) - (Vector3)(perp * (arrowSize * 0.6f));
            Handles.DrawAAConvexPolygon(new Vector3[] { p1, p2, p3 });
        }
        Handles.EndGUI();

        // draw nodes (as draggable windows)
        BeginWindows();
        int winId = 1000;
        foreach (var n in data.nodes)
        {
            if (n == null) continue;
            Rect r = GetNodeRect(n, canvas);
            r = GUI.Window(winId++, r, id => DrawNodeWindow((string)id.ToString(), n), n.nodeName);
            UpdateNodeRect(n, r);

            n.position = SnapToGrid(new Vector2(r.x - canvas.x, r.y - canvas.y));
            n.position.x = Mathf.Max(0f, n.position.x);
            n.position.y = Mathf.Max(0f, n.position.y);
            n.column = Mathf.Max(0, Mathf.RoundToInt(n.position.x / ColumnSpacing));
        }
        EndWindows();

        // handle input for add/connect/select
        HandleCanvasInput(canvas);

        EditorGUILayout.EndScrollView();

        // inspector on right
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box", GUILayout.Height(220));
        EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
        if (selectedNode == null) EditorGUILayout.LabelField("None");
        else
        {
            EditorGUI.BeginChangeCheck();
            selectedNode.nodeName = EditorGUILayout.TextField("Name", selectedNode.nodeName);
            selectedNode.description = EditorGUILayout.TextArea(selectedNode.description, GUILayout.Height(60));
            selectedNode.cost = EditorGUILayout.IntField("Cost", selectedNode.cost);
            selectedNode.icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedNode.icon, typeof(Sprite), false);
            selectedNode.locked = EditorGUILayout.Toggle("Locked", selectedNode.locked);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedNode);
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);
            // list outgoing edges
            for (int i = data.edges.Count - 1; i >= 0; i--)
            {
                var ed = data.edges[i];
                if (ed.fromId == selectedNode.id)
                {
                    var to = data.GetNodeById(ed.toId);
                    string label = to != null ? ("→ " + to.nodeName) : ("→ (missing)");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label);
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        data.edges.RemoveAt(i);
                        EditorUtility.SetDirty(data);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // list incoming edges
            EditorGUILayout.LabelField("Incoming", EditorStyles.boldLabel);
            for (int i = data.edges.Count - 1; i >= 0; i--)
            {
                var ed = data.edges[i];
                if (ed.toId == selectedNode.id)
                {
                    var from = data.GetNodeById(ed.fromId);
                    string label = from != null ? (from.nodeName + " →") : ("(missing) →");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label);
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        data.edges.RemoveAt(i);
                        EditorUtility.SetDirty(data);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(selectedNode);
            if (GUILayout.Button("Delete Node"))
            {
                // remove edges referencing this node
                data.edges.RemoveAll(ed => ed.fromId == selectedNode.id || ed.toId == selectedNode.id);
                data.nodes.Remove(selectedNode);
                string path = AssetDatabase.GetAssetPath(selectedNode);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
                selectedNode = null;
                EditorUtility.SetDirty(data);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    void AutoLayoutNodes()
    {
        if (data == null)
            return;

        // compute columns by traversing edges (parents -> children)
        var nodes = data.nodes;
        Dictionary<string, int> col = new Dictionary<string, int>();

        // nodes with no incoming edges are roots
        foreach (var n in nodes)
        {
            if (n == null) continue;
            bool hasIncoming = data.edges.Exists(e => e.toId == n.id);
            if (!hasIncoming) col[n.id] = 0;
        }

        // propagate columns
        bool changed = true;
        int safety = 0;
        while (changed && safety < 100)
        {
            changed = false;
            safety++;
            foreach (var e in data.edges)
            {
                if (string.IsNullOrEmpty(e.fromId) || string.IsNullOrEmpty(e.toId)) continue;
                int fromCol = col.ContainsKey(e.fromId) ? col[e.fromId] : 0;
                int toCol = col.ContainsKey(e.toId) ? col[e.toId] : -1;
                int desired = Mathf.Max(toCol, fromCol + 1);
                if (!col.ContainsKey(e.toId) || col[e.toId] < desired)
                {
                    col[e.toId] = desired;
                    changed = true;
                }
            }
        }

        // apply columns and place nodes on canvas grid
        float columnWidth = ColumnSpacing;
        float startX = CanvasMargin;
        float topY = position.height * 0.18f;
        float midY = position.height * 0.5f;
        float botY = position.height * 0.82f;

        foreach (var n in nodes)
        {
            if (n == null) continue;
            int c = col.ContainsKey(n.id) ? col[n.id] : 0;
            n.column = c;
            float x = startX + c * columnWidth;
            // determine row index from current y
            float y = n.position.y;
            float distTop = Mathf.Abs(y - topY);
            float distMid = Mathf.Abs(y - midY);
            float distBot = Mathf.Abs(y - botY);
            int rowIndex = 1;
            if (distTop <= distMid && distTop <= distBot) rowIndex = 0;
            else if (distMid <= distTop && distMid <= distBot) rowIndex = 1;
            else rowIndex = 2;
            n.row = rowIndex;
            float py = rowIndex == 0 ? topY : (rowIndex == 1 ? midY : botY);
            n.position = SnapToGrid(new Vector2(x, py));
            EditorUtility.SetDirty(n);
            // update rect
            if (nodeRects.ContainsKey(n.id)) nodeRects[n.id] = new Rect(n.position.x + 50, n.position.y + 50, NodeWidth, NodeHeight);
        }
    }

    void DrawGrid(Rect canvas, float gridSpacing, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(canvas.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(canvas.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = gridColor;
        for (int i = 0; i < widthDivs; i++)
        {
            float x = canvas.x + i * gridSpacing;
            Handles.DrawLine(new Vector3(x, canvas.y, 0), new Vector3(x, canvas.y + canvas.height, 0));
        }
        for (int j = 0; j < heightDivs; j++)
        {
            float y = canvas.y + j * gridSpacing;
            Handles.DrawLine(new Vector3(canvas.x, y, 0), new Vector3(canvas.x + canvas.width, y, 0));
        }
        Handles.color = Color.white;
        Handles.EndGUI();
    }

    void DrawNodeWindow(string id, TechTreeNode node)
    {
        Color prev = GUI.backgroundColor;
        if (node.locked) GUI.backgroundColor = Color.grey * 0.9f;

        EditorGUILayout.BeginHorizontal();
        if (node.icon != null)
        {
            GUILayout.Label(AssetPreview.GetAssetPreview(node.icon), GUILayout.Width(40), GUILayout.Height(40));
        }
        EditorGUILayout.BeginVertical();
        GUILayout.Label(node.nodeName, EditorStyles.boldLabel);
        GUILayout.Label("Cost: " + node.cost);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        // clicking inside the window selects it or sets up a connection
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            HandleNodeClick(node);
            Event.current.Use();
            Repaint();
        }

        GUI.DragWindow();
        GUI.backgroundColor = prev;
    }

    void HandleNodeClick(TechTreeNode node)
    {
        if (node == null)
            return;

        if (mode != Mode.Connect)
        {
            selectedNode = node;
            return;
        }

        if (connectSourceId == null)
        {
            connectSourceId = node.id;
            connectTargetId = null;
            selectedNode = node;
            return;
        }

        if (connectSourceId == node.id)
        {
            connectSourceId = null;
            connectTargetId = null;
            selectedNode = node;
            return;
        }

        connectTargetId = node.id;
        if (TryCreateEdge(connectSourceId, connectTargetId))
        {
            selectedNode = node;
            SaveData();
        }

        connectSourceId = null;
        connectTargetId = null;
    }

    void HandleCanvasInput(Rect canvas)
    {
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        if (mode == Mode.Add && e.type == EventType.MouseDown && e.button == 0 && canvas.Contains(mouse))
        {
            Vector2 local = mouse - new Vector2(canvas.x, canvas.y);
            var node = CreateNodeToRightOf(selectedNode);

            if (selectedNode == null)
            {
                node.position = SnapToGrid(new Vector2(Mathf.Max(0f, local.x), Mathf.Max(0f, local.y)));
                node.column = Mathf.Max(0, Mathf.RoundToInt(node.position.x / ColumnSpacing));
                UpdateNodeRect(node, new Rect(canvas.x + node.position.x, canvas.y + node.position.y, NodeWidth, NodeHeight));
            }

            selectedNode = node;
            SaveData();
            Repaint();
            e.Use();
        }
    }

    TechTreeNode CreateNodeAssetAt(Vector2 pos)
    {
        string folder = "Assets/TechTreeNodes";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "TechTreeNodes");
        }

        var node = ScriptableObject.CreateInstance<TechTreeNode>();
        node.nodeName = "Node_" + (data.nodes.Count + 1);
        node.position = pos;
        node.id = System.Guid.NewGuid().ToString();

        string baseName = Path.Combine(folder, node.nodeName + ".asset");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(baseName);
        AssetDatabase.CreateAsset(node, assetPath);
        AssetDatabase.SaveAssets();

        // create runtime rect
        nodeRects[node.id] = new Rect(pos.x + 50, pos.y + 50, NodeWidth, NodeHeight);

        return node;
    }
}
