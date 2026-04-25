using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class BattleUISceneSetup
{
    [MenuItem("Tools/Battle UI/Setup In Active Scene")]
    public static void SetupInActiveScene()
    {
        SetupInActiveSceneInternal(false);
    }

    [MenuItem("Tools/Battle UI/Setup In Lowpoly Mode")]
    public static void SetupInLowpolyMode()
    {
        SetupInActiveSceneInternal(true);
    }

    [MenuItem("Tools/Battle UI/Setup Mobile UI")]
    public static void SetupMobileUI()
    {
        // Create the standard UI first, then tweak layout for mobile
        SetupInActiveSceneInternal(false);

        const float mobileHudMargin = 20f;
        const float mobileHealthTopOffset = 12f;
        const float mobileMinimapSize = 500f;
        const float mobileMinimapRightOffset = 96f;
        const float mobileHealthAmmoGap = 1f;
        const float mobileAmmoSlotWidth = 124f;
        const float mobileAmmoSlotSpacing = 6f;
        const float mobileAmmoContainerHeight = 96f;
        float mobileAmmoContainerWidth = (mobileAmmoSlotWidth * 3f) + (mobileAmmoSlotSpacing * 4f);

        GameObject battleUiRoot = GameObject.Find("BattleUI");
        if (battleUiRoot == null)
            return;

        Transform playerTransform = FindPlayerTransform();
        TankController playerController = ResolveComponentForPlayer<TankController>(playerTransform);
        CameraController cameraController = ResolveComponentForPlayer<CameraController>(playerTransform);
        AimController aimController = ResolveComponentForPlayer<AimController>(playerTransform);
        CannonFiring cannonFiring = ResolveComponentForPlayer<CannonFiring>(playerTransform);
        PauseMenuController pauseManager = ResolveSceneComponent<PauseMenuController>();

        Transform healthRoot = battleUiRoot.transform.Find("HealthTopRight");
        Transform ammoRoot = battleUiRoot.transform.Find("AmmoBottom");
        Transform minimapRoot = battleUiRoot.transform.Find("MiniMapTopLeft");

        RectTransform healthRect = healthRoot != null ? EnsureRectTransform(healthRoot.gameObject) : null;
        RectTransform ammoRect = ammoRoot != null ? EnsureRectTransform(ammoRoot.gameObject) : null;
        RectTransform minimapRect = minimapRoot != null ? EnsureRectTransform(minimapRoot.gameObject) : null;

        // Move health to top-left, minimap to top-right, and ammo below health on left
        if (healthRect != null)
        {
            healthRect.anchorMin = new Vector2(0f, 1f);
            healthRect.anchorMax = new Vector2(0f, 1f);
            healthRect.pivot = new Vector2(0f, 1f);
            // make health area compact and aligned for mobile
            healthRect.sizeDelta = new Vector2(420f, 112f);
            healthRect.anchoredPosition = new Vector2(mobileHudMargin, -mobileHealthTopOffset);

            // If a HealthBar exists inside, increase its visual height immediately
            Transform healthSliderT = healthRoot.Find("HealthBar");
            if (healthSliderT != null)
            {
                RectTransform sliderRect = healthSliderT.GetComponent<RectTransform>();
                if (sliderRect != null)
                {
                    sliderRect.anchorMin = new Vector2(0f, 1f);
                    sliderRect.anchorMax = new Vector2(1f, 1f);
                    sliderRect.pivot = new Vector2(0.5f, 1f);
                    sliderRect.sizeDelta = new Vector2(0f, 56f);
                    sliderRect.anchoredPosition = new Vector2(0f, -4f);
                }
            }

            // Move health text inside the bar
            Transform healthTextT = healthRoot.Find("HealthText");
            if (healthTextT != null)
            {
                RectTransform htRect = healthTextT.GetComponent<RectTransform>();
                TextMeshProUGUI ht = healthTextT.GetComponent<TextMeshProUGUI>();
                if (htRect != null)
                {
                    htRect.anchorMin = new Vector2(0f, 0f);
                    htRect.anchorMax = new Vector2(1f, 1f);
                    htRect.pivot = new Vector2(0.5f, 0.5f);
                    htRect.sizeDelta = Vector2.zero;
                    htRect.anchoredPosition = Vector2.zero;
                }
                if (ht != null)
                {
                    ht.alignment = TextAlignmentOptions.Center;
                    ht.fontSize = Mathf.Max(22, ht.fontSize);
                }
            }
        }

        if (minimapRect != null)
        {
            // move minimap to the top-right where health previously sat
            minimapRect.anchorMin = new Vector2(1f, 1f);
            minimapRect.anchorMax = new Vector2(1f, 1f);
            minimapRect.pivot = new Vector2(1f, 1f);
            minimapRect.sizeDelta = new Vector2(mobileMinimapSize, mobileMinimapSize);
            minimapRect.anchoredPosition = new Vector2(-(mobileHudMargin + mobileMinimapRightOffset), -mobileHudMargin);
        }

        if (ammoRect != null && healthRect != null)
        {
            // Anchor ammo to top-left as well and position directly below health
            ammoRect.anchorMin = new Vector2(0f, 1f);
            ammoRect.anchorMax = new Vector2(0f, 1f);
            ammoRect.pivot = new Vector2(0f, 1f);
            float gap = mobileHealthAmmoGap;
            ammoRect.sizeDelta = new Vector2(mobileAmmoContainerWidth, mobileAmmoContainerHeight);
            float healthTopY = healthRect.anchoredPosition.y;
            float healthHeight = healthRect.sizeDelta.y;
            float ammoTopY = healthTopY - healthHeight - gap;
            float ammoLeftX = mobileHudMargin + Mathf.Max(0f, (healthRect.sizeDelta.x - mobileAmmoContainerWidth) * 0.5f);
            ammoRect.anchoredPosition = new Vector2(ammoLeftX, ammoTopY);
        }

        HUDController hudController = battleUiRoot.GetComponent<HUDController>();
        if (hudController != null)
        {
            SerializedObject serializedHud = new SerializedObject(hudController);
            // Tweak default slot sizes for mobile
            serializedHud.FindProperty("ammoSlotWidth").floatValue = mobileAmmoSlotWidth;
            serializedHud.FindProperty("ammoSlotHeight").floatValue = 88f;
            serializedHud.FindProperty("ammoSlotSpacing").floatValue = mobileAmmoSlotSpacing;
            serializedHud.FindProperty("ammoIconSize").floatValue = 48f;
            serializedHud.FindProperty("ammoSlotPadding").floatValue = 10f;
            SerializedProperty ammoMarginProp = serializedHud.FindProperty("ammoScreenMargin");
            if (ammoMarginProp != null)
                ammoMarginProp.vector2Value = new Vector2(10f, 8f);
            SerializedProperty minimapSizeProp = serializedHud.FindProperty("minimapSize");
            if (minimapSizeProp != null)
                minimapSizeProp.vector2Value = new Vector2(mobileMinimapSize, mobileMinimapSize);
            SerializedProperty minimapOffsetProp = serializedHud.FindProperty("minimapScreenOffset");
            if (minimapOffsetProp != null)
                minimapOffsetProp.vector2Value = new Vector2(mobileMinimapRightOffset, 0f);
            // mark HUD as mobile so runtime layout code doesn't override editor placement
            SerializedProperty mobileProp = serializedHud.FindProperty("mobileUiLayout");
            if (mobileProp != null)
                mobileProp.boolValue = true;
            // make health bar larger and move health text inside at runtime
            SerializedProperty healthBarHeightProp = serializedHud.FindProperty("healthBarHeight");
            if (healthBarHeightProp != null)
                healthBarHeightProp.floatValue = 56f;
            SerializedProperty healthTextOffsetProp = serializedHud.FindProperty("healthTextOffsetBelowBar");
            if (healthTextOffsetProp != null)
                healthTextOffsetProp.floatValue = 6f;
            // place ammo to left, not right
            SerializedProperty bulletSideProp = serializedHud.FindProperty("bulletCountOnRight");
            if (bulletSideProp != null)
                bulletSideProp.boolValue = false;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hudController);
        }

        SetupMobileTouchControls(battleUiRoot.transform, playerController, cameraController, aimController, cannonFiring, pauseManager);

        Transform crosshair = battleUiRoot.transform.Find("Crosshair");
        if (crosshair != null)
            crosshair.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = battleUiRoot;
        Debug.Log("Mobile Battle UI setup complete in active scene.");
    }

    static void SetupInActiveSceneInternal(bool lowPolyMode)
    {
        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        GameObject battleUiRoot = GetOrCreateChild(canvas.gameObject, "BattleUI");
        RectTransform battleUiRect = battleUiRoot.GetComponent<RectTransform>();
        if (battleUiRect == null)
            battleUiRect = battleUiRoot.AddComponent<RectTransform>();
        StretchToParent(battleUiRect);

        HUDController hudController = battleUiRoot.GetComponent<HUDController>();
        if (hudController == null)
            hudController = battleUiRoot.AddComponent<HUDController>();

        EnsureCrosshair(battleUiRoot.transform);

        GameObject healthRoot = GetOrCreateChild(battleUiRoot, "HealthTopRight");
        RectTransform healthRootRect = EnsureRectTransform(healthRoot);
        healthRootRect.anchorMin = new Vector2(1f, 1f);
        healthRootRect.anchorMax = new Vector2(1f, 1f);
        healthRootRect.pivot = new Vector2(1f, 1f);
        healthRootRect.sizeDelta = new Vector2(280f, 70f);
        healthRootRect.anchoredPosition = new Vector2(-24f, -24f);

        Slider healthSlider = GetOrCreateSlider(healthRoot.transform, "HealthBar");
        ApplyTankHealthBarStyle(healthSlider);
        RectTransform healthSliderRect = healthSlider.GetComponent<RectTransform>();
        healthSliderRect.anchorMin = new Vector2(0f, 0f);
        healthSliderRect.anchorMax = new Vector2(1f, 0f);
        healthSliderRect.pivot = new Vector2(0.5f, 0f);
        healthSliderRect.sizeDelta = new Vector2(0f, 22f);
        healthSliderRect.anchoredPosition = new Vector2(0f, 8f);

        TextMeshProUGUI healthText = GetOrCreateTMPText(healthRoot.transform, "HealthText", "HP 100/100", 24, TextAlignmentOptions.TopRight);
        RectTransform healthTextRect = healthText.GetComponent<RectTransform>();
        healthTextRect.anchorMin = new Vector2(0f, 1f);
        healthTextRect.anchorMax = new Vector2(1f, 1f);
        healthTextRect.pivot = new Vector2(1f, 1f);
        healthTextRect.sizeDelta = new Vector2(0f, 30f);
        healthTextRect.anchoredPosition = new Vector2(0f, 0f);

        GameObject minimapRoot = GetOrCreateChild(battleUiRoot, "MiniMapTopLeft");
        RectTransform minimapRect = EnsureRectTransform(minimapRoot);
        minimapRect.anchorMin = new Vector2(0f, 1f);
        minimapRect.anchorMax = new Vector2(0f, 1f);
        minimapRect.pivot = new Vector2(0f, 1f);
        minimapRect.sizeDelta = new Vector2(500f, 500f);
        minimapRect.anchoredPosition = new Vector2(24f, -24f);

        RemoveRadarVisuals(minimapRoot.transform);

        RawImage minimapImage = EnsureCircularMinimapRootAndGetView(minimapRoot);

        MinimapRadarOverlay radarOverlay = minimapRoot.GetComponent<MinimapRadarOverlay>();
        if (radarOverlay == null)
            radarOverlay = minimapRoot.AddComponent<MinimapRadarOverlay>();
        radarOverlay.SetMinimapView(minimapImage);

        SerializedObject serializedRadar = new SerializedObject(radarOverlay);
        serializedRadar.FindProperty("lowPolyUiMode").boolValue = lowPolyMode;
        serializedRadar.FindProperty("showCompassRing").boolValue = !lowPolyMode;
        serializedRadar.FindProperty("showDiagonalGrid").boolValue = !lowPolyMode;
        serializedRadar.FindProperty("showRadarSweep").boolValue = !lowPolyMode;
        serializedRadar.ApplyModifiedPropertiesWithoutUndo();

        Image minimapMaskImage = minimapRoot.GetComponent<Image>();
        if (minimapMaskImage == null)
            minimapMaskImage = minimapRoot.AddComponent<Image>();

        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circleSprite != null)
            minimapMaskImage.sprite = circleSprite;

        minimapMaskImage.type = Image.Type.Simple;
        minimapMaskImage.preserveAspect = true;
        minimapMaskImage.color = Color.white;

        Mask minimapMask = minimapRoot.GetComponent<Mask>();
        if (minimapMask == null)
            minimapMask = minimapRoot.AddComponent<Mask>();
        minimapMask.showMaskGraphic = false;

        RectTransform minimapImageRect = minimapImage.GetComponent<RectTransform>();
        StretchToParent(minimapImageRect);

        TextMeshProUGUI playerMarker = GetOrCreateTMPText(minimapRoot.transform, "PlayerMarker", "▲", 34, TextAlignmentOptions.Center);
        RemoveDuplicatePlayerMarkers(minimapRoot.transform, playerMarker.transform);
        RectTransform playerMarkerRect = playerMarker.GetComponent<RectTransform>();
        playerMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        playerMarkerRect.sizeDelta = new Vector2(36f, 36f);
        playerMarkerRect.anchoredPosition = Vector2.zero;
        playerMarker.color = Color.green;
        playerMarker.raycastTarget = false;
        playerMarker.transform.SetAsLastSibling();

        GameObject ammoRoot = GetOrCreateChild(battleUiRoot, "AmmoBottom");
        RectTransform ammoRootRect = EnsureRectTransform(ammoRoot);
        ammoRootRect.anchorMin = new Vector2(1f, 0f);
        ammoRootRect.anchorMax = new Vector2(1f, 0f);
        ammoRootRect.pivot = new Vector2(1f, 0f);
        ammoRootRect.sizeDelta = new Vector2(280f, 44f);
        ammoRootRect.anchoredPosition = new Vector2(-24f, 24f);

        // Remove the single overlaying ammo text and create three boxed ammo slots
        ammoRootRect.sizeDelta = new Vector2(340f, 92f);

        int slotCount = 3;
        float slotWidth = 120f;
        float slotHeight = 140f; // tallish rectangle
        float slotSpacing = 16f;

        for (int i = 0; i < slotCount; i++)
        {
            string slotName = $"AmmoSlot{ i + 1 }";
            Transform existingSlot = ammoRoot.transform.Find(slotName);
            GameObject slotObj = null;
            if (existingSlot != null)
                slotObj = existingSlot.gameObject;

            if (slotObj == null)
            {
                slotObj = new GameObject(slotName, typeof(RectTransform));
                slotObj.transform.SetParent(ammoRoot.transform, false);
            }

            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(1f, 0f);
            slotRect.anchorMax = new Vector2(1f, 0f);
            slotRect.pivot = new Vector2(1f, 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);
            slotRect.anchoredPosition = new Vector2(-slotSpacing - (i * (slotWidth + slotSpacing)), slotSpacing);

            // Background box
            Transform bgTransform = slotObj.transform.Find("SlotBg");
            Image bgImage = null;
            if (bgTransform != null)
                bgImage = bgTransform.GetComponent<Image>();

            if (bgImage == null)
            {
                GameObject bgObj = new GameObject("SlotBg", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(slotObj.transform, false);
                bgImage = bgObj.GetComponent<Image>();
            }

            RectTransform bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgImage.color = new Color(0.06f, 0.07f, 0.06f, 0.95f);
            bgImage.raycastTarget = false;

            // Icon (top)
            Transform iconTransform = slotObj.transform.Find("AmmoIcon");
            Image iconImage = null;
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();

            if (iconImage == null)
            {
                GameObject iconObj = new GameObject("AmmoIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(slotObj.transform, false);
                iconImage = iconObj.GetComponent<Image>();
            }

            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            // centered near the top inside the box
            iconRect.anchoredPosition = new Vector2(0f, -12f);
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            Sprite defaultIcon = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (iconImage.sprite == null && defaultIcon != null)
                iconImage.sprite = defaultIcon;

            // Count text (bottom)
            TextMeshProUGUI countText = GetOrCreateTMPText(slotObj.transform, $"AmmoCount{ i + 1 }", "?/ ?", 20, TextAlignmentOptions.Bottom | TextAlignmentOptions.Center);
            RectTransform countRect = countText.GetComponent<RectTransform>();
            // center the text rect horizontally at the bottom of the slot
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(0.5f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.sizeDelta = new Vector2(slotWidth - 16f, 24f);
            // positioned near the bottom inside the box
            countRect.anchoredPosition = new Vector2(0f, 12f);
            countText.color = Color.white;
        }

        Camera minimapCamera = GetOrCreateMinimapCamera();
        Transform playerTransform = FindPlayerTransform();
        MinimapCameraFollow minimapFollow = minimapCamera.GetComponent<MinimapCameraFollow>();
        if (minimapFollow == null)
            minimapFollow = minimapCamera.gameObject.AddComponent<MinimapCameraFollow>();
        minimapFollow.SetRotateWithTarget(true);
        if (playerTransform != null)
            minimapFollow.SetTarget(playerTransform);

        MinimapPlayerMarker minimapPlayerMarker = playerMarker.GetComponent<MinimapPlayerMarker>();
        if (minimapPlayerMarker == null)
            minimapPlayerMarker = playerMarker.gameObject.AddComponent<MinimapPlayerMarker>();
        if (playerTransform != null)
            minimapPlayerMarker.SetTarget(playerTransform);

        RenderTexture minimapTexture = GetOrCreateMinimapRenderTexture();
        minimapCamera.targetTexture = minimapTexture;
        minimapImage.texture = minimapTexture;

        TankController playerController = null;
        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<TankController>();
            if (playerController == null)
                playerController = playerTransform.GetComponentInChildren<TankController>(true);
        }

        SerializedObject serializedHud = new SerializedObject(hudController);
        serializedHud.FindProperty("playerController").objectReferenceValue = playerController;
        serializedHud.FindProperty("healthBar").objectReferenceValue = healthSlider;
        if (healthSlider.fillRect != null)
            serializedHud.FindProperty("healthFillImage").objectReferenceValue = healthSlider.fillRect.GetComponent<Image>();
        serializedHud.FindProperty("healthText").objectReferenceValue = healthText;
        serializedHud.FindProperty("ammoContainer").objectReferenceValue = ammoRootRect;

        // Set default ammo layout values in HUDController so they are editable in inspector
        serializedHud.FindProperty("ammoSlotWidth").floatValue = 140f;
        serializedHud.FindProperty("ammoSlotHeight").floatValue = 104f;
        serializedHud.FindProperty("ammoSlotSpacing").floatValue = 18f;
        serializedHud.FindProperty("ammoIconSize").floatValue = 52f;
        serializedHud.FindProperty("ammoSlotPadding").floatValue = 12f;
        serializedHud.FindProperty("ammoSlotBgColor").colorValue = new Color(0.06f, 0.07f, 0.06f, 0.95f);

        // Populate HUDController arrays for icons and counts
        SerializedProperty iconArray = serializedHud.FindProperty("ammoIconImages");
        SerializedProperty countArray = serializedHud.FindProperty("ammoCountTexts");
        iconArray.arraySize = 3;
        countArray.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            string slotName = $"AmmoSlot{ i + 1 }";
            Transform slot = ammoRoot.transform.Find(slotName);
            Image icon = null;
            TextMeshProUGUI countText = null;
            if (slot != null)
            {
                Transform iconT = slot.Find("AmmoIcon");
                if (iconT != null)
                    icon = iconT.GetComponent<Image>();

                Transform countT = slot.Find($"AmmoCount{ i + 1 }");
                if (countT != null)
                    countText = countT.GetComponent<TextMeshProUGUI>();
            }

                iconArray.GetArrayElementAtIndex(i).objectReferenceValue = icon;
                countArray.GetArrayElementAtIndex(i).objectReferenceValue = countText;
        }
        serializedHud.FindProperty("minimapCamera").objectReferenceValue = minimapCamera;
        serializedHud.FindProperty("minimapImage").objectReferenceValue = minimapImage;
        serializedHud.FindProperty("use2DMinimap").boolValue = true;
        serializedHud.FindProperty("lowPolyHealthUiMode").boolValue = lowPolyMode;
        serializedHud.FindProperty("bulletCountOnRight").boolValue = true;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(hudController);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = battleUiRoot;

        Debug.Log(lowPolyMode ? "Battle UI lowpoly setup complete in active scene." : "Battle UI setup complete in active scene.");
    }

    static Canvas GetOrCreateCanvas()
    {
        Canvas existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null)
            return existing;

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            Object.DestroyImmediate(legacyModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null)
            return child.gameObject;

        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent.transform, false);
        return gameObject;
    }

    static RectTransform EnsureRectTransform(GameObject gameObject)
    {
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();
        return rectTransform;
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static Slider GetOrCreateSlider(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out Slider existingSlider))
            return existingSlider;

        DefaultControls.Resources resources = new DefaultControls.Resources();
        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.wholeNumbers = false;
        return slider;
    }

    static void ApplyTankHealthBarStyle(Slider slider)
    {
        if (slider == null)
            return;

        slider.transition = Selectable.Transition.None;

        Transform background = slider.transform.Find("Background");
        if (background != null)
        {
            Image backgroundImage = background.GetComponent<Image>();
            if (backgroundImage != null)
                backgroundImage.color = new Color(0.08f, 0.1f, 0.08f, 0.95f);
        }

        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = new Color(0.2f, 0.92f, 0.22f, 1f);
        }

        Transform handleArea = slider.transform.Find("Handle Slide Area");
        if (handleArea != null)
            handleArea.gameObject.SetActive(false);
    }

    static TextMeshProUGUI GetOrCreateTMPText(Transform parent, string name, string initialText, int fontSize, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = null;
        if (existing != null)
            text = existing.GetComponent<TextMeshProUGUI>();

        if (text == null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        text.text = initialText;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    static RawImage GetOrCreateRawImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        RawImage rawImage = null;
        if (existing != null)
            rawImage = existing.GetComponent<RawImage>();

        if (rawImage == null)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            rawImage = imageObject.GetComponent<RawImage>();
            rawImage.color = Color.white;
        }

        return rawImage;
    }

    static RawImage EnsureCircularMinimapRootAndGetView(GameObject minimapRoot)
    {
        RawImage legacyRawImage = minimapRoot.GetComponent<RawImage>();
        Texture legacyTexture = null;
        Color legacyColor = Color.white;

        if (legacyRawImage != null)
        {
            legacyTexture = legacyRawImage.texture;
            legacyColor = legacyRawImage.color;
            Object.DestroyImmediate(legacyRawImage);
        }

        Graphic rootGraphic = minimapRoot.GetComponent<Graphic>();
        if (rootGraphic != null && rootGraphic is not Image)
            Object.DestroyImmediate(rootGraphic);

        RawImage minimapView = GetOrCreateRawImage(minimapRoot.transform, "MinimapView");
        if (legacyTexture != null)
            minimapView.texture = legacyTexture;
        minimapView.color = legacyColor;

        return minimapView;
    }

    static Camera GetOrCreateMinimapCamera()
    {
        GameObject existing = GameObject.Find("MinimapCamera");
        if (existing == null)
            existing = new GameObject("MinimapCamera", typeof(Camera));

        Camera cameraComponent = existing.GetComponent<Camera>();
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = 24f;
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 1f);
        cameraComponent.cullingMask = ~0;
        cameraComponent.nearClipPlane = 0.1f;
        cameraComponent.farClipPlane = 400f;
        cameraComponent.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        return cameraComponent;
    }

    static RenderTexture GetOrCreateMinimapRenderTexture()
    {
        const string assetPath = "Assets/Settings/MinimapRT.renderTexture";
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(assetPath);

        if (renderTexture != null)
            return renderTexture;

        renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = "MinimapRT"
        };

        AssetDatabase.CreateAsset(renderTexture, assetPath);
        AssetDatabase.SaveAssets();
        return renderTexture;
    }

    static Transform FindPlayerTransform()
    {
        Transform namedTank = FindFirstExistingTransform(
            "minitank-v10-green 1",
            "minitank-v10-green",
            "minitank-processed-v3 (1)",
            "minitank-processed-v3 (1) Variant Variant 1");
        if (namedTank != null)
            return namedTank;

        TankController[] tanks = Object.FindObjectsByType<TankController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i] == null)
                continue;

            if (IsPreferredMiniTankName(tanks[i].gameObject.name))
                return tanks[i].transform;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            TankController taggedTank = taggedPlayer.GetComponent<TankController>();
            if (taggedTank == null)
                taggedTank = taggedPlayer.GetComponentInChildren<TankController>(true);

            if (taggedTank != null)
                return taggedTank.transform;
        }

        TankController[] allTanks = Object.FindObjectsByType<TankController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allTanks.Length > 0 && allTanks[0] != null)
            return allTanks[0].transform;

        return null;
    }

    static T ResolveComponentForPlayer<T>(Transform playerTransform) where T : Component
    {
        if (playerTransform != null)
        {
            T component = playerTransform.GetComponent<T>();
            if (component != null)
                return component;

            component = playerTransform.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        T[] sceneComponents = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneComponents.Length; i++)
        {
            if (sceneComponents[i] != null)
                return sceneComponents[i];
        }

        return null;
    }

    static T ResolveSceneComponent<T>() where T : Component
    {
        T[] sceneComponents = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneComponents.Length; i++)
        {
            if (sceneComponents[i] != null)
                return sceneComponents[i];
        }

        return null;
    }

    static void SetupMobileTouchControls(Transform battleUiRoot, TankController tankController, CameraController cameraController, AimController aimController, CannonFiring cannonFiring, PauseMenuController pauseManager)
    {
        if (battleUiRoot == null)
            return;

        Transform existingControls = battleUiRoot.Find("MobileTouchControls");
        if (existingControls != null)
            Object.DestroyImmediate(existingControls.gameObject);

        GameObject controlsRoot = new GameObject("MobileTouchControls", typeof(RectTransform));
        controlsRoot.transform.SetParent(battleUiRoot, false);
        controlsRoot.transform.SetAsLastSibling();

        RectTransform controlsRect = controlsRoot.GetComponent<RectTransform>();
        StretchToParent(controlsRect);

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (uiSprite == null)
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        if (panelSprite == null)
            panelSprite = uiSprite;

        if (uiSprite == null)
            return;

        CreateMovementJoystick(controlsRoot.transform, uiSprite, tankController);
        CreateLookZone(controlsRoot.transform, panelSprite, cameraController, aimController);
        CreateActionButton(controlsRoot.transform, "FireButton", "FIRE", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(176f, 176f), new Color(0.82f, 0.22f, 0.2f, 0.9f), uiSprite, cannonFiring, true);
        CreateActionButton(controlsRoot.transform, "SwitchWeaponButton", "SWAP", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 220f), new Vector2(176f, 82f), new Color(0.17f, 0.54f, 0.88f, 0.88f), uiSprite, tankController, false);
        CreateActionButton(controlsRoot.transform, "DashButton", "DASH", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 220f), new Vector2(140f, 82f), new Color(0.22f, 0.22f, 0.24f, 0.82f), uiSprite, null, false);
        CreatePauseButton(controlsRoot.transform, panelSprite, pauseManager);
    }

    static Transform FindFirstExistingTransform(params string[] objectNames)
    {
        for (int i = 0; i < objectNames.Length; i++)
        {
            GameObject candidate = GameObject.Find(objectNames[i]);
            if (candidate != null)
                return candidate.transform;
        }

        return null;
    }

    static void CreateMovementJoystick(Transform parent, Sprite uiSprite, TankController tankController)
    {
        GameObject root = GetOrCreateChild(parent.gameObject, "MoveJoystick");
        RectTransform rootRect = EnsureRectTransform(root);
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.sizeDelta = new Vector2(360f, 360f);
        rootRect.anchoredPosition = new Vector2(24f, 24f);

        Image background = GetOrCreateImage(root.transform, "Background", uiSprite, new Color(0.08f, 0.09f, 0.1f, 0.34f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.raycastTarget = true;

        Image ring = GetOrCreateImage(root.transform, "Ring", uiSprite, new Color(1f, 1f, 1f, 0.08f));
        RectTransform ringRect = ring.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = new Vector2(280f, 280f);
        ringRect.anchoredPosition = Vector2.zero;
        ring.raycastTarget = false;

        Image handle = GetOrCreateImage(root.transform, "Handle", uiSprite, new Color(1f, 1f, 1f, 0.8f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(104f, 104f);
        handleRect.anchoredPosition = Vector2.zero;
        handle.raycastTarget = false;

        UIVirtualJoystick joystick = root.GetComponent<UIVirtualJoystick>();
        if (joystick == null)
            joystick = root.AddComponent<UIVirtualJoystick>();
        joystick.containerRect = backgroundRect;
        joystick.handleRect = handleRect;
        joystick.joystickRange = 92f;
        joystick.magnitudeMultiplier = 1f;

        if (tankController != null)
        {
            tankController.movementJoystick = joystick;
            EditorUtility.SetDirty(tankController);
            UnityEventTools.AddPersistentListener(joystick.joystickOutputEvent, tankController.SetMoveInput);
        }

        joystick.joystickRange = 104f;

        TextMeshProUGUI label = GetOrCreateTMPText(root.transform, "MoveLabel", "MOVE", 22, TextAlignmentOptions.Top);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(220f, 28f);
        labelRect.anchoredPosition = new Vector2(0f, -12f);
        label.raycastTarget = false;
    }

    static void CreateLookZone(Transform parent, Sprite panelSprite, CameraController cameraController, AimController aimController)
    {
        GameObject root = GetOrCreateChild(parent.gameObject, "LookZone");
        RectTransform rootRect = EnsureRectTransform(root);
        rootRect.anchorMin = new Vector2(0.42f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Image zoneImage = GetOrCreateImage(root.transform, "ZoneImage", panelSprite, new Color(1f, 1f, 1f, 0.03f));
        RectTransform zoneRect = zoneImage.GetComponent<RectTransform>();
        zoneRect.anchorMin = Vector2.zero;
        zoneRect.anchorMax = Vector2.one;
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;
        zoneImage.raycastTarget = true;

        UIVirtualTouchZone touchZone = root.GetComponent<UIVirtualTouchZone>();
        if (touchZone == null)
            touchZone = root.AddComponent<UIVirtualTouchZone>();
        touchZone.containerRect = zoneRect;
        touchZone.handleRect = null;
        touchZone.clampToMagnitude = true;
        touchZone.magnitudeMultiplier = 1f;

        if (cameraController != null)
        {
            UnityEventTools.AddPersistentListener(touchZone.touchZoneOutputEvent, cameraController.SetLookInput);
        }

        if (aimController != null)
        {
            UnityEventTools.AddPersistentListener(touchZone.touchZoneOutputEvent, aimController.SetLookInput);
        }

        TextMeshProUGUI label = GetOrCreateTMPText(root.transform, "LookLabel", "LOOK", 22, TextAlignmentOptions.TopRight);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(1f, 1f);
        labelRect.sizeDelta = new Vector2(160f, 28f);
        labelRect.anchoredPosition = new Vector2(-24f, -12f);
        label.color = new Color(1f, 1f, 1f, 0.75f);
        label.raycastTarget = false;
    }

    static void CreateActionButton(Transform parent, string name, string labelText, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color backgroundColor, Sprite uiSprite, Component target, bool fireButton)
    {
        GameObject root = GetOrCreateChild(parent.gameObject, name);
        RectTransform rootRect = EnsureRectTransform(root);
        rootRect.anchorMin = anchorMin;
        rootRect.anchorMax = anchorMax;
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = sizeDelta;
        rootRect.anchoredPosition = anchoredPosition;

        Image buttonImage = GetOrCreateImage(root.transform, "ButtonImage", uiSprite, backgroundColor);
        RectTransform buttonRect = buttonImage.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        buttonImage.raycastTarget = true;

        UIVirtualButton virtualButton = root.GetComponent<UIVirtualButton>();
        if (virtualButton == null)
            virtualButton = root.AddComponent<UIVirtualButton>();

        if (fireButton && target is CannonFiring cannonFiring)
        {
            UnityEventTools.AddPersistentListener(virtualButton.buttonStateOutputEvent, cannonFiring.SetFireButtonState);
        }
        else if (!fireButton && target is TankController tankController)
        {
            UnityEventTools.AddPersistentListener(virtualButton.buttonStateOutputEvent, tankController.SetTurretCycleButtonState);
        }

        TextMeshProUGUI buttonLabel = GetOrCreateTMPText(root.transform, "ButtonLabel", labelText, 24, TextAlignmentOptions.Center);
        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        buttonLabel.fontStyle = FontStyles.Bold;
        buttonLabel.color = Color.white;
        buttonLabel.raycastTarget = false;
    }

    static void CreatePauseButton(Transform parent, Sprite uiSprite, PauseMenuController pauseManager)
    {
        GameObject root = GetOrCreateChild(parent.gameObject, "PauseButton");
        RectTransform rootRect = EnsureRectTransform(root);
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.sizeDelta = new Vector2(156f, 74f);
        rootRect.anchoredPosition = new Vector2(-24f, -12f);

        Image buttonImage = GetOrCreateImage(root.transform, "ButtonImage", uiSprite, new Color(0.14f, 0.14f, 0.16f, 0.9f));
        RectTransform buttonRect = buttonImage.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        buttonImage.raycastTarget = true;

        UIVirtualButton virtualButton = root.GetComponent<UIVirtualButton>();
        if (virtualButton == null)
            virtualButton = root.AddComponent<UIVirtualButton>();

        if (pauseManager != null)
            UnityEventTools.AddPersistentListener(virtualButton.buttonClickOutputEvent, pauseManager.TogglePause);

        TextMeshProUGUI buttonLabel = GetOrCreateTMPText(root.transform, "ButtonLabel", "PAUSE", 24, TextAlignmentOptions.Center);
        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        buttonLabel.fontStyle = FontStyles.Bold;
        buttonLabel.color = Color.white;
        buttonLabel.raycastTarget = false;
    }

    static Image GetOrCreateImage(Transform parent, string name, Sprite sprite, Color color)
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
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = color;
        return image;
    }

    static bool IsPreferredMiniTankName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        return objectName.ToLowerInvariant().Contains("minitank-processed-v3");
    }

    static void RemoveRadarVisuals(Transform minimapRoot)
    {
        if (minimapRoot == null)
            return;

        for (int i = minimapRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = minimapRoot.GetChild(i);
            string childName = child.name.ToLowerInvariant();

            bool isRadarVisual = childName.Contains("radar") || childName.Contains("sweep") || childName.Contains("ring");
            if (!isRadarVisual)
                continue;

            if (child.name == "PlayerMarker" || child.name == "MinimapView")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    static void RemoveDuplicatePlayerMarkers(Transform minimapRoot, Transform keepMarker)
    {
        if (minimapRoot == null)
            return;

        TextMeshProUGUI[] texts = minimapRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null)
                continue;

            if (text.transform == keepMarker)
                continue;

            bool isMarkerName = text.name.ToLowerInvariant().Contains("playermarker");
            bool isMarkerGlyph = !string.IsNullOrEmpty(text.text) && text.text.Contains("▲");
            if (!isMarkerName && !isMarkerGlyph)
                continue;

            Object.DestroyImmediate(text.gameObject);
        }
    }

    static void EnsureCrosshair(Transform battleUiRoot)
    {
        if (battleUiRoot == null)
            return;

        Transform crosshairTransform = battleUiRoot.Find("Crosshair");

        if (crosshairTransform == null)
        {
            CrosshairScript[] existingCrosshairScripts = Object.FindObjectsByType<CrosshairScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingCrosshairScripts.Length; i++)
            {
                CrosshairScript existingScript = existingCrosshairScripts[i];
                if (existingScript == null)
                    continue;

                if (existingScript.gameObject.scene != battleUiRoot.gameObject.scene)
                    continue;

                crosshairTransform = existingScript.transform;
                break;
            }
        }

        if (crosshairTransform == null)
        {
            Image[] existingImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingImages.Length; i++)
            {
                Image candidateImage = existingImages[i];
                if (candidateImage == null)
                    continue;

                if (!string.Equals(candidateImage.name, "Crosshair", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (candidateImage.gameObject.scene != battleUiRoot.gameObject.scene)
                    continue;

                crosshairTransform = candidateImage.transform;
                break;
            }
        }

        Image crosshairImage = null;

        if (crosshairTransform != null)
        {
            crosshairTransform.SetParent(battleUiRoot, false);
            crosshairImage = crosshairTransform.GetComponent<Image>();
        }

        if (crosshairImage == null)
        {
            GameObject crosshairObject = new GameObject("Crosshair", typeof(RectTransform), typeof(Image), typeof(CrosshairScript));
            crosshairObject.transform.SetParent(battleUiRoot, false);
            crosshairImage = crosshairObject.GetComponent<Image>();
            crosshairTransform = crosshairObject.transform;
        }
        else if (crosshairTransform.GetComponent<CrosshairScript>() == null)
        {
            crosshairTransform.gameObject.AddComponent<CrosshairScript>();
        }

        crosshairTransform.gameObject.name = "Crosshair";
        crosshairTransform.gameObject.SetActive(true);
        crosshairTransform.SetAsLastSibling();

        RectTransform crosshairRect = crosshairTransform as RectTransform;
        if (crosshairRect == null)
            crosshairRect = crosshairTransform.gameObject.AddComponent<RectTransform>();

        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.sizeDelta = new Vector2(52f, 52f);
        crosshairRect.anchoredPosition = Vector2.zero;

        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (crosshairImage.sprite == null && defaultSprite != null)
            crosshairImage.sprite = defaultSprite;

        crosshairImage.type = Image.Type.Simple;
        crosshairImage.preserveAspect = true;
        crosshairImage.color = Color.white;
        crosshairImage.raycastTarget = false;
    }
}
