using UnityEngine;

public class PickupDrops : MonoBehaviour
{
    [HideInInspector] public ItemInstance itemInstance;

    [Header("Visuals")]
    [SerializeField] private MeshRenderer pickupMeshRenderer;

    // Single shared AudioSource on this prefab for playing the clip
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        pickupMeshRenderer ??= GetComponentInChildren<MeshRenderer>();
        audioSource ??= GetComponentInChildren<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (PlayerInventory.Instance != null && itemInstance != null)
            {
                ItemSO so = itemInstance.GetItemSO();
                PlayerInventory.Instance.AddItem(so, itemInstance.quantity);
                Debug.Log($"✅ Picked up: {itemInstance.quantity}x {so.itemName}");

                if (audioSource != null && so.pickupSound != null)
                {
                    audioSource.clip = so.pickupSound;
                    audioSource.Play();
                }
            }

            if (pickupMeshRenderer != null)
                pickupMeshRenderer.enabled = false;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            float delay = (audioSource != null && audioSource.clip != null)
                ? audioSource.clip.length
                : 0f;

            Destroy(gameObject, delay);
        }
    }
}