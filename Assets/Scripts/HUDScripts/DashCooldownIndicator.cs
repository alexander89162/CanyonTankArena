using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCooldownIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankController tankController;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Style")]
    [SerializeField] private Color readyColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color cooldownColor = new Color(1f, 1f, 1f, 0.56f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.82f, 1f, 0.82f);
    [SerializeField] private bool showNumericCooldown = true;

    private const float ResolveInterval = 0.25f;
    private float nextResolveTime;

    void Awake()
    {
        ResolveReferences();
        RefreshVisual();
    }

    void Update()
    {
        if (Time.unscaledTime >= nextResolveTime)
        {
            if (tankController == null)
                tankController = ResolveTankController();

            nextResolveTime = Time.unscaledTime + ResolveInterval;
        }

        RefreshVisual();
    }

    public void SetTankController(TankController controller)
    {
        tankController = controller;
    }

    public void SetVisualReferences(Image icon, Image fill, TMP_Text text)
    {
        iconImage = icon;
        cooldownFillImage = fill;
        cooldownText = text;
        RefreshVisual();
    }

    void ResolveReferences()
    {
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (cooldownFillImage == null)
        {
            Transform fillTransform = transform.Find("CooldownFill");
            if (fillTransform != null)
                cooldownFillImage = fillTransform.GetComponent<Image>();
        }

        if (cooldownText == null)
        {
            Transform textTransform = transform.Find("CooldownText");
            if (textTransform != null)
                cooldownText = textTransform.GetComponent<TMP_Text>();
        }

        if (tankController == null)
            tankController = ResolveTankController();

        if (cooldownFillImage != null)
        {
            cooldownFillImage.type = Image.Type.Filled;
            cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownFillImage.fillClockwise = false;
            cooldownFillImage.color = fillColor;
            cooldownFillImage.raycastTarget = false;
        }

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (cooldownText != null)
            cooldownText.raycastTarget = false;
    }

    TankController ResolveTankController()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            TankController taggedTank = taggedPlayer.GetComponent<TankController>();
            if (taggedTank == null)
                taggedTank = taggedPlayer.GetComponentInChildren<TankController>(true);

            if (taggedTank != null)
                return taggedTank;
        }

        TankController[] tanks = Object.FindObjectsByType<TankController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (tanks.Length > 0)
            return tanks[0];

        return null;
    }

    void RefreshVisual()
    {
        float remaining = tankController != null ? tankController.DashCooldownRemaining : 0f;
        float normalized = tankController != null ? tankController.DashCooldownNormalized : 0f;
        bool isCoolingDown = normalized > 0.0001f;

        if (iconImage != null)
            iconImage.color = isCoolingDown ? cooldownColor : readyColor;

        if (cooldownFillImage != null)
        {
            cooldownFillImage.color = fillColor;
            cooldownFillImage.fillAmount = normalized;
            cooldownFillImage.enabled = isCoolingDown;
        }

        if (cooldownText != null)
        {
            if (!showNumericCooldown || !isCoolingDown)
            {
                cooldownText.text = string.Empty;
            }
            else
            {
                cooldownText.text = Mathf.CeilToInt(remaining).ToString();
            }
        }
    }
}
