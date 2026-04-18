//using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairScript : MonoBehaviour
{
    enum CrosshairTargetState
    {
        Default,
        Enemy,
        Obstacle
    }

    [Header("Crosshair Settings")]
    [Tooltip("Aim source for targeting and fallback raycasts")]
    [SerializeField] private AimController aimController;
    
    [Tooltip("The main camera (usually assigned automatically)")]
    [SerializeField] private Camera mainCamera;
    
    [Tooltip("If true, crosshair stays at screen center. If false, follows target")]
    [SerializeField] private bool lockToCenter = false;

    [Tooltip("Max distance for aim ray when using AimController fallback")]
    [SerializeField] private float maxAimDistance = 500f;

    [Tooltip("Offset forward from aim source to avoid self-hit")]
    [SerializeField] private float muzzleForwardOffset = 1f;

    [Tooltip("Layer mask for AimController fallback raycast")]
    [SerializeField] private LayerMask aimLayerMask = -1;

    [Tooltip("Draw debug ray for AimController fallback")]
    [SerializeField] private bool showDebugRay = false;

    [Tooltip("How often to retry finding the active AimController when missing")]
    [SerializeField] private float aimResolveInterval = 0.5f;

    [Tooltip("Layer mask for center-screen target classification")]
    [SerializeField] private LayerMask detectionLayerMask = -1;

    [Tooltip("Max detection distance for crosshair state")]
    [SerializeField] private float detectionDistance = 500f;
    
    [Header("Visual Settings")]
    [Tooltip("Crosshair color")]
    [SerializeField] private Color crosshairColor = Color.white;

    [Tooltip("Optional custom sprite for the crosshair image")]
    [SerializeField] private Sprite crosshairSprite;

    [Tooltip("Inner ring color when aiming at enemy")]
    [SerializeField] private Color enemyInnerRingColor = new Color(1f, 0.18f, 0.18f, 1f);

    [Tooltip("Inner ring color when aiming at obstacle/ground")]
    [SerializeField] private Color obstacleInnerRingColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    
    [Tooltip("Size of the crosshair")]
    [SerializeField] private float crosshairSize = 1.5f;
    
    private RectTransform rectTransform;
    private Image crosshairImage;
    private Image innerRingImage;
    private Canvas canvas;
    private float nextAimResolveTime;

    private static Sprite cachedInnerRingSprite;
    
    void Start()
    {
        // Get components
        rectTransform = GetComponent<RectTransform>();
        crosshairImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        
        // Verify Canvas setup
        if (canvas != null)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                Debug.LogWarning("CrosshairScript: Canvas should be in Screen Space mode (Overlay or Camera)!");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
        else
        {
            Debug.LogError("CrosshairScript: No Canvas found! Crosshair must be a child of a Canvas.");
        }
        
        // Get main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = ResolveGameplayCamera();
        }
        
        // Auto-find AimController fallback if not assigned
        if (aimController == null)
        {
            aimController = ResolvePreferredAimController();
        }

        if (aimController == null)
            Debug.LogWarning("CrosshairScript: No AimController found in scene. Add one to your player tank.");
        
        // Apply visual settings
        if (crosshairImage != null)
        {
            if (crosshairSprite != null)
                crosshairImage.sprite = crosshairSprite;

            crosshairImage.color = crosshairColor;
        }

        EnsureInnerRingOverlay();
        
        // Set initial scale
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * crosshairSize;
        }
        
        // Hide system cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    AimController ResolvePreferredAimController()
    {
        GameObject preferredGreenTank = FindPreferredGreenTankObject();
        if (preferredGreenTank != null)
        {
            AimController preferredAim = preferredGreenTank.GetComponentInChildren<AimController>(true);
            if (preferredAim != null)
                return preferredAim;
        }

        AimController localAim = GetComponentInParent<AimController>();
        if (localAim != null)
            return localAim;

        GameObject taggedPlayer = TryFindTaggedObject("Player");
        if (taggedPlayer != null)
        {
            AimController playerAim = taggedPlayer.GetComponentInChildren<AimController>(true);
            if (playerAim != null)
                return playerAim;
        }

        return FindFirstObjectByType<AimController>();
    }

    static GameObject FindPreferredGreenTankObject()
    {
        GameObject preferred = GameObject.Find("minitank-v10-green 1");
        if (preferred != null)
            return preferred;

        return GameObject.Find("minitank-v10-green");
    }

    static GameObject TryFindTaggedObject(string tag)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch (UnityException)
        {
            return null;
        }
    }

    static Camera ResolveGameplayCamera()
    {
        Camera taggedMain = Camera.main;
        if (IsGameplayCamera(taggedMain))
            return taggedMain;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null)
                continue;

            if (candidate.name.Contains("PlayerCamera"))
                return candidate;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (IsGameplayCamera(candidate))
                return candidate;
        }

        return taggedMain;
    }

    static bool IsGameplayCamera(Camera camera)
    {
        if (camera == null || !camera.isActiveAndEnabled)
            return false;

        string cameraName = camera.name.ToLowerInvariant();
        if (cameraName.Contains("minimap"))
            return false;

        return true;
    }

    void Update()
    {
        if (rectTransform == null || mainCamera == null)
            return;

        EnsureAimControllerReference();
        
        if (lockToCenter)
        {
            // Simple mode: keep crosshair at screen center
            CenterCrosshair();
        }
        /*else
        {
            // Advanced mode: project target to screen
            ProjectTargetToScreen();
        }

        UpdateDynamicInnerRingColor();
        */
    }

    void LateUpdate()
    {
        if (rectTransform == null || mainCamera == null)
            return; 

        if (!lockToCenter)
        {
            ProjectTargetToScreen();
        }

        UpdateDynamicInnerRingColor();
    }

    void CenterCrosshair()
    {
        // Position at center of the screen
        rectTransform.anchoredPosition = Vector2.zero;
    }
    
    void ProjectTargetToScreen()
    {
        Vector3 targetPosition;

        if (aimController != null && aimController.HasValidTarget())
        {
            targetPosition = aimController.GetTargetPosition();
        }
        else if (aimController == null)
        {
            Vector3 aimDirection = aimController.GetAimDirection().normalized;
            Transform raySource = aimController.GetCannonTransform();
            if (raySource == null)
                raySource = aimController.GetTurretTransform();
                
                Debug.Log($"[Crosshair] Using fallback raycast from {(raySource != null ? raySource.name : "aimController position")} in direction {/*aimDirection*/Vector3.forward} (no AimController target)");

            Vector3 rayOrigin = (raySource != null ? raySource.position : aimController.transform.position) + /*aimDirection*/ Vector3.zero * muzzleForwardOffset;

            bool hitFound = TryRaycastIgnoringAimControllerSelf(rayOrigin, /*aimDirection*/ Vector3.forward, maxAimDistance, aimLayerMask, out RaycastHit hit);

            if (hitFound)
            {
                targetPosition = hit.point;
            }
            else
            {
                targetPosition = rayOrigin /*+ aimDirection*/ * maxAimDistance;
            }

           /* if (showDebugRay)
                Debug.DrawRay(rayOrigin, aimDirection * maxAimDistance, Color.cyan);*/
        }
        else
        {
            CenterCrosshair();
            return;
        }
        
        // Convert world position to screen position
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetPosition);
        
        // Check if point is in front of camera
        if (screenPoint.z > 0)
        {
            // Convert screen point to canvas position
            Vector2 canvasPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out canvasPosition
            );
            
            rectTransform.anchoredPosition = canvasPosition;
        }
        else
        {
            // Point is behind camera, keep at center
            CenterCrosshair();
        }
    }

    void EnsureAimControllerReference()
    {
        if (aimController != null && aimController.isActiveAndEnabled && aimController.gameObject.activeInHierarchy)
            return;

        if (Time.unscaledTime < nextAimResolveTime)
            return;

        aimController = ResolvePreferredAimController();
        nextAimResolveTime = Time.unscaledTime + Mathf.Max(0.1f, aimResolveInterval);
    }

    bool TryRaycastIgnoringAimControllerSelf(Vector3 origin, Vector3 direction, float distance, int mask, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        Transform selfRoot = aimController != null ? aimController.transform.root : null;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider candidateCollider = hits[i].collider;
            if (candidateCollider == null)
                continue;

            Transform candidate = candidateCollider.transform;
            if (selfRoot != null && candidate != null && candidate.IsChildOf(selfRoot))
                continue;

            hit = hits[i];
            return true;
        }

        hit = default;
        return false;
    }
    
    void OnDestroy()
    {
        // Restore cursor when destroyed
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void EnsureInnerRingOverlay()
    {
        if (rectTransform == null)
            return;

        Transform existing = transform.Find("InnerRingOverlay");
        if (existing != null)
            innerRingImage = existing.GetComponent<Image>();

        if (innerRingImage == null)
        {
            GameObject ringObject = new GameObject("InnerRingOverlay", typeof(RectTransform), typeof(Image));
            ringObject.transform.SetParent(transform, false);
            innerRingImage = ringObject.GetComponent<Image>();
        }

        if (innerRingImage == null)
            return;

        RectTransform ringRect = innerRingImage.rectTransform;
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = new Vector2(58f, 58f);
        ringRect.anchoredPosition = Vector2.zero;

        innerRingImage.sprite = GetInnerRingSprite();
        innerRingImage.type = Image.Type.Simple;
        innerRingImage.preserveAspect = true;
        innerRingImage.raycastTarget = false;
        innerRingImage.color = crosshairColor;
    }

    void UpdateDynamicInnerRingColor()
    {
        if (innerRingImage == null || mainCamera == null)
            return;

        CrosshairTargetState state = ClassifyCenterTarget();
        switch (state)
        {
            case CrosshairTargetState.Enemy:
                innerRingImage.color = enemyInnerRingColor;
                break;
            case CrosshairTargetState.Obstacle:
                innerRingImage.color = obstacleInnerRingColor;
                break;
            default:
                innerRingImage.color = crosshairColor;
                break;
        }
    }

    CrosshairTargetState ClassifyCenterTarget()
    {
        Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = mainCamera.ScreenPointToRay(centerScreen);

        if (!Physics.Raycast(ray, out RaycastHit hit, detectionDistance, detectionLayerMask))
            return CrosshairTargetState.Default;

        if (HasTagInHierarchy(hit.transform, "Enemy"))
            return CrosshairTargetState.Enemy;

        return CrosshairTargetState.Obstacle;
    }

    static bool HasTagInHierarchy(Transform current, string tag)
    {
        Transform walker = current;
        while (walker != null)
        {
            try
            {
                if (walker.CompareTag(tag))
                    return true;
            }
            catch (UnityException)
            {
                return false;
            }

            walker = walker.parent;
        }

        return false;
    }

    static Sprite GetInnerRingSprite()
    {
        if (cachedInnerRingSprite != null)
            return cachedInnerRingSprite;

        const int size = 128;
        const float outerRadius = 47f;
        const float thickness = 6f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float innerRadius = outerRadius - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                bool onRing = distance <= outerRadius && distance >= innerRadius;
                pixels[y * size + x] = onRing ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        cachedInnerRingSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedInnerRingSprite;
    }
    
    // Public methods to change settings at runtime
    public void SetLockToCenter(bool value)
    {
        lockToCenter = value;
    }
    
    public void SetCrosshairColor(Color color)
    {
        crosshairColor = color;
        if (crosshairImage != null)
        {
            crosshairImage.color = color;
        }

        if (innerRingImage != null)
            innerRingImage.color = color;
    }
    
    public void SetCrosshairSize(float size)
    {
        crosshairSize = size;
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * size;
        }
    }

    public void SetCrosshairSprite(Sprite sprite)
    {
        crosshairSprite = sprite;
        if (crosshairImage != null && sprite != null)
            crosshairImage.sprite = sprite;
    }
}
