using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

public class TechTreeEditorWindow : EditorWindow
{
    TechTreeData data;
    Vector2 scroll;
    TechTreeNode selectedNode;

    enum Mode { Select, Add, Connect, Pan }
    Mode mode = Mode.Select;

    // runtime window rects per node id
    Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();

    // connection state
    string connectSourceId = null;

    [MenuItem("Tools/Tech Tree Editor")]
    public static void ShowWindow()
    {
        GetWindow<TechTreeEditorWindow>("Tech Tree Editor");
    }

    void OnEnable()
    {
        nodeRects = new Dictionary<string, Rect>();
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
                }
            }
        }

        if (GUILayout.Button("Save Data", GUILayout.Width(100)))
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Auto Layout", GUILayout.Width(100)))
        {
            AutoLayoutNodes();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

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
        if (GUILayout.Button("Center View", GUILayout.Width(90))) scroll = Vector2.zero;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // canvas area
        scroll = EditorGUILayout.BeginScrollView(scroll);

        Rect canvas = GUILayoutUtility.GetRect(Mathf.Max(1200, position.width - 20), Mathf.Max(800, position.height - 150));
        GUI.Box(canvas, "");

        // draw grid
        DrawGrid(canvas, 32, new Color(0.15f, 0.15f, 0.15f));

        // ensure rects exist for nodes
        foreach (var n in data.nodes)
        {
            if (n == null) continue;
            if (!nodeRects.ContainsKey(n.id)) nodeRects[n.id] = new Rect(canvas.x + n.position.x, canvas.y + n.position.y, 180, 80);
        }

        // ensure all nodes have ids
        foreach (var n in data.nodes)
        {
            if (n == null) continue;
                if (string.IsNullOrEmpty(n.id))
                {
                    n.id = System.Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(n);
                }
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
            Rect r = nodeRects[n.id];
            r = GUI.Window(winId++, r, id => DrawNodeWindow((string)id.ToString(), n), n.nodeName);
            nodeRects[n.id] = r;
            // update stored node position (relative to canvas)
            n.position = new Vector2(r.x - canvas.x, r.y - canvas.y);

            // snap to three rows (top/mid/bottom)
            float topY = canvas.height * 0.18f;
            float midY = canvas.height * 0.5f;
            float botY = canvas.height * 0.82f;
            float y = n.position.y;
            float distTop = Mathf.Abs(y - topY);
            float distMid = Mathf.Abs(y - midY);
            float distBot = Mathf.Abs(y - botY);
            if (distTop <= distMid && distTop <= distBot) n.position.y = topY;
            else if (distMid <= distTop && distMid <= distBot) n.position.y = midY;
            else n.position.y = botY;

            // compute column from x and store lightly
            float columnWidth = 260f;
            int colIndex = Mathf.RoundToInt(n.position.x / columnWidth);
            n.column = Mathf.Max(0, colIndex);
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
                int desired = Math.Max(toCol, fromCol + 1);
                if (!col.ContainsKey(e.toId) || col[e.toId] < desired)
                {
                    col[e.toId] = desired;
                    changed = true;
                }
            }
        }

        // apply columns and place nodes on canvas grid
        float columnWidth = 260f;
        float startX = 60f;
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
            n.position = new Vector2(x, py);
            EditorUtility.SetDirty(n);
            // update rect
            if (nodeRects.ContainsKey(n.id)) nodeRects[n.id] = new Rect(n.position.x + 50, n.position.y + 50, 180, 80);
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

        // clicking inside window selects it
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            selectedNode = node;
            Repaint();
        }

        GUI.DragWindow();
        GUI.backgroundColor = prev;
    }

    void HandleCanvasInput(Rect canvas)
    {
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        if (mode == Mode.Add && e.type == EventType.MouseDown && e.button == 0 && canvas.Contains(mouse))
        {
            // add node at mouse position
            Vector2 local = mouse - new Vector2(canvas.x, canvas.y);
            var node = CreateNodeAssetAt(local);
            data.nodes.Add(node);
            EditorUtility.SetDirty(data);
            Repaint();
            e.Use();
        }

        if (mode == Mode.Connect && e.type == EventType.MouseDown && e.button == 0)
        {
            // check node clicked
            foreach (var kv in nodeRects)
            {
                if (kv.Value.Contains(mouse))
                {
                    if (connectSourceId == null)
                    {
                        connectSourceId = kv.Key;
                    }
                    else
                    {
                        // create edge from source -> target (only allow left->right)
                        if (connectSourceId != kv.Key)
                        {
                            var a = data.GetNodeById(connectSourceId);
                            var b = data.GetNodeById(kv.Key);
                            if (a != null && b != null)
                            {
                                // require target to be to the right of source (based on x)
                                float ax = nodeRects[a.id].x;
                                float bx = nodeRects[b.id].x;
                                if (bx <= ax)
                                {
                                    EditorUtility.DisplayDialog("Invalid Connection", "Connections must go from left to right. Move the target to the right first.", "OK");
                                }
                                else
                                {
                                    var edge = new TechTreeData.Edge { fromId = connectSourceId, toId = kv.Key };
                                    data.edges.Add(edge);
                                    EditorUtility.SetDirty(data);
                                }
                            }
                        }
                        connectSourceId = null;
                    }
                    e.Use();
                    break;
                }
            }
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
        nodeRects[node.id] = new Rect(pos.x + 50, pos.y + 50, 180, 80);

        return node;
    }
}
