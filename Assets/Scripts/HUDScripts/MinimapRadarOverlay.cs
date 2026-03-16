using UnityEngine;
using UnityEngine.UI;

public class MinimapRadarOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage minimapView;
    [SerializeField] private Transform trackingTarget;

    [Header("Top-Down Map")]
    [SerializeField] private bool useTopDownTextureMap = true;
    [SerializeField] private bool autoTuneReadabilityForTopDown = true;
    [SerializeField] private Texture2D topDownMapTexture;
    [SerializeField] private bool autoWorldBoundsFromActiveTerrain = true;
    [SerializeField] private Vector2 worldMinXZ = new Vector2(-500f, -500f);
    [SerializeField] private Vector2 worldMaxXZ = new Vector2(500f, 500f);
    [SerializeField, Range(0.05f, 1f)] private float mapZoomNormalized = 0.28f;
    [SerializeField] private bool clampMapToBounds = true;
    [SerializeField] private bool rotateMapWithPlayer = true;
    [SerializeField] private float mapYawOffset = 0f;

    [Header("Radar Style")]
    [SerializeField] private Color radarTintColor = new Color(0.25f, 1f, 0.35f, 0.12f);
    [SerializeField] private Color crosshairColor = new Color(0.35f, 1f, 0.45f, 0.2f);
    [SerializeField] private Color sweepColor = new Color(0.45f, 1f, 0.55f, 0.2f);
    [SerializeField] private Color sweepSoftColor = new Color(0.45f, 1f, 0.55f, 0.08f);
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private Color gridColor = new Color(0.4f, 1f, 0.55f, 0.2f);
    [SerializeField] private Color cardinalColor = new Color(0.86f, 1f, 0.9f, 1f);
    [SerializeField] private Color cardinalOutlineColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField, Range(14, 40)] private int cardinalFontSize = 28;
    [SerializeField, Range(10f, 64f)] private float cardinalVerticalInset = 24f;
    [SerializeField, Range(10f, 64f)] private float cardinalHorizontalInset = 30f;
    [SerializeField, Range(-12f, 12f)] private float cardinalWestNudge = 3f;
    [SerializeField, Range(-12f, 12f)] private float cardinalEastNudge = -1f;
    [SerializeField, Range(24f, 96f)] private float cardinalLabelWidth = 56f;
    [SerializeField, Range(24f, 96f)] private float cardinalLabelHeight = 44f;
    [SerializeField, Range(0.02f, 0.2f)] private float borderThickness = 0.06f;
    [SerializeField] private int circleTextureSize = 512;
    [SerializeField] private bool rotateCardinalsWithPlayer = true;

    [Header("Compass Ring")]
    [SerializeField] private bool showCompassRing = true;
    [SerializeField] private Color compassTickColor = new Color(0.85f, 0.95f, 1f, 0.65f);
    [SerializeField] private Color compassLabelColor = new Color(0.92f, 1f, 0.96f, 0.95f);
    [SerializeField] private Color compassLabelOutlineColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField, Range(2f, 6f)] private float compassTickWidth = 2f;
    [SerializeField, Range(6f, 20f)] private float compassMinorTickLength = 8f;
    [SerializeField, Range(8f, 28f)] private float compassMajorTickLength = 14f;
    [SerializeField, Range(6f, 30f)] private float compassRadiusInset = 12f;
    [SerializeField, Range(4f, 24f)] private float compassLabelInset = 10f;
    [SerializeField, Range(10, 24)] private int compassDegreeFontSize = 14;
    [SerializeField, Range(12, 28)] private int compassDirectionFontSize = 18;

    [Header("Animation")]
    [SerializeField] private float sweepSpeedDegreesPerSecond = 40f;
    [SerializeField, Range(0.05f, 0.5f)] private float sweepFillAmount = 0.17f;
    [SerializeField, Range(0.1f, 0.8f)] private float softSweepFillAmount = 0.26f;

    [Header("UI Performance")]
    [SerializeField] private bool lowPolyUiMode = false;
    [SerializeField] private bool showDiagonalGrid = true;
    [SerializeField] private bool showRadarSweep = true;

    private RectTransform sweepPivot;
    private RectTransform cardinalPivot;
    private RectTransform gridCompassPivot;
    private RectTransform staticGridPivot;
    private RectTransform rotatingLettersPivot;
    private RectTransform minimapViewRect;
    private static Sprite cachedRectSprite;
    private static Sprite cachedCircleSprite;
    private static Sprite cachedRingSprite;

    void Awake()
    {
        borderColor = new Color(0f, 0f, 0f, 0.95f);

        if (minimapView == null)
            minimapView = GetComponentInChildren<RawImage>();

        if (minimapView != null)
            minimapViewRect = minimapView.rectTransform;

        EnsureRadarVisuals();
    }

    void Update()
    {
        if (sweepPivot == null)
            return;

        if (showRadarSweep)
        {
            float zRotation = -sweepSpeedDegreesPerSecond * Time.unscaledDeltaTime;
            sweepPivot.Rotate(0f, 0f, zRotation, Space.Self);
        }

        UpdateTopDownMapView();

        if (rotateCardinalsWithPlayer && trackingTarget != null)
        {
            float compassRotation = -trackingTarget.eulerAngles.y;
            if (rotatingLettersPivot != null)
                rotatingLettersPivot.localRotation = Quaternion.Euler(0f, 0f, compassRotation);
            else if (gridCompassPivot != null)
                gridCompassPivot.localRotation = Quaternion.Euler(0f, 0f, compassRotation);
            else if (cardinalPivot != null)
                cardinalPivot.localRotation = Quaternion.Euler(0f, 0f, compassRotation);
        }
        else
        {
            if (rotatingLettersPivot != null)
                rotatingLettersPivot.localRotation = Quaternion.identity;
            else if (gridCompassPivot != null)
                gridCompassPivot.localRotation = Quaternion.identity;
            else if (cardinalPivot != null)
                cardinalPivot.localRotation = Quaternion.identity;
        }
    }

    public void SetMinimapView(RawImage view)
    {
        if (minimapView == view && sweepPivot != null && cardinalPivot != null)
            return;

        minimapView = view;
        minimapViewRect = minimapView != null ? minimapView.rectTransform : null;
        EnsureRadarVisuals();
    }

    public void SetTrackingTarget(Transform target)
    {
        trackingTarget = target;
    }

    public void SetTopDownMapTexture(Texture2D mapTexture)
    {
        topDownMapTexture = mapTexture;
    }

    public bool TryWorldToMinimapAnchoredPosition(Vector3 worldPosition, Transform referenceTransform, float edgePadding, out Vector2 anchoredPosition, out bool isInRange)
    {
        anchoredPosition = Vector2.zero;
        isInRange = false;

        if (referenceTransform == null)
            return false;

        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
            return false;

        Vector2 minBounds = worldMinXZ;
        Vector2 maxBounds = worldMaxXZ;

        if (autoWorldBoundsFromActiveTerrain && Terrain.activeTerrain != null)
        {
            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            minBounds = new Vector2(terrainPosition.x, terrainPosition.z);
            maxBounds = new Vector2(terrainPosition.x + terrainSize.x, terrainPosition.z + terrainSize.z);
        }

        float xRange = Mathf.Max(0.001f, maxBounds.x - minBounds.x);
        float zRange = Mathf.Max(0.001f, maxBounds.y - minBounds.y);

        float uvSize = Mathf.Clamp(mapZoomNormalized, 0.05f, 1f);
        float visibleWorldWidth = xRange * uvSize;
        float visibleWorldHeight = zRange * uvSize;

        Vector3 worldDelta = worldPosition - referenceTransform.position;
        Vector3 localDelta = worldDelta;

        if (rotateMapWithPlayer)
        {
            Quaternion inversePlayerYaw = Quaternion.Euler(0f, -referenceTransform.eulerAngles.y, 0f);
            localDelta = inversePlayerYaw * worldDelta;
        }

        float normalizedX = localDelta.x / Mathf.Max(0.001f, visibleWorldWidth * 0.5f);
        float normalizedY = localDelta.z / Mathf.Max(0.001f, visibleWorldHeight * 0.5f);

        float radius = Mathf.Min(rootRect.rect.width, rootRect.rect.height) * 0.5f - Mathf.Max(0f, edgePadding);
        if (radius <= 0f)
            return false;

        Vector2 unclamped = new Vector2(normalizedX, normalizedY) * radius;
        isInRange = unclamped.magnitude <= radius;

        if (unclamped.magnitude > radius)
            anchoredPosition = unclamped.normalized * radius;
        else
            anchoredPosition = unclamped;

        return true;
    }

    public void SetRotateCardinalsWithPlayer(bool shouldRotate)
    {
        rotateCardinalsWithPlayer = shouldRotate;

        if (!rotateCardinalsWithPlayer)
        {
            if (rotatingLettersPivot != null)
                rotatingLettersPivot.localRotation = Quaternion.identity;
            if (gridCompassPivot != null)
                gridCompassPivot.localRotation = Quaternion.identity;
            if (cardinalPivot != null)
                cardinalPivot.localRotation = Quaternion.identity;
        }
    }

    public void EnsureRadarVisuals()
    {
        ApplyTopDownReadabilityProfile();

        Sprite circleSprite = GetCircleSprite();
        Sprite rectSprite = GetRectSprite();

        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
            return;

        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.sprite = circleSprite;
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = true;
        }

        Image tint = GetOrCreateImage("RadarTint", rootRect, circleSprite);
        StretchToParent(tint.rectTransform);
        tint.type = Image.Type.Simple;
        tint.preserveAspect = true;
        tint.color = radarTintColor;
        tint.raycastTarget = false;

        RectTransform staticGrid = GetOrCreateRect("RadarGridStaticPivot", rootRect);
        StretchToParent(staticGrid);
        staticGridPivot = staticGrid;

        RectTransform rotatingGrid = GetOrCreateRect("RadarGridCompassPivot", rootRect);
        StretchToParent(rotatingGrid);
        gridCompassPivot = rotatingGrid;
        rotatingLettersPivot = rotatingGrid;

        Image horizontal = GetOrCreateImage("RadarCrossHorizontal", rootRect, rectSprite);
        horizontal.rectTransform.SetParent(staticGrid, false);
        horizontal.rectTransform.anchorMin = new Vector2(0.1f, 0.5f);
        horizontal.rectTransform.anchorMax = new Vector2(0.9f, 0.5f);
        horizontal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        horizontal.rectTransform.sizeDelta = new Vector2(0f, 2f);
        horizontal.rectTransform.anchoredPosition = Vector2.zero;
        horizontal.color = crosshairColor;
        horizontal.raycastTarget = false;

        Image vertical = GetOrCreateImage("RadarCrossVertical", rootRect, rectSprite);
        vertical.rectTransform.SetParent(staticGrid, false);
        vertical.rectTransform.anchorMin = new Vector2(0.5f, 0.1f);
        vertical.rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
        vertical.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        vertical.rectTransform.sizeDelta = new Vector2(2f, 0f);
        vertical.rectTransform.anchoredPosition = Vector2.zero;
        vertical.color = crosshairColor;
        vertical.raycastTarget = false;

        Image diagonalA = GetOrCreateImage("RadarGridDiagonalA", rootRect, rectSprite);
        diagonalA.rectTransform.SetParent(staticGrid, false);
        diagonalA.rectTransform.anchorMin = new Vector2(0.16f, 0.5f);
        diagonalA.rectTransform.anchorMax = new Vector2(0.84f, 0.5f);
        diagonalA.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        diagonalA.rectTransform.sizeDelta = new Vector2(0f, 1.5f);
        diagonalA.rectTransform.anchoredPosition = Vector2.zero;
        diagonalA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        diagonalA.color = gridColor;
        diagonalA.raycastTarget = false;
        diagonalA.gameObject.SetActive(showDiagonalGrid);

        Image diagonalB = GetOrCreateImage("RadarGridDiagonalB", rootRect, rectSprite);
        diagonalB.rectTransform.SetParent(staticGrid, false);
        diagonalB.rectTransform.anchorMin = new Vector2(0.16f, 0.5f);
        diagonalB.rectTransform.anchorMax = new Vector2(0.84f, 0.5f);
        diagonalB.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        diagonalB.rectTransform.sizeDelta = new Vector2(0f, 1.5f);
        diagonalB.rectTransform.anchoredPosition = Vector2.zero;
        diagonalB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        diagonalB.color = gridColor;
        diagonalB.raycastTarget = false;
        diagonalB.gameObject.SetActive(showDiagonalGrid);

        Image ringOuter = GetOrCreateImage("RadarGridRingOuter", rootRect, GetRingSprite());
        ringOuter.rectTransform.SetParent(staticGrid, false);
        StretchToParent(ringOuter.rectTransform);
        ringOuter.rectTransform.offsetMin = new Vector2(34f, 34f);
        ringOuter.rectTransform.offsetMax = new Vector2(-34f, -34f);
        ringOuter.type = Image.Type.Simple;
        ringOuter.preserveAspect = true;
        ringOuter.color = gridColor;
        ringOuter.raycastTarget = false;

        Image ringInner = GetOrCreateImage("RadarGridRingInner", rootRect, GetRingSprite());
        ringInner.rectTransform.SetParent(staticGrid, false);
        StretchToParent(ringInner.rectTransform);
        ringInner.rectTransform.offsetMin = new Vector2(68f, 68f);
        ringInner.rectTransform.offsetMax = new Vector2(-68f, -68f);
        ringInner.type = Image.Type.Simple;
        ringInner.preserveAspect = true;
        ringInner.color = gridColor;
        ringInner.raycastTarget = false;

        RectTransform pivot = GetOrCreateRect("RadarSweepPivot", rootRect);
        StretchToParent(pivot);
        sweepPivot = pivot;

        Image softSweep = GetOrCreateImage("RadarSweepSoft", pivot, circleSprite);
        StretchToParent(softSweep.rectTransform);
        softSweep.type = Image.Type.Filled;
        softSweep.fillMethod = Image.FillMethod.Radial360;
        softSweep.fillOrigin = 2;
        softSweep.fillClockwise = false;
        softSweep.fillAmount = softSweepFillAmount;
        softSweep.color = sweepSoftColor;
        softSweep.raycastTarget = false;
        softSweep.gameObject.SetActive(showRadarSweep);

        Image sweep = GetOrCreateImage("RadarSweep", pivot, circleSprite);
        StretchToParent(sweep.rectTransform);
        sweep.type = Image.Type.Filled;
        sweep.fillMethod = Image.FillMethod.Radial360;
        sweep.fillOrigin = 2;
        sweep.fillClockwise = false;
        sweep.fillAmount = sweepFillAmount;
        sweep.color = sweepColor;
        sweep.raycastTarget = false;
        sweep.gameObject.SetActive(showRadarSweep);

        Image border = GetOrCreateImage("RadarBorder", rootRect, GetRingSprite());
        StretchToParent(border.rectTransform);
        border.rectTransform.offsetMin = new Vector2(-1f, -1f);
        border.rectTransform.offsetMax = new Vector2(1f, 1f);
        border.type = Image.Type.Simple;
        border.preserveAspect = true;
        border.color = borderColor;
        border.raycastTarget = false;

        RectTransform compassPivot = GetOrCreateRect("RadarCompassPivot", rotatingGrid);
        StretchToParent(compassPivot);
        cardinalPivot = compassPivot;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text north = GetOrCreateText("RadarNorth", compassPivot, "N", uiFont);
        PlaceCardinal(north.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -cardinalVerticalInset), cardinalLabelWidth, cardinalLabelHeight);
        north.color = cardinalColor;
        north.fontSize = cardinalFontSize;
        north.fontStyle = FontStyle.Bold;
        EnsureCardinalOutline(north, cardinalOutlineColor);

        Text south = GetOrCreateText("RadarSouth", compassPivot, "S", uiFont);
        PlaceCardinal(south.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, cardinalVerticalInset), cardinalLabelWidth, cardinalLabelHeight);
        south.color = cardinalColor;
        south.fontSize = cardinalFontSize;
        south.fontStyle = FontStyle.Bold;
        EnsureCardinalOutline(south, cardinalOutlineColor);

        Text west = GetOrCreateText("RadarWest", compassPivot, "W", uiFont);
        PlaceCardinal(west.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(cardinalHorizontalInset + cardinalWestNudge, 0f), cardinalLabelWidth, cardinalLabelHeight);
        west.color = cardinalColor;
        west.fontSize = cardinalFontSize;
        west.fontStyle = FontStyle.Bold;
        EnsureCardinalOutline(west, cardinalOutlineColor);

        Text east = GetOrCreateText("RadarEast", compassPivot, "E", uiFont);
        PlaceCardinal(east.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-cardinalHorizontalInset + cardinalEastNudge, 0f), cardinalLabelWidth, cardinalLabelHeight);
        east.color = cardinalColor;
        east.fontSize = cardinalFontSize;
        east.fontStyle = FontStyle.Bold;
        EnsureCardinalOutline(east, cardinalOutlineColor);

        RectTransform compassRing = GetOrCreateRect("RadarCompassRing", rotatingGrid);
        StretchToParent(compassRing);
        compassRing.gameObject.SetActive(showCompassRing);
        if (showCompassRing)
            EnsureCompassRing(compassRing, rectSprite, uiFont, rootRect);

        Outline outline = border.GetComponent<Outline>();
        if (outline != null)
            Destroy(outline);

        if (minimapView != null)
            minimapView.transform.SetAsFirstSibling();

        Transform playerMarker = transform.Find("PlayerMarker");
        if (playerMarker != null)
            playerMarker.SetAsLastSibling();
        else
            east.transform.SetAsLastSibling();
    }

    static Sprite GetRectSprite()
    {
        if (cachedRectSprite != null)
            return cachedRectSprite;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[4];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        cachedRectSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return cachedRectSprite;
    }

    Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
            return cachedCircleSprite;

        int size = Mathf.Clamp(circleTextureSize, 256, 1024);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1.5f;
        float aaWidth = 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) / aaWidth + 0.5f);
                byte alphaByte = (byte)Mathf.RoundToInt(alpha * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alphaByte);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        cachedCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    Sprite GetRingSprite()
    {
        if (cachedRingSprite != null)
            return cachedRingSprite;

        int size = Mathf.Clamp(circleTextureSize, 256, 1024);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = size * 0.5f - 1.5f;
        float innerRadius = Mathf.Max(1f, outerRadius * (1f - borderThickness));
        float aaWidth = 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float outerAlpha = Mathf.Clamp01((outerRadius - dist) / aaWidth + 0.5f);
                float innerAlpha = Mathf.Clamp01((innerRadius - dist) / aaWidth + 0.5f);
                float ringAlpha = Mathf.Clamp01(outerAlpha - innerAlpha);
                byte alphaByte = (byte)Mathf.RoundToInt(ringAlpha * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alphaByte);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        cachedRingSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedRingSprite;
    }

    static Image GetOrCreateImage(string name, RectTransform parent, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        Image image = null;
        if (existing != null)
            image = existing.GetComponent<Image>();

        if (image == null)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            image = imageObject.GetComponent<Image>();
        }

        image.sprite = sprite;
        return image;
    }

    static Text GetOrCreateText(string name, RectTransform parent, string value, Font font)
    {
        Transform existing = parent.Find(name);
        Text text = null;
        if (existing != null)
            text = existing.GetComponent<Text>();

        if (text == null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<Text>();
        }

        text.text = value;
        text.font = font;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.resizeTextForBestFit = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void PlaceCardinal(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, float labelWidth, float labelHeight)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(labelWidth, labelHeight);
        rect.anchoredPosition = anchoredPosition;
    }

    static RectTransform GetOrCreateRect(string name, RectTransform parent)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = null;
        if (existing != null)
            rect = existing as RectTransform;

        if (rect == null)
        {
            GameObject pivotObject = new GameObject(name, typeof(RectTransform));
            pivotObject.transform.SetParent(parent, false);
            rect = pivotObject.GetComponent<RectTransform>();
        }

        return rect;
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void EnsureCardinalOutline(Text text, Color outlineColor)
    {
        if (text == null)
            return;

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
            outline = text.gameObject.AddComponent<Outline>();

        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    void UpdateTopDownMapView()
    {
        if (!useTopDownTextureMap || minimapView == null || trackingTarget == null)
            return;

        if (topDownMapTexture == null)
            return;

        if (minimapView.texture != topDownMapTexture)
            minimapView.texture = topDownMapTexture;

        Vector2 minBounds = worldMinXZ;
        Vector2 maxBounds = worldMaxXZ;

        if (autoWorldBoundsFromActiveTerrain && Terrain.activeTerrain != null)
        {
            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            minBounds = new Vector2(terrainPosition.x, terrainPosition.z);
            maxBounds = new Vector2(terrainPosition.x + terrainSize.x, terrainPosition.z + terrainSize.z);
        }

        float xRange = Mathf.Max(0.001f, maxBounds.x - minBounds.x);
        float zRange = Mathf.Max(0.001f, maxBounds.y - minBounds.y);

        float normalizedX = Mathf.Clamp01((trackingTarget.position.x - minBounds.x) / xRange);
        float normalizedY = Mathf.Clamp01((trackingTarget.position.z - minBounds.y) / zRange);

        float uvSize = Mathf.Clamp(mapZoomNormalized, 0.05f, 1f);
        Rect uvRect = minimapView.uvRect;
        uvRect.width = uvSize;
        uvRect.height = uvSize;
        uvRect.x = normalizedX - uvSize * 0.5f;
        uvRect.y = normalizedY - uvSize * 0.5f;

        if (clampMapToBounds)
        {
            uvRect.x = Mathf.Clamp(uvRect.x, 0f, 1f - uvRect.width);
            uvRect.y = Mathf.Clamp(uvRect.y, 0f, 1f - uvRect.height);
        }

        minimapView.uvRect = uvRect;

        if (minimapViewRect == null)
            minimapViewRect = minimapView.rectTransform;

        if (minimapViewRect != null)
        {
            if (rotateMapWithPlayer)
                minimapViewRect.localRotation = Quaternion.Euler(0f, 0f, -trackingTarget.eulerAngles.y + mapYawOffset);
            else
                minimapViewRect.localRotation = Quaternion.identity;
        }
    }

    void ApplyTopDownReadabilityProfile()
    {
        if (!useTopDownTextureMap || !autoTuneReadabilityForTopDown)
            return;

        if (lowPolyUiMode)
        {
            showDiagonalGrid = false;
            showRadarSweep = false;
            showCompassRing = false;
            sweepFillAmount = 0.12f;
            softSweepFillAmount = 0.18f;
        }

        radarTintColor = new Color(1f, 1f, 1f, 0.035f);
        crosshairColor = new Color(0.72f, 0.88f, 1f, 0.2f);
        gridColor = new Color(0.72f, 0.88f, 1f, 0.16f);
        sweepColor = new Color(0.65f, 0.9f, 1f, 0.08f);
        sweepSoftColor = new Color(0.65f, 0.9f, 1f, 0.03f);
        cardinalColor = new Color(0.95f, 0.98f, 1f, 1f);
        compassTickColor = new Color(0.86f, 0.93f, 1f, 0.6f);
        compassLabelColor = new Color(0.95f, 0.98f, 1f, 0.95f);
    }

    void EnsureCompassRing(RectTransform compassRing, Sprite rectSprite, Font uiFont, RectTransform rootRect)
    {
        float referenceSize = Mathf.Min(rootRect.rect.width, rootRect.rect.height);
        if (referenceSize <= 0f)
            referenceSize = circleTextureSize;

        float radius = Mathf.Max(40f, referenceSize * 0.5f - compassRadiusInset);

        for (int degrees = 0; degrees < 360; degrees += 15)
        {
            bool majorTick = degrees % 30 == 0;
            float tickLength = majorTick ? compassMajorTickLength : compassMinorTickLength;

            Image tick = GetOrCreateImage($"CompassTick_{degrees}", compassRing, rectSprite);
            tick.raycastTarget = false;
            tick.color = compassTickColor;

            RectTransform tickRect = tick.rectTransform;
            tickRect.anchorMin = new Vector2(0.5f, 0.5f);
            tickRect.anchorMax = new Vector2(0.5f, 0.5f);
            tickRect.pivot = new Vector2(0.5f, 0f);
            tickRect.sizeDelta = new Vector2(compassTickWidth, tickLength);

            Vector2 direction = DegreesToCompassDirection(degrees);
            tickRect.anchoredPosition = direction * (radius - tickLength);
            tickRect.localRotation = Quaternion.Euler(0f, 0f, -degrees);

            bool isCardinal = degrees % 90 == 0;
            bool isIntercardinal = degrees % 90 == 45;

            if (isCardinal)
                continue;

            string label = isIntercardinal ? GetIntercardinalLabel(degrees) : degrees.ToString();
            Text labelText = GetOrCreateText($"CompassLabel_{degrees}", compassRing, label, uiFont);
            labelText.raycastTarget = false;
            labelText.color = compassLabelColor;
            labelText.fontStyle = FontStyle.Bold;
            labelText.fontSize = isIntercardinal ? compassDirectionFontSize : compassDegreeFontSize;
            EnsureCardinalOutline(labelText, compassLabelOutlineColor);

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = isIntercardinal ? new Vector2(52f, 24f) : new Vector2(40f, 22f);
            labelRect.anchoredPosition = direction * (radius - tickLength - compassLabelInset);
            labelRect.localRotation = Quaternion.identity;
            labelText.alignment = TextAnchor.MiddleCenter;
        }
    }

    static Vector2 DegreesToCompassDirection(int degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    static string GetIntercardinalLabel(int degrees)
    {
        return degrees switch
        {
            45 => "NE",
            135 => "SE",
            225 => "SW",
            315 => "NW",
            _ => string.Empty
        };
    }
}