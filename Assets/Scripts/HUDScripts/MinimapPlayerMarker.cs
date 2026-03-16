using UnityEngine;

public class MinimapPlayerMarker : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float yawOffset = 0f;

    private RectTransform markerRect;

    void Awake()
    {
        markerRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (target == null || markerRect == null)
            return;

        float zRotation = -target.eulerAngles.y + yawOffset;
        markerRect.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }
}
