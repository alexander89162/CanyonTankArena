using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechTreeSceneView : MonoBehaviour
{
    [SerializeField] private TechTreeData data;
    [SerializeField] private Vector2 nodeSize = new Vector2(220f, 96f);
    [SerializeField] private Color nodeColor = new Color(0.14f, 0.18f, 0.16f, 0.96f);
    [SerializeField] private Color nodeLockedColor = new Color(0.32f, 0.32f, 0.32f, 0.95f);
    [SerializeField] private Color edgeColor = new Color(0.62f, 0.86f, 1f, 0.95f);
    [SerializeField] private Color titleColor = new Color(1f, 1f, 1f, 0.98f);
    [SerializeField] private Color subtitleColor = new Color(0.78f, 0.84f, 0.78f, 0.95f);

    private Canvas canvas;
    private RectTransform nodeLayer;
    private RectTransform edgeLayer;
    private readonly Dictionary<string, RectTransform> nodeRects = new Dictionary<string, RectTransform>();
    private int lastHash;
    private bool rebuildQueued;

    public void SetData(TechTreeData newData)
    {
        data = newData;
        QueueRebuild();
    }

    public TechTreeData GetData()
    {
        return data;
    }

    void OnEnable()
    {
        QueueRebuild();
    }

    void OnValidate()
    {
        QueueRebuild();
    }

    void Update()
    {
        if (Application.isPlaying)
            return;

        int currentHash = ComputeDataHash();
        if (currentHash != lastHash)
        {
            QueueRebuild();
        }
    }

    void QueueRebuild()
    {
        if (Application.isPlaying)
        {
            Rebuild();
            return;
        }

#if UNITY_EDITOR
        if (rebuildQueued)
            return;

        rebuildQueued = true;
        EditorApplication.delayCall += RebuildDeferred;
#else
        Rebuild();
#endif
    }

#if UNITY_EDITOR
    void RebuildDeferred()
    {
        EditorApplication.delayCall -= RebuildDeferred;
        rebuildQueued = false;

        if (this == null)
            return;

        Rebuild();
    }
#endif

    void EnsureCanvas()
    {
        if (canvas != null)
            return;

        Transform existing = transform.Find("TechTreeCanvas");
        if (existing != null)
        {
            canvas = existing.GetComponent<Canvas>();
            if (canvas != null)
            {
                nodeLayer = existing.Find("NodeLayer") as RectTransform;
                edgeLayer = existing.Find("EdgeLayer") as RectTransform;
                return;
            }
        }

        GameObject canvasObject = new GameObject("TechTreeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject edgeLayerObject = new GameObject("EdgeLayer", typeof(RectTransform));
        edgeLayerObject.transform.SetParent(canvasObject.transform, false);
        edgeLayer = edgeLayerObject.GetComponent<RectTransform>();
        edgeLayer.anchorMin = Vector2.zero;
        edgeLayer.anchorMax = Vector2.one;
        edgeLayer.offsetMin = Vector2.zero;
        edgeLayer.offsetMax = Vector2.zero;

        GameObject nodeLayerObject = new GameObject("NodeLayer", typeof(RectTransform));
        nodeLayerObject.transform.SetParent(canvasObject.transform, false);
        nodeLayer = nodeLayerObject.GetComponent<RectTransform>();
        nodeLayer.anchorMin = Vector2.zero;
        nodeLayer.anchorMax = Vector2.one;
        nodeLayer.offsetMin = Vector2.zero;
        nodeLayer.offsetMax = Vector2.zero;
    }

    void Rebuild()
    {
        EnsureCanvas();

        ClearChildren(edgeLayer);
        ClearChildren(nodeLayer);
        nodeRects.Clear();

        if (data == null || data.nodes == null)
        {
            lastHash = 0;
            return;
        }

        foreach (var node in data.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
                continue;

            RectTransform rect = CreateNodeView(node);
            nodeRects[node.id] = rect;
        }

        foreach (var edge in data.edges)
        {
            if (edge == null)
                continue;

            if (!nodeRects.TryGetValue(edge.fromId, out RectTransform fromRect))
                continue;

            if (!nodeRects.TryGetValue(edge.toId, out RectTransform toRect))
                continue;

            CreateEdgeView(fromRect, toRect);
        }

        lastHash = ComputeDataHash();
    }

    int ComputeDataHash()
    {
        if (data == null)
            return 0;

        int hash = 17;
        if (data.nodes != null)
        {
            foreach (var node in data.nodes)
            {
                if (node == null)
                    continue;

                hash = hash * 31 + (node.id != null ? node.id.GetHashCode() : 0);
                hash = hash * 31 + (node.nodeName != null ? node.nodeName.GetHashCode() : 0);
                hash = hash * 31 + node.position.GetHashCode();
                hash = hash * 31 + node.locked.GetHashCode();
            }
        }

        if (data.edges != null)
        {
            foreach (var edge in data.edges)
            {
                if (edge == null)
                    continue;

                hash = hash * 31 + (edge.fromId != null ? edge.fromId.GetHashCode() : 0);
                hash = hash * 31 + (edge.toId != null ? edge.toId.GetHashCode() : 0);
            }
        }

        return hash;
    }

    void ClearChildren(RectTransform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    static Sprite fallbackSprite;

    static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        Texture2D texture = Texture2D.whiteTexture;
        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        fallbackSprite.name = "TechTreeFallbackSprite";
        return fallbackSprite;
    }

    RectTransform CreateNodeView(TechTreeNode node)
    {
        GameObject nodeObject = new GameObject(node.nodeName, typeof(RectTransform), typeof(Image), typeof(Button));
        nodeObject.transform.SetParent(nodeLayer, false);

        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = nodeSize;
        rect.anchoredPosition = new Vector2(node.position.x, -node.position.y);

        Image background = nodeObject.GetComponent<Image>();
        background.sprite = GetFallbackSprite();
        background.type = Image.Type.Simple;
        background.color = node.locked ? nodeLockedColor : nodeColor;

        Button button = nodeObject.GetComponent<Button>();
        button.onClick.AddListener(() => OnNodeClicked(node));

        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(nodeObject.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(14f, -52f);
        titleRect.offsetMax = new Vector2(-14f, -12f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = node.nodeName;
        title.fontSize = 22f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.color = titleColor;

        GameObject costObject = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI));
        costObject.transform.SetParent(nodeObject.transform, false);
        RectTransform costRect = costObject.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0f, 0f);
        costRect.anchorMax = new Vector2(1f, 0f);
        costRect.pivot = new Vector2(0.5f, 0f);
        costRect.offsetMin = new Vector2(14f, 10f);
        costRect.offsetMax = new Vector2(-14f, 36f);

        TextMeshProUGUI cost = costObject.GetComponent<TextMeshProUGUI>();
        cost.text = $"Cost: {node.cost}";
        cost.fontSize = 18f;
        cost.alignment = TextAlignmentOptions.BottomLeft;
        cost.color = subtitleColor;

        if (node.icon != null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(nodeObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1f, 1f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.pivot = new Vector2(1f, 1f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            iconRect.anchoredPosition = new Vector2(-12f, -12f);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = node.icon;
            icon.preserveAspect = true;
        }

        return rect;
    }

    private void OnNodeClicked(TechTreeNode nodeData)
    {
        TechNodeSO techNode = TechTreeManager.Instance.GetAllTechNodes()
            .Find(t => t.nodeID == nodeData.id);

        if (techNode != null)
        {
            if (TechTreeManager.Instance.UnlockNode(techNode))
            {
                // Refresh the tree visually
                if (GetComponent<TechTreeController>() != null)
                    GetComponent<TechTreeController>().OnSkillUnlocked();
            }
        }
    }

    void CreateEdgeView(RectTransform fromRect, RectTransform toRect)
    {
        Vector2 start = GetRightEdgePoint(fromRect);
        Vector2 end = GetLeftEdgePoint(toRect);
        Vector2 direction = (end - start).normalized;
        float shaftLength = Mathf.Max(0f, Vector2.Distance(start, end) - 18f);
        Vector2 shaftEnd = start + direction * shaftLength;

        CreateLine(edgeLayer, start, shaftEnd, 5f, edgeColor, "EdgeLine");
        CreateArrowHead(edgeLayer, shaftEnd, direction, 12f, edgeColor);
    }

    Vector2 GetRightEdgePoint(RectTransform rect)
    {
        return rect.anchoredPosition + new Vector2(rect.sizeDelta.x * 0.5f, 0f);
    }

    Vector2 GetLeftEdgePoint(RectTransform rect)
    {
        return rect.anchoredPosition - new Vector2(rect.sizeDelta.x * 0.5f, 0f);
    }

    void CreateLine(RectTransform parent, Vector2 start, Vector2 end, float thickness, Color color, string name)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        Vector2 center = (start + end) * 0.5f;
        Vector2 delta = end - start;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(delta.magnitude, thickness);
        rect.anchoredPosition = new Vector2(center.x, center.y);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.sprite = GetFallbackSprite();
        image.type = Image.Type.Simple;
        image.color = color;
    }

    void CreateArrowHead(RectTransform parent, Vector2 tip, Vector2 direction, float size, Color color)
    {
        Vector2 left = Quaternion.Euler(0f, 0f, 150f) * direction * size;
        Vector2 right = Quaternion.Euler(0f, 0f, -150f) * direction * size;

        CreateLine(parent, tip, tip - left, 4f, color, "ArrowLeft");
        CreateLine(parent, tip, tip - right, 4f, color, "ArrowRight");
    }
}