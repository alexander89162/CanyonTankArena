using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseController playerController;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private RectTransform ammoContainer;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private MinimapRadarOverlay minimapRadarOverlay;
    [SerializeField] private bool use2DMinimap = true;
    [SerializeField] private Color minimap2DBackgroundColor = new Color(0.18f, 0.28f, 0.2f, 1f);

    private bool minimapMarkerCleaned;
    private MinimapPlayerMarker minimapPlayerMarker;
    private readonly List<MinimapEnemyMarker> minimapEnemyMarkers = new List<MinimapEnemyMarker>();

    [Header("Layout")]
    [SerializeField] private bool bulletCountOnRight = true;
    [SerializeField] private Vector2 ammoScreenMargin = new Vector2(32f, 28f);

    [Header("Health Bar Style")]
    [SerializeField] private bool useHealthColorGradient = true;
    [SerializeField] private bool lowPolyHealthUiMode = false;
    [SerializeField] private Color healthHighColor = new Color(0.2f, 0.92f, 0.22f, 1f);
    [SerializeField] private Color healthMidColor = new Color(0.94f, 0.67f, 0.15f, 1f);
    [SerializeField] private Color healthLowColor = new Color(0.9f, 0.17f, 0.14f, 1f);
    [SerializeField, Range(0.2f, 0.8f)] private float healthMidThreshold = 0.55f;
    [SerializeField] private float healthBarHeight = 30f;
    [SerializeField] private float healthTextOffsetBelowBar = 8f;
    [SerializeField] private Color healthTextDisplayColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color healthFrameColor = new Color(0.06f, 0.07f, 0.06f, 0.98f);
    [SerializeField] private float healthFrameThickness = 3f;

    [Header("Minimap Follow")]
    [SerializeField] private Vector3 minimapFollowOffset = new Vector3(0f, 35f, 0f);
    [SerializeField] private bool minimapRotateWithPlayer = true;
    [SerializeField] private Vector2 minimapSize = new Vector2(500f, 500f);

    [Header("Performance")]
    [SerializeField, Range(0.03f, 0.5f)] private float uiRefreshInterval = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float targetResolveInterval = 0.25f;

    private float nextUiRefreshTime;
    private float nextTargetResolveTime;
    private float nextEnemyResolveTime;
    private Transform cachedMinimapTarget;
    private readonly List<Transform> cachedEnemyTargets = new List<Transform>();

    void Awake()
    {
        ResolveUIReferences();
        TryResolvePlayerController();
        EnsureHealthBarPresentation();

        ApplyAmmoAnchor();
        EnsureMinimapRadarOverlay();
        RefreshUI();

        nextUiRefreshTime = Time.unscaledTime + uiRefreshInterval;
    }

    void Update()
    {
        if (Time.unscaledTime < nextUiRefreshTime)
            return;

        RefreshUI();
        nextUiRefreshTime = Time.unscaledTime + uiRefreshInterval;
    }

    void LateUpdate()
    {
        ForceMinimapCameraPose();
    }

    public void RefreshUI()
    {
        ResolveUIReferences();
        EnsureHealthBarPresentation();

        if (playerController == null)
            TryResolvePlayerController();

        if (playerController != null)
            UpdateHealthAndAmmo(playerController);
        else
            SetMissingPlayerState();

        UpdateMinimapState();
    }

    public void SetBulletCountSide(bool showOnRight)
    {
        bulletCountOnRight = showOnRight;
        ApplyAmmoAnchor();
    }

    void UpdateHealthAndAmmo(BaseController controller)
    {
        float maxHealth = Mathf.Max(1f, controller.MaxHealth);
        float currentHealth = Mathf.Clamp(controller.CurrentHealth, 0f, maxHealth);
        float health01 = currentHealth / maxHealth;

        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        ApplyHealthFillColor(health01);

        if (healthText != null)
            healthText.text = $"HP {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";

        if (ammoText != null)
            ammoText.text = $"Ammo {controller.CurrentAmmo}/{controller.MaxAmmo}";
    }

    void SetMissingPlayerState()
    {
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = 1f;
            healthBar.value = 0f;
        }

        ApplyHealthFillColor(0f);

        if (healthText != null)
            healthText.text = "HP --/--";

        if (ammoText != null)
            ammoText.text = "Ammo --/--";
    }

    void UpdateMinimapState()
    {
        bool minimapReady = use2DMinimap ? minimapImage != null : minimapCamera != null;

        if (minimapImage != null)
        {
            minimapImage.enabled = minimapReady;

            if (use2DMinimap)
            {
                minimapImage.color = minimapImage.texture == null ? minimap2DBackgroundColor : Color.white;

                if (minimapCamera != null && minimapCamera.enabled)
                    minimapCamera.enabled = false;
            }
            else
            {
                minimapImage.color = Color.white;

                if (minimapCamera != null && !minimapCamera.enabled)
                    minimapCamera.enabled = true;
            }
        }

        if (minimapReady)
        {
            if (!use2DMinimap)
                EnsureMinimapCameraBehavior();

            EnsureMinimapRadarOverlay();

            if (!minimapMarkerCleaned)
            {
                CleanupDuplicateMinimapMarkers();
                minimapMarkerCleaned = true;
            }

            EnsureMinimapPlayerMarkerTracking();
            EnsureMinimapEnemyMarkerTracking();
        }
    }

    void EnsureMinimapRadarOverlay()
    {
        if (minimapImage == null)
            return;

        RectTransform minimapRoot = minimapImage.transform.parent as RectTransform;
        if (minimapRoot == null)
            return;

        if (minimapSize.x <= 0f || minimapSize.y <= 0f)
            minimapSize = new Vector2(500f, 500f);

        minimapRoot.sizeDelta = minimapSize;

        if (minimapRadarOverlay == null)
            minimapRadarOverlay = minimapRoot.GetComponent<MinimapRadarOverlay>();

        if (minimapRadarOverlay == null)
            minimapRadarOverlay = minimapRoot.gameObject.AddComponent<MinimapRadarOverlay>();

        minimapRadarOverlay.SetMinimapView(minimapImage);

        minimapRadarOverlay.SetRotateCardinalsWithPlayer(true);

        Transform minimapTarget = ResolveMinimapTargetTransform(false);
        if (minimapTarget != null)
            minimapRadarOverlay.SetTrackingTarget(minimapTarget);
    }

    void EnsureMinimapCameraBehavior()
    {
        if (minimapCamera == null)
            return;

        MinimapCameraFollow minimapFollow = minimapCamera.GetComponent<MinimapCameraFollow>();
        if (minimapFollow == null)
            minimapFollow = minimapCamera.gameObject.AddComponent<MinimapCameraFollow>();

        minimapFollow.SetRotateWithTarget(true);

        Transform minimapTarget = ResolveMinimapTargetTransform(false);
        if (minimapTarget != null)
            minimapFollow.SetTarget(minimapTarget);
    }

    void ForceMinimapCameraPose()
    {
        if (use2DMinimap)
            return;

        if (minimapCamera == null)
            return;

        Transform followTarget = ResolveMinimapTargetTransform(false);
        if (followTarget == null)
            return;

        minimapCamera.transform.position = followTarget.position + minimapFollowOffset;

        if (minimapRotateWithPlayer)
            minimapCamera.transform.rotation = Quaternion.Euler(90f, followTarget.eulerAngles.y, 0f);
        else
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    Transform ResolveMinimapTargetTransform(bool forceRefresh)
    {
        if (!forceRefresh)
        {
            if (cachedMinimapTarget != null && cachedMinimapTarget.gameObject.activeInHierarchy)
                return cachedMinimapTarget;

            if (Time.unscaledTime < nextTargetResolveTime)
                return null;
        }

        nextTargetResolveTime = Time.unscaledTime + targetResolveInterval;

        GameObject preferredTank = GameObject.Find("minitank-v10-green 1");
        if (preferredTank == null)
            preferredTank = GameObject.Find("minitank-v10-green");

        if (preferredTank != null)
        {
            cachedMinimapTarget = preferredTank.transform;
            return cachedMinimapTarget;
        }

        GameObject taggedPlayer = TryFindTaggedPlayer();
        if (taggedPlayer != null)
        {
            cachedMinimapTarget = taggedPlayer.transform;
            return cachedMinimapTarget;
        }

        TankController tankController = FindFirstObjectByType<TankController>();
        if (tankController != null)
        {
            cachedMinimapTarget = tankController.transform;
            return cachedMinimapTarget;
        }

        if (playerController != null)
        {
            TankController nestedTankController = playerController.GetComponent<TankController>();
            if (nestedTankController == null)
                nestedTankController = playerController.GetComponentInChildren<TankController>();

            if (nestedTankController != null)
            {
                cachedMinimapTarget = nestedTankController.transform;
                return cachedMinimapTarget;
            }

            cachedMinimapTarget = playerController.transform;
            return cachedMinimapTarget;
        }

        cachedMinimapTarget = null;
        return null;
    }

    void CleanupDuplicateMinimapMarkers()
    {
        if (minimapImage == null)
            return;

        RectTransform minimapRoot = minimapImage.transform.parent as RectTransform;
        if (minimapRoot == null)
            return;

        TMP_Text[] markerTexts = minimapRoot.GetComponentsInChildren<TMP_Text>(true);
        MinimapPlayerMarker keptMarker = null;

        for (int i = 0; i < markerTexts.Length; i++)
        {
            TMP_Text markerText = markerTexts[i];
            if (markerText == null)
                continue;

            bool isMarkerName = markerText.name.ToLowerInvariant().Contains("playermarker");
            bool isMarkerGlyph = markerText.text != null && markerText.text.Contains("▲");
            if (!isMarkerName && !isMarkerGlyph)
                continue;

            MinimapPlayerMarker marker = markerText.GetComponent<MinimapPlayerMarker>();

            if (keptMarker == null)
            {
                if (marker == null)
                    marker = markerText.gameObject.AddComponent<MinimapPlayerMarker>();

                keptMarker = marker;
                continue;
            }

            Destroy(markerText.gameObject);
        }

        if (keptMarker == null)
            return;

        Transform markerTarget = null;
        if (playerController != null)
            markerTarget = playerController.transform;
        else
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                markerTarget = taggedPlayer.transform;
        }

        if (markerTarget != null)
            keptMarker.SetTarget(markerTarget);

        minimapPlayerMarker = keptMarker;
        ApplyPlayerMarkerStyle(minimapPlayerMarker);
        keptMarker.transform.SetAsLastSibling();
    }

    void EnsureMinimapPlayerMarkerTracking()
    {
        if (minimapImage == null)
            return;

        RectTransform minimapRoot = minimapImage.transform.parent as RectTransform;
        if (minimapRoot == null)
            return;

        if (minimapPlayerMarker == null)
        {
            MinimapPlayerMarker[] markers = minimapRoot.GetComponentsInChildren<MinimapPlayerMarker>(true);
            if (markers.Length > 0)
                minimapPlayerMarker = markers[0];
        }

        if (minimapPlayerMarker == null)
            return;

        Transform minimapTarget = ResolveMinimapTargetTransform(false);
        if (minimapTarget != null)
            minimapPlayerMarker.SetTarget(minimapTarget);

        ApplyPlayerMarkerStyle(minimapPlayerMarker);

        minimapPlayerMarker.transform.SetAsLastSibling();
    }

    void ApplyPlayerMarkerStyle(MinimapPlayerMarker marker)
    {
        if (marker == null)
            return;

        TMP_Text markerText = marker.GetComponent<TMP_Text>();
        if (markerText == null)
            return;

        markerText.text = "▲";
        markerText.fontSize = 44;
        markerText.color = new Color(0.22f, 1f, 0.3f, 1f);

        Outline outline = markerText.GetComponent<Outline>();
        if (outline == null)
            outline = markerText.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
    }

    void EnsureMinimapEnemyMarkerTracking()
    {
        if (minimapImage == null)
            return;

        RectTransform minimapRoot = minimapImage.transform.parent as RectTransform;
        if (minimapRoot == null)
            return;

        Transform playerTarget = ResolveMinimapTargetTransform(false);
        if (playerTarget == null)
            return;

        List<Transform> enemyTargets = ResolveEnemyTargetTransforms(playerTarget);

        if (enemyTargets.Count == 0)
        {
            for (int i = 0; i < minimapEnemyMarkers.Count; i++)
            {
                MinimapEnemyMarker marker = minimapEnemyMarkers[i];
                if (marker != null)
                    marker.gameObject.SetActive(false);
            }
            return;
        }

        EnsureEnemyMarkerPool(minimapRoot, enemyTargets.Count);

        for (int i = 0; i < minimapEnemyMarkers.Count; i++)
        {
            MinimapEnemyMarker marker = minimapEnemyMarkers[i];
            if (marker == null)
                continue;

            if (i < enemyTargets.Count)
            {
                marker.SetTarget(enemyTargets[i]);
                marker.SetPlayerReference(playerTarget);
                marker.SetRadarOverlay(minimapRadarOverlay);
                marker.gameObject.SetActive(true);
                ApplyEnemyMarkerStyle(marker);
                marker.transform.SetAsLastSibling();
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }

        if (minimapPlayerMarker != null)
            minimapPlayerMarker.transform.SetAsLastSibling();
    }

    void EnsureEnemyMarkerPool(RectTransform minimapRoot, int requiredCount)
    {
        for (int i = minimapEnemyMarkers.Count - 1; i >= 0; i--)
        {
            if (minimapEnemyMarkers[i] == null)
                minimapEnemyMarkers.RemoveAt(i);
        }

        MinimapEnemyMarker[] existingMarkers = minimapRoot.GetComponentsInChildren<MinimapEnemyMarker>(true);
        for (int i = 0; i < existingMarkers.Length; i++)
        {
            MinimapEnemyMarker existingMarker = existingMarkers[i];
            if (existingMarker != null && !minimapEnemyMarkers.Contains(existingMarker))
                minimapEnemyMarkers.Add(existingMarker);
        }

        while (minimapEnemyMarkers.Count < requiredCount)
        {
            MinimapEnemyMarker marker = CreateEnemyMarker(minimapRoot, minimapEnemyMarkers.Count + 1);
            if (marker == null)
                break;

            minimapEnemyMarkers.Add(marker);
        }

        minimapEnemyMarkers.Sort((left, right) =>
        {
            if (left == null && right == null)
                return 0;
            if (left == null)
                return 1;
            if (right == null)
                return -1;
            return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
        });
    }

    List<Transform> ResolveEnemyTargetTransforms(Transform playerTarget)
    {
        if (Time.unscaledTime < nextEnemyResolveTime)
            return cachedEnemyTargets;

        nextEnemyResolveTime = Time.unscaledTime + 0.75f;
        cachedEnemyTargets.Clear();

        HashSet<int> seenIds = new HashSet<int>();

        TryFindTaggedEnemies(cachedEnemyTargets, seenIds, "Enemy", playerTarget);

        TankController[] tanks = FindObjectsByType<TankController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < tanks.Length; i++)
        {
            TankController candidate = tanks[i];
            if (candidate == null)
                continue;

            Transform candidateTransform = candidate.transform;
            if (candidateTransform == null || candidateTransform == playerTarget)
                continue;

            GameObject candidateObject = candidateTransform.gameObject;
            if (HasTagSafe(candidateObject, "Player") || HasTagSafe(candidateObject, "player"))
                continue;

            int instanceId = candidateObject.GetInstanceID();
            if (!seenIds.Add(instanceId))
                continue;

            cachedEnemyTargets.Add(candidateTransform);
        }

        return cachedEnemyTargets;
    }

    void TryFindTaggedEnemies(List<Transform> targets, HashSet<int> seenIds, string tag, Transform playerTarget)
    {
        GameObject[] taggedEnemies = Array.Empty<GameObject>();

        try
        {
            taggedEnemies = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
        }

        for (int i = 0; i < taggedEnemies.Length; i++)
        {
            GameObject taggedEnemy = taggedEnemies[i];
            if (taggedEnemy == null)
                continue;

            Transform enemyTransform = taggedEnemy.transform;
            if (enemyTransform == null || enemyTransform == playerTarget)
                continue;

            int instanceId = taggedEnemy.GetInstanceID();
            if (!seenIds.Add(instanceId))
                continue;

            targets.Add(enemyTransform);
        }
    }

    static bool HasTagSafe(GameObject gameObject, string tag)
    {
        if (gameObject == null)
            return false;

        try
        {
            return gameObject.CompareTag(tag);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    MinimapEnemyMarker CreateEnemyMarker(RectTransform minimapRoot, int markerIndex)
    {
        GameObject markerObject = new GameObject($"EnemyMarker_{markerIndex}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(MinimapEnemyMarker));
        markerObject.transform.SetParent(minimapRoot, false);

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(34f, 34f);
        markerRect.anchoredPosition = Vector2.zero;

        return markerObject.GetComponent<MinimapEnemyMarker>();
    }

    void ApplyEnemyMarkerStyle(MinimapEnemyMarker marker)
    {
        if (marker == null)
            return;

        TMP_Text markerText = marker.GetComponent<TMP_Text>();
        if (markerText == null)
            return;

        markerText.text = "▲";
        markerText.fontSize = 36;
        markerText.color = new Color(1f, 0.23f, 0.2f, 1f);
        markerText.alignment = TextAlignmentOptions.Center;

        Outline outline = markerText.GetComponent<Outline>();
        if (outline == null)
            outline = markerText.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
    }

    void ApplyAmmoAnchor()
    {
        if (ammoContainer == null)
            return;

        if (bulletCountOnRight)
        {
            ammoContainer.anchorMin = new Vector2(1f, 0f);
            ammoContainer.anchorMax = new Vector2(1f, 0f);
            ammoContainer.pivot = new Vector2(1f, 0f);
            ammoContainer.anchoredPosition = new Vector2(-ammoScreenMargin.x, ammoScreenMargin.y);
        }
        else
        {
            ammoContainer.anchorMin = new Vector2(0f, 0f);
            ammoContainer.anchorMax = new Vector2(0f, 0f);
            ammoContainer.pivot = new Vector2(0f, 0f);
            ammoContainer.anchoredPosition = ammoScreenMargin;
        }
    }

    void ResolveUIReferences()
    {
        if (healthBar == null)
        {
            Transform healthRoot = transform.Find("HealthTopRight");
            if (healthRoot != null)
                healthBar = healthRoot.GetComponentInChildren<Slider>(true);
        }

        if (healthFillImage == null && healthBar != null && healthBar.fillRect != null)
            healthFillImage = healthBar.fillRect.GetComponent<Image>();

        if (healthText == null)
            healthText = FindTextByName("HealthText");

        if (ammoText == null)
            ammoText = FindTextByName("AmmoText");

        if (ammoContainer == null)
        {
            Transform ammoRoot = transform.Find("AmmoBottom");
            if (ammoRoot != null)
                ammoContainer = ammoRoot as RectTransform;
        }

        if (minimapImage == null)
        {
            Transform minimapRoot = transform.Find("MiniMapTopLeft");
            if (minimapRoot != null)
                minimapImage = minimapRoot.GetComponentInChildren<RawImage>(true);
        }

        if (minimapCamera == null)
        {
            GameObject minimapCameraObject = GameObject.Find("MinimapCamera");
            if (minimapCameraObject != null)
                minimapCamera = minimapCameraObject.GetComponent<Camera>();
        }
    }

    TMP_Text FindTextByName(string objectName)
    {
        Transform target = transform.Find(objectName);
        if (target != null)
            return target.GetComponent<TMP_Text>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.name == objectName)
                return text;
        }

        return null;
    }

    void TryResolvePlayerController()
    {
        GameObject taggedPlayer = TryFindTaggedPlayer();
        if (taggedPlayer != null)
            playerController = taggedPlayer.GetComponent<BaseController>();

        if (playerController == null)
            playerController = FindFirstObjectByType<BaseController>();

        cachedMinimapTarget = ResolveMinimapTargetTransform(true);
    }

    static GameObject TryFindTaggedPlayer()
    {
        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
        }

        if (taggedPlayer == null)
        {
            try
            {
                taggedPlayer = GameObject.FindGameObjectWithTag("player");
            }
            catch (UnityException)
            {
            }
        }

        return taggedPlayer;
    }

    void ApplyHealthFillColor(float health01)
    {
        if (!useHealthColorGradient)
            return;

        if (healthFillImage == null && healthBar != null && healthBar.fillRect != null)
            healthFillImage = healthBar.fillRect.GetComponent<Image>();

        if (healthFillImage == null)
            return;

        float clamped = Mathf.Clamp01(health01);
        float split = Mathf.Clamp(healthMidThreshold, 0.01f, 0.99f);

        Color fillColor;
        if (clamped >= split)
        {
            float t = Mathf.InverseLerp(split, 1f, clamped);
            fillColor = Color.Lerp(healthMidColor, healthHighColor, t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, split, clamped);
            fillColor = Color.Lerp(healthLowColor, healthMidColor, t);
        }

        healthFillImage.color = fillColor;
    }

    void EnsureHealthBarPresentation()
    {
        if (healthBar != null)
        {
            RectTransform healthBarRect = healthBar.GetComponent<RectTransform>();
            if (healthBarRect != null)
            {
                healthBarRect.anchorMin = new Vector2(0f, 1f);
                healthBarRect.anchorMax = new Vector2(1f, 1f);
                healthBarRect.pivot = new Vector2(0.5f, 1f);
                healthBarRect.sizeDelta = new Vector2(0f, healthBarHeight);
                healthBarRect.anchoredPosition = Vector2.zero;
            }

            Transform fillArea = healthBar.transform.Find("Fill Area");
            if (fillArea != null)
            {
                RectTransform fillAreaRect = fillArea as RectTransform;
                if (fillAreaRect != null)
                {
                    fillAreaRect.anchorMin = Vector2.zero;
                    fillAreaRect.anchorMax = Vector2.one;
                    float inset = lowPolyHealthUiMode ? Mathf.Max(1f, healthFrameThickness) : 0f;
                    fillAreaRect.offsetMin = new Vector2(inset, inset);
                    fillAreaRect.offsetMax = new Vector2(-inset, -inset);
                }

                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    RectTransform fillRect = fill as RectTransform;
                    if (fillRect != null)
                    {
                        fillRect.anchorMin = Vector2.zero;
                        fillRect.anchorMax = Vector2.one;
                        fillRect.offsetMin = Vector2.zero;
                        fillRect.offsetMax = Vector2.zero;
                    }
                }
            }

            Transform background = healthBar.transform.Find("Background");
            if (background != null)
            {
                Image backgroundImage = background.GetComponent<Image>();
                if (backgroundImage != null)
                    backgroundImage.color = lowPolyHealthUiMode ? healthFrameColor : new Color(0.04f, 0.05f, 0.04f, 0.95f);
            }

            EnsureHealthFrameImage(healthBar);
        }

        if (healthText != null && healthBar != null)
        {
            RectTransform healthTextRect = healthText.GetComponent<RectTransform>();
            RectTransform healthBarRect = healthBar.GetComponent<RectTransform>();
            if (healthTextRect != null && healthBarRect != null)
            {
                healthTextRect.anchorMin = new Vector2(0f, 1f);
                healthTextRect.anchorMax = new Vector2(1f, 1f);
                healthTextRect.pivot = new Vector2(0.5f, 1f);
                healthTextRect.sizeDelta = new Vector2(0f, 26f);
                healthTextRect.anchoredPosition = new Vector2(0f, -(healthBarHeight + healthTextOffsetBelowBar));
            }

            healthText.alignment = TextAlignmentOptions.Center;
            healthText.color = healthTextDisplayColor;
            healthText.fontSize = Mathf.Max(22f, healthText.fontSize);
        }
    }

    void EnsureHealthFrameImage(Slider slider)
    {
        if (!lowPolyHealthUiMode || slider == null)
            return;

        Transform frameTransform = slider.transform.Find("TankHealthFrame");
        Image frameImage = null;
        RectTransform frameRect = null;

        if (frameTransform != null)
        {
            frameImage = frameTransform.GetComponent<Image>();
            frameRect = frameTransform as RectTransform;
        }

        if (frameImage == null)
        {
            GameObject frameObject = new GameObject("TankHealthFrame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(slider.transform, false);
            frameRect = frameObject.GetComponent<RectTransform>();
            frameImage = frameObject.GetComponent<Image>();
            frameObject.transform.SetAsFirstSibling();
        }

        if (frameRect != null)
        {
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
        }

        frameImage.color = healthFrameColor;
        frameImage.raycastTarget = false;
    }
}
