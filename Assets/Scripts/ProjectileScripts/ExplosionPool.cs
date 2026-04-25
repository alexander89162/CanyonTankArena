using UnityEngine;
using UnityEngine.Pool;
using System.Linq;

public class ExplosionPool : MonoBehaviour
{
    public static ExplosionPool Instance { get; private set; }

    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int defaultCapacity = 16;
    [SerializeField] private int maxSize = 128;

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        Instance = this;
        pool = new ObjectPool<GameObject>(
            createFunc:    () => Instantiate(explosionPrefab),
            actionOnGet:   obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    public void Spawn(Vector3 position, Quaternion rotation, float scale = 1f)
    {
        GameObject obj = pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.localScale = Vector3.one * scale;
        
        foreach (ParticleSystem ps in obj.GetComponentsInChildren<ParticleSystem>())
            ps.Play();

        StartCoroutine(ReturnWhenFinished(obj));
    }

    private System.Collections.IEnumerator ReturnWhenFinished(GameObject obj)
    {
        ParticleSystem[] systems = obj.GetComponentsInChildren<ParticleSystem>();
        yield return new WaitUntil(() => systems.All(ps => !ps.IsAlive(true)));
        pool.Release(obj);
    }
}