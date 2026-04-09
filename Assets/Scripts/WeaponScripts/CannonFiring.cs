using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System;

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
    private Action<InputAction.CallbackContext> firePerformedHandler;

    private void OnEnable()
    {
        if (fireAction?.action != null)
        {
            if (firePerformedHandler == null)
                firePerformedHandler = OnFirePerformed;
            fireAction.action.performed += firePerformedHandler;
            fireAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction?.action != null)
        {
            if (firePerformedHandler != null)
                fireAction.action.performed -= firePerformedHandler;
            fireAction.action.Disable();
        }
    }

    public void RequestFire()
    {
        TryFire();
    }

    public void SetFireButtonState(bool isPressed)
    {
        if (isPressed)
            TryFire();
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        TryFire();
    }

    private void TryFire()
    {
        
        if (Time.time < nextFireTime) return;
        if (shellPrefab == null || muzzlePoint == null) return;

        nextFireTime = Time.time + fireCooldown;
        Camera audioCamera = Camera.main;
        if (fireSound != null && audioCamera != null)
            AudioSource.PlayClipAtPoint(fireSound, audioCamera.transform.position, volume);

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