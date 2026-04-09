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

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsedTime = 0f;

    public void PopOut()
    {
        startPosition = transform.position;
        
        // Calculate random landing position
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomSideways, randomSideways),
            0,
            Random.Range(-randomSideways, randomSideways)
        );

        targetPosition = startPosition + randomOffset;
        targetPosition.y = startPosition.y; // land at same height

        elapsedTime = 0f;
        StartCoroutine(PopCoroutine());
    }

    private IEnumerator PopCoroutine()
    {
        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / popDuration;

            // Smooth ease out
            t = 1 - Mathf.Pow(1 - t, 3);

            // Parabolic arc (goes up then down)
            float height = Mathf.Sin(t * Mathf.PI) * popHeight;

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);
            newPos.y += height;

            transform.position = newPos;

            // Optional rotation
            if (rotateWhilePopping)
                transform.Rotate(rotationSpeed * Time.deltaTime);

            yield return null;
        }

        // Final position
        transform.position = targetPosition;

        // Optional gentle bob after landing
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