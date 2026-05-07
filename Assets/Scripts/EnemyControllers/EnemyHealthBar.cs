using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    [Header("Positioning")]
    [SerializeField] private float heightOffset = 2.8f;

    private HealthComponent linkedHealth;
    private Transform camTransform;
    private CanvasGroup canvasGroup;

    public void Initialize(HealthComponent health)
    {
        linkedHealth = health;

        // Ensure CanvasGroup exists
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Make sure it's visible
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;

        if (health != null)
        {
            health.OnHealthChanged.AddListener(UpdateHealth);
            UpdateHealth(health.HealthNormalized);
        }

        camTransform = Camera.main != null ? Camera.main.transform : FindFirstObjectByType<Camera>()?.transform;
    }

    private void UpdateHealth(float normalized)
    {
        if (healthSlider != null)
            healthSlider.value = normalized;
    }

    void LateUpdate()
    {
        if (linkedHealth == null || linkedHealth.IsDead)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return;
        }

        // Position above enemy
        transform.position = transform.parent.position + Vector3.up * heightOffset;

        // Face camera
        if (camTransform != null)
        {
            transform.LookAt(camTransform.position);
            transform.Rotate(0, 180f, 0);
        }

        canvasGroup.alpha = 1f;   // Keep visible while alive
    }

    private void OnDestroy()
    {
        if (linkedHealth != null)
            linkedHealth.OnHealthChanged.RemoveListener(UpdateHealth);
    }
}