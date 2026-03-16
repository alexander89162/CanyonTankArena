using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 35f, 0f);
    [SerializeField] private bool rotateWithTarget = true;
    [SerializeField] private bool prioritizePlayerTagTarget = true;
    [SerializeField, Range(0.1f, 1f)] private float targetResolveInterval = 0.25f;

    private float nextTargetResolveTime;

    void Awake()
    {
        rotateWithTarget = true;
        TryResolveTargetFromPlayerTag();
    }

    void LateUpdate()
    {
        if (prioritizePlayerTagTarget)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                TryResolveTargetFromPlayerTag();
        }
        else if (target == null)
            TryResolveTargetFromPlayerTag();

        if (target == null)
            return;

        transform.position = target.position + offset;

        if (rotateWithTarget)
        {
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        }
    }

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    public void SetRotateWithTarget(bool shouldRotate)
    {
        rotateWithTarget = shouldRotate;
    }

    void TryResolveTargetFromPlayerTag()
    {
        if (Time.unscaledTime < nextTargetResolveTime)
            return;

        nextTargetResolveTime = Time.unscaledTime + targetResolveInterval;

        Transform taggedPlayer = TryGetPlayerTagTransform();
        if (taggedPlayer != null)
            target = taggedPlayer;
    }

    static Transform TryGetPlayerTagTransform()
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

        if (taggedPlayer == null)
            return null;

        return taggedPlayer.transform;
    }
}
