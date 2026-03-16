using UnityEngine;

public class MinimapEnemyMarker : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform playerReference;
    [SerializeField] private MinimapRadarOverlay radarOverlay;
    [SerializeField] private float edgePadding = 18f;
    [SerializeField] private float yawOffset = 0f;
    [SerializeField] private bool hideWhenOutOfRange = false;

    private RectTransform markerRect;

    void Awake()
    {
        markerRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (markerRect == null)
            return;

        if (target == null || playerReference == null || radarOverlay == null)
        {
            markerRect.gameObject.SetActive(false);
            return;
        }

        if (!radarOverlay.TryWorldToMinimapAnchoredPosition(target.position, playerReference, edgePadding, out Vector2 anchoredPosition, out bool isInRange))
        {
            markerRect.gameObject.SetActive(false);
            return;
        }

        if (!isInRange && hideWhenOutOfRange)
        {
            markerRect.gameObject.SetActive(false);
            return;
        }

        markerRect.gameObject.SetActive(true);
        markerRect.anchoredPosition = anchoredPosition;

        float zRotation = -target.eulerAngles.y + yawOffset;
        markerRect.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    public void SetTarget(Transform enemyTarget)
    {
        target = enemyTarget;
    }

    public void SetPlayerReference(Transform playerTarget)
    {
        playerReference = playerTarget;
    }

    public void SetRadarOverlay(MinimapRadarOverlay overlay)
    {
        radarOverlay = overlay;
    }
}
