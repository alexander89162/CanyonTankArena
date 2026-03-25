using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CannonFiring : MonoBehaviour
{
    [Header("References — usually same as AimController")]
    [SerializeField] private Transform cannonTransform;     // barrel / muzzle pivot
    [SerializeField] private Transform muzzlePoint;         // empty child at barrel tip

    [Header("Projectile")]
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private float fireCooldown = 1.8f;     // sec between shots
    [SerializeField] private float muzzleVelocity = 60f;    // passed to projectile

    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;

    public AudioClip fireSound;
    public float volume = 1.0f;

    private float nextFireTime;

    private void OnEnable()
    {
        if (fireAction?.action != null)
        {
            fireAction.action.performed += ctx => TryFire();
            fireAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction?.action != null)
        {
            fireAction.action.performed -= ctx => TryFire();
            fireAction.action.Disable();
        }
    }

    private void TryFire()
    {
        
        if (Time.time < nextFireTime) return;
        if (shellPrefab == null || muzzlePoint == null) return;

        nextFireTime = Time.time + fireCooldown;
        AudioSource.PlayClipAtPoint(fireSound, Camera.main.transform.position, volume);

        MovementController movement = GetComponentInParent<MovementController>();
        Vector3 tankVelocity = movement != null ? movement.currentVelocityV : Vector3.zero;

        GameObject shell = Instantiate(shellPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Give it initial speed direction (no rigidbody — script handles movement)
        SimpleProjectile proj = shell.GetComponent<SimpleProjectile>();
        if (proj != null)
        {
            // proj.speed = muzzleVelocity;
            proj.initialVelocity = muzzlePoint.forward * muzzleVelocity + tankVelocity;
        }

        // Muzzle flash / sound
        // muzzleFlash.Play();
        // AudioSource.PlayClipAtPoint(fireSound, muzzlePoint.position);
        
        
    }
}