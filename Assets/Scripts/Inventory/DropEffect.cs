using UnityEngine;
using System.Collections;

public class DropEffect : MonoBehaviour
{
    [Header("Pop Settings")]
    [SerializeField] private float popHeight = 2.5f;
    [SerializeField] private float popDuration = 0.6f;
    [SerializeField] private float randomSideways = 1.2f;

    [Header("Rotation")]
    [SerializeField] private bool rotateWhilePopping = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 180, 0);

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 50f;
    [SerializeField] private float groundOffset = 0.1f; 

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsedTime = 0f;
    private bool isPopping = false;

    public void PopOut()
    {
        startPosition = transform.position;

        // Random horizontal scatter
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomSideways, randomSideways),
            0,
            Random.Range(-randomSideways, randomSideways)
        );

        targetPosition = startPosition + randomOffset;

        Vector3 rayOrigin = new Vector3(targetPosition.x, startPosition.y + 1f, targetPosition.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundLayer))
        {
            targetPosition.y = hit.point.y + groundOffset;
        }
        else
        {
            targetPosition.y = startPosition.y;
        }

        elapsedTime = 0f;
        isPopping = true;
        StopAllCoroutines();
        StartCoroutine(PopCoroutine());
    }

    private IEnumerator PopCoroutine()
    {
        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / popDuration);

            float tEased = 1 - Mathf.Pow(1 - t, 3);

            float height = Mathf.Sin(t * Mathf.PI) * popHeight;

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, tEased);
            newPos.y = Mathf.Lerp(startPosition.y, targetPosition.y, tEased) + height;
            transform.position = newPos;

            if (rotateWhilePopping)
                transform.Rotate(rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = targetPosition;
        isPopping = false;

        StartCoroutine(GentleBob());
    }

    private IEnumerator GentleBob()
    {
        float bobTime = 0f;
        Vector3 basePos = transform.position;

        while (true)
        {
            bobTime += Time.deltaTime * 2f;
            float bobOffset = Mathf.Sin(bobTime) * 0.08f;
            transform.position = basePos + Vector3.up * bobOffset;
            yield return null;
        }
    }
}