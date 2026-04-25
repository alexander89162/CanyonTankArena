using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankController : MonoBehaviour
{
    public MovementController movement;
    private TankSlope tankSlope;

    public Transform[] turrets = new Transform[3];
    public UIVirtualJoystick movementJoystick;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 30f;
    [SerializeField] private float dashCooldown = 0.35f;
    [SerializeField] private float dashStepDistance = 0.5f;

    [Header("Dash After Image")]
    [SerializeField] private float afterImageLifetime = 0.32f;
    [SerializeField] private Color afterImageTint = new Color(1f, 1f, 1f, 0.04f);
    [SerializeField] private int afterImageGhostCount = 4;
    [SerializeField] private float afterImageMinSpacing = 3f;

    private int turretIndex = 0;
    private Transform activeTurret;
    private CharacterController characterController;

    [SerializeField] private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction dashAction;
    private Vector2 keyboardMoveInput;
    private Vector2 touchMoveInput;
    private float nextDashTime;
    private UIVirtualButton dashButton;
    private bool dashButtonBound;
    private Renderer[] cachedRenderers;

    private const string TanksActionMapName = "Tanks";
    private const string MoveActionName = "Move";
    private const string DashActionName = "Dash";
    private const string DashButtonObjectName = "DashButton";

    private void Awake()
    {
        SwitchToTurret(turretIndex);

        if (movement == null)
            movement = GetComponent<MovementController>();

        if (tankSlope == null)
            tankSlope = GetComponent<TankSlope>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            RefreshCachedRenderers();

        CacheActions();
        TryBindDashButton();
    }

    private void OnEnable()
    {
        TryBindDashButton();
    }

    private void Start()
    {
        TryBindDashButton();
        RefreshCachedRenderers();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        keyboardMoveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
            TryDash();
    }

    public void OnCycleTurret(InputAction.CallbackContext context)
    {
        if (context.performed)
            CycleTurret();
    }

    public void SetMoveInput(Vector2 newMoveInput)
    {
        touchMoveInput = newMoveInput;
    }

    public void SetDashButtonState(bool isPressed)
    {
        if (isPressed)
            TryDash();
    }

    public void CycleTurret()
    {
        turretIndex = (turretIndex + 1) % 3;
        SwitchToTurret(turretIndex);
    }

    public void SetTurretCycleButtonState(bool isPressed)
    {
        if (isPressed)
            CycleTurret();
    }

    private void Update()
    {
        Vector2 effectiveMoveInput = GetEffectiveMoveInput();

        if (movement != null)
            movement.moveInput = effectiveMoveInput;

        if (movementJoystick != null)
            movementJoystick.SetVisualInput(effectiveMoveInput);

        if (IsDashPressedThisFrame())
            TryDash();

        if (!dashButtonBound && Time.frameCount % 30 == 0)
            TryBindDashButton();
    }

    private Vector2 GetEffectiveMoveInput()
    {
        if (touchMoveInput.sqrMagnitude > 0.0001f)
            return touchMoveInput;

        Vector2 actionMoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (actionMoveInput.sqrMagnitude > 0.0001f)
            return actionMoveInput;

        return keyboardMoveInput;
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputActionMap tanksMap = playerInput.actions.FindActionMap(TanksActionMapName, false);
        if (tanksMap == null)
            return;

        moveAction = tanksMap.FindAction(MoveActionName, false);
        dashAction = tanksMap.FindAction(DashActionName, false);
    }

    private bool IsDashPressedThisFrame()
    {
        if (dashAction != null && dashAction.WasPressedThisFrame())
            return true;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return false;
    }

    private void TryBindDashButton()
    {
        if (dashButtonBound)
            return;

        UIVirtualButton[] virtualButtons = Object.FindObjectsByType<UIVirtualButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < virtualButtons.Length; i++)
        {
            UIVirtualButton candidate = virtualButtons[i];
            if (candidate == null || candidate.gameObject.name != DashButtonObjectName)
                continue;

            dashButton = candidate;
            dashButton.buttonStateOutputEvent.RemoveListener(SetDashButtonState);
            dashButton.buttonStateOutputEvent.AddListener(SetDashButtonState);
            dashButtonBound = true;
            return;
        }
    }

    private void TryDash()
    {
        if (Time.time < nextDashTime)
            return;

        Vector3 dashDirection = GetDashDirection(transform.position);
        if (dashDirection.sqrMagnitude < 0.0001f)
            return;

        nextDashTime = Time.time + dashCooldown;

        if (characterController != null)
        {
            DashAlongGround(dashDirection);
            return;
        }

        transform.position += dashDirection * dashDistance;
    }

    private void DashAlongGround(Vector3 dashDirection)
    {
        List<Pose> dashSamples = new List<Pose>(afterImageGhostCount + 1);
        dashSamples.Add(new Pose(transform.position, transform.rotation));

        float remainingDistance = dashDistance;

        while (remainingDistance > 0f)
        {
            float stepDistance = Mathf.Min(dashStepDistance, remainingDistance);
            Vector3 groundNormal = GetGroundNormal(transform.position);
            Vector3 stepDirection = Vector3.ProjectOnPlane(dashDirection, groundNormal);

            if (stepDirection.sqrMagnitude < 0.0001f)
                stepDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal);

            if (stepDirection.sqrMagnitude < 0.0001f)
                break;

            characterController.Move(stepDirection.normalized * stepDistance);
            dashSamples.Add(new Pose(transform.position, transform.rotation));
            remainingDistance -= stepDistance;
        }

        SpawnDashAfterImages(dashSamples);
    }

    private Vector3 GetDashDirection(Vector3 samplePosition)
    {
        Vector3 groundNormal = GetGroundNormal(samplePosition);
        Vector3 dashDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal);

        if (dashDirection.sqrMagnitude > 0.0001f)
            return dashDirection.normalized;

        return Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
    }

    private Vector3 GetGroundNormal(Vector3 samplePosition)
    {
        if (characterController == null)
            return Vector3.up;

        Bounds bounds = characterController.bounds;
        Vector3 rayStart = new Vector3(samplePosition.x, bounds.max.y + 0.1f, samplePosition.z);
        float rayDistance = bounds.size.y + 3f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
            return hit.normal;

        return Vector3.up;
    }

    private void SpawnDashAfterImages(List<Pose> dashSamples)
    {
        RefreshCachedRenderers();
        Debug.Log($"[AfterImage] RefreshCachedRenderers found {cachedRenderers.Length} renderers");

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            Debug.LogWarning("[AfterImage] No renderers found - returning early");
            return;
        }

        if (dashSamples == null || dashSamples.Count == 0)
        {
            Debug.LogWarning("[AfterImage] No dash samples provided - returning early");
            return;
        }

        Debug.Log($"[AfterImage] Building spaced samples from {dashSamples.Count} dash samples (minSpacing={afterImageMinSpacing})");
        List<Pose> spacedSamples = BuildSpacedAfterImageSamples(dashSamples);
        Debug.Log($"[AfterImage] Got {spacedSamples.Count} spaced samples after filtering");
        if (spacedSamples.Count == 0)
        {
            Debug.LogWarning("[AfterImage] No spaced samples after filtering - returning early");
            return;
        }

        int ghostLimit = Mathf.Min(afterImageGhostCount, spacedSamples.Count);
        if (ghostLimit <= 0)
        {
            Debug.LogWarning("[AfterImage] Ghost limit is 0 or less - returning early");
            return;
        }

        Debug.Log($"[AfterImage] Spawning {ghostLimit} ghost afterimages");

        if (ghostLimit == 1)
        {
            Debug.Log($"[AfterImage] Single ghost mode - spawning at position {spacedSamples[spacedSamples.Count - 1].position}");
            SpawnAfterImageGhost(spacedSamples[spacedSamples.Count - 1], afterImageTint.a * 0.6f);
            return;
        }

        for (int i = 0; i < ghostLimit; i++)
        {
            float sampleT = (float)i / (ghostLimit - 1);
            int sampleIndex = Mathf.RoundToInt(sampleT * (spacedSamples.Count - 1));
            float blend = sampleT;
            float alpha = Mathf.Lerp(afterImageTint.a, afterImageTint.a * 0.12f, blend);
            Debug.Log($"[AfterImage] Ghost {i}: sampleIndex={sampleIndex}, position={spacedSamples[sampleIndex].position}, alpha={alpha}");
            SpawnAfterImageGhost(spacedSamples[sampleIndex], alpha);
        }
    }

    private List<Pose> BuildSpacedAfterImageSamples(List<Pose> dashSamples)
    {
        List<Pose> spacedSamples = new List<Pose>(dashSamples.Count);
        float minSpacing = Mathf.Max(0.01f, afterImageMinSpacing);

        for (int i = 0; i < dashSamples.Count; i++)
        {
            Pose sample = dashSamples[i];
            if (spacedSamples.Count == 0)
            {
                spacedSamples.Add(sample);
                continue;
            }

            Pose previous = spacedSamples[spacedSamples.Count - 1];
            if ((sample.position - previous.position).sqrMagnitude >= minSpacing * minSpacing)
                spacedSamples.Add(sample);
        }

        Pose endSample = dashSamples[dashSamples.Count - 1];
        if (spacedSamples.Count == 0 || (endSample.position - spacedSamples[spacedSamples.Count - 1].position).sqrMagnitude > 0.0001f)
            spacedSamples.Add(endSample);

        return spacedSamples;
    }
    private void SpawnAfterImageGhost(Pose pose, float alpha)
    {
        Debug.Log($"[AfterImage] SpawnAfterImageGhost called with pose={pose.position}, alpha={alpha}");
        GameObject ghostRoot = new GameObject($"{gameObject.name}_AfterImage");
        ghostRoot.transform.SetPositionAndRotation(pose.position, pose.rotation);
        ghostRoot.transform.localScale = transform.localScale;
        Debug.Log($"[AfterImage] Created ghostRoot at {ghostRoot.transform.position}");

        int ghostChildCount = 0;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer sourceRenderer = cachedRenderers[i];
            if (sourceRenderer == null || !sourceRenderer.enabled || !sourceRenderer.gameObject.activeInHierarchy)
            {
                Debug.Log($"[AfterImage] Renderer {i} filtered: null={sourceRenderer == null}, enabled={sourceRenderer?.enabled}, activeInHierarchy={sourceRenderer?.gameObject.activeInHierarchy}");
                continue;
            }

            MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
            SkinnedMeshRenderer sourceSkinnedMeshRenderer = sourceRenderer as SkinnedMeshRenderer;

            if (sourceMeshFilter == null && sourceSkinnedMeshRenderer == null)
            {
                Debug.Log($"[AfterImage] Renderer {i} has no mesh: MeshFilter={sourceMeshFilter}, SkinnedMeshRenderer={sourceSkinnedMeshRenderer}");
                continue;
            }

            ghostChildCount++;
            Debug.Log($"[AfterImage] Creating ghost child for renderer '{sourceRenderer.gameObject.name}'");
            GameObject ghostChild = new GameObject(sourceRenderer.gameObject.name);
            ghostChild.transform.SetParent(ghostRoot.transform, false);
            ghostChild.transform.position = sourceRenderer.transform.position;
            ghostChild.transform.rotation = sourceRenderer.transform.rotation;
            ghostChild.transform.localScale = sourceRenderer.transform.lossyScale;

            Mesh ghostMesh = null;

            if (sourceSkinnedMeshRenderer != null)
            {
                ghostMesh = new Mesh();
                sourceSkinnedMeshRenderer.BakeMesh(ghostMesh);
            }
            else if (sourceMeshFilter != null)
            {
                ghostMesh = sourceMeshFilter.sharedMesh;
            }

            if (ghostMesh == null)
            {
                Debug.LogWarning($"[AfterImage] Failed to get mesh for renderer '{sourceRenderer.gameObject.name}'");
                Destroy(ghostChild);
                continue;
            }

            Debug.Log($"[AfterImage] Got mesh for renderer: {ghostMesh.name} with {ghostMesh.vertexCount} vertices");

            if (sourceSkinnedMeshRenderer != null)
                Destroy(ghostMesh, afterImageLifetime);

            MeshFilter ghostMeshFilter = ghostChild.AddComponent<MeshFilter>();
            ghostMeshFilter.sharedMesh = ghostMesh;

            MeshRenderer ghostRenderer = ghostChild.AddComponent<MeshRenderer>();
            Material[] ghostMats = BuildGhostMaterials(sourceRenderer.sharedMaterials, alpha);
            Debug.Log($"[AfterImage] Created {ghostMats.Length} ghost materials");
            ghostRenderer.sharedMaterials = ghostMats;
            ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghostRenderer.receiveShadows = false;
        }

        Debug.Log($"[AfterImage] Finished spawning ghost with {ghostChildCount} child renderers");
        Destroy(ghostRoot, afterImageLifetime);
    }

    private Material[] BuildGhostMaterials(Material[] sourceMaterials, float alpha)
    {
        if (sourceMaterials == null || sourceMaterials.Length == 0)
            return System.Array.Empty<Material>();

        Material[] ghostMaterials = new Material[sourceMaterials.Length];
        Color targetColor = new Color(afterImageTint.r, afterImageTint.g, afterImageTint.b, alpha);

        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            Material sourceMaterial = sourceMaterials[i];
            if (sourceMaterial == null)
                continue;

            Material ghostMaterial = CreateGhostMaterial(sourceMaterial);
            SetMaterialTransparent(ghostMaterial);
            ApplyGhostColor(ghostMaterial, targetColor);
            Destroy(ghostMaterial, afterImageLifetime);
            ghostMaterials[i] = ghostMaterial;
        }

        return ghostMaterials;
    }

    private void ApplyGhostColor(Material ghostMaterial, Color color)
    {
        if (ghostMaterial == null)
            return;

        if (ghostMaterial.HasProperty("_BaseColor"))
            ghostMaterial.SetColor("_BaseColor", color);

        if (ghostMaterial.HasProperty("_Color"))
            ghostMaterial.SetColor("_Color", color);

        ghostMaterial.color = color;
    }

    private void SetMaterialTransparent(Material ghostMaterial)
    {
        if (ghostMaterial == null)
            return;

        ghostMaterial.SetOverrideTag("RenderType", "Transparent");

        if (ghostMaterial.HasProperty("_Mode"))
            ghostMaterial.SetFloat("_Mode", 3f);

        if (ghostMaterial.HasProperty("_Surface"))
            ghostMaterial.SetFloat("_Surface", 1f);

        if (ghostMaterial.HasProperty("_Blend"))
            ghostMaterial.SetFloat("_Blend", 0f);

        if (ghostMaterial.HasProperty("_AlphaClip"))
            ghostMaterial.SetFloat("_AlphaClip", 0f);

        if (ghostMaterial.HasProperty("_SrcBlend"))
            ghostMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (ghostMaterial.HasProperty("_DstBlend"))
            ghostMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (ghostMaterial.HasProperty("_ZWrite"))
            ghostMaterial.SetFloat("_ZWrite", 0f);

        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        ghostMaterial.renderQueue = 3000;
    }

    private Material CreateGhostMaterial(Material sourceMaterial)
    {
        Shader transparentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (transparentShader == null)
            transparentShader = Shader.Find("Unlit/Transparent");

        Material ghostMaterial = transparentShader != null
            ? new Material(transparentShader)
            : new Material(sourceMaterial);

        CopySourceTexture(sourceMaterial, ghostMaterial);
        return ghostMaterial;
    }

    private void CopySourceTexture(Material sourceMaterial, Material ghostMaterial)
    {
        if (sourceMaterial == null || ghostMaterial == null)
            return;

        if (sourceMaterial.HasProperty("_BaseMap") && ghostMaterial.HasProperty("_BaseMap"))
        {
            ghostMaterial.SetTexture("_BaseMap", sourceMaterial.GetTexture("_BaseMap"));
            ghostMaterial.SetTextureOffset("_BaseMap", sourceMaterial.GetTextureOffset("_BaseMap"));
            ghostMaterial.SetTextureScale("_BaseMap", sourceMaterial.GetTextureScale("_BaseMap"));
        }

        if (sourceMaterial.HasProperty("_MainTex") && ghostMaterial.HasProperty("_MainTex"))
        {
            ghostMaterial.SetTexture("_MainTex", sourceMaterial.GetTexture("_MainTex"));
            ghostMaterial.SetTextureOffset("_MainTex", sourceMaterial.GetTextureOffset("_MainTex"));
            ghostMaterial.SetTextureScale("_MainTex", sourceMaterial.GetTextureScale("_MainTex"));
        }
    }

    private void RefreshCachedRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnDestroy()
    {
        if (dashButton != null)
            dashButton.buttonStateOutputEvent.RemoveListener(SetDashButtonState);
    }

    private void SwitchToTurret(int index)
    {
        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] != null)
                turrets[i].gameObject.SetActive(i == index);
        }

        if (turrets[index] != null)
            activeTurret = turrets[index];
    }
}
