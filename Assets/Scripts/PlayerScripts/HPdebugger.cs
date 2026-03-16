using UnityEngine;

public class HPdebugger : MonoBehaviour
{
    [SerializeField] private DamageController damageReceiver;
    [SerializeField] private float heightOffset = 2.5f;
    [SerializeField] private GUIStyle style;

    private HealthComponent health;

    private void Awake()
    {
        damageReceiver ??= GetComponent<DamageController>();
        health = damageReceiver?.GetComponent<HealthComponent>();

        // Basic style setup if not assigned in inspector
        if (style == null)
        {
            style = new GUIStyle
            {
                fontSize = 18,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };
        }
    }

    private void OnGUI()
    {
        if (health == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * heightOffset);
        if (screenPos.z < 0) return;

        float hp = health.CurrentHealth;
        float maxHp = health.MaxHealth;
        string hpText = $"HP: {hp}/{maxHp}";

        GUI.Label(
            new Rect(screenPos.x - 80, Screen.height - screenPos.y - 30, 160, 60),
            hpText,
            style
        );
    }
}