using UnityEngine;
using PrimeTween;

/*Aimer implementation for minigun.
NOTE: this aimer has rig issues so offsets are hardcoded into the angle math*/
public class MinigunAimer : WeaponAimer
{
    [SerializeField] private Transform minigunBase;
    [SerializeField] private Transform minigunBody;
    [SerializeField] private Transform minigunBarrels;
    [SerializeField] private Transform barrelEnd;
    [SerializeField] private float lowerTarget = 4f;
    public Vector3 bulletSpawnOffset = new Vector3(0.5f, 0f, 3f);
    public float bulletDamage = 12f;
    public float projectileSpeed = 350f;
    public float bulletMaxLifetime = 2f;
    public float barrelsSpinSpeed = 800f;
    public float delayBeforeFire = 2.2f;
    public float cooldownMultiplier = 2f;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sound")]
    [SerializeField] private AudioSource minigunAudioSource;
    [SerializeField] private AudioClip revUpClip;    // plays once when spinning starts
    [SerializeField] private AudioClip firingClip;   // loops while actually firing
    [SerializeField] private AudioClip spinDownClip; // plays once when spinning stops

    private Quaternion baseRestRotation;
    private Quaternion bodyRestRotation;
    private float spinningTime = 0f;
    private bool firedLastFrame = false;
    private bool weaponLocked = false;

    private enum SoundState { Silent, RevingUp, Firing, SpinningDown }
    private SoundState soundState = SoundState.Silent;

    protected override void Awake()
    {
        base.Awake();
        baseRestRotation = minigunBase.localRotation;
        bodyRestRotation = minigunBody.localRotation;

        if (gameObject.CompareTag("Player") && PlayerTankStats.Instance != null && PlayerTankStats.Instance.minigunDamageMultiplier != 0f)
        {
            bulletDamage += bulletDamage * PlayerTankStats.Instance.minigunDamageMultiplier;
        }
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;
        pitch += lowerTarget;

        minigunBase.localRotation = baseRestRotation * Quaternion.Euler(0, yaw, 0);
        minigunBody.localRotation = bodyRestRotation * Quaternion.Euler(pitch, 0, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (currentAmmo > 0) firedLastFrame = true;
        else
        {
            ReloadWeapon();
            return;
        }

        if (fireTimer > 0 || spinningTime < delayBeforeFire || weaponLocked) return;
        fireTimer = fireCooldown;
        currentAmmo--;

        Vector3 dir = (targetPosition - barrelEnd.position).normalized;

        Bullet b = new Bullet
        {
            position = barrelEnd.position + barrelEnd.TransformDirection(bulletSpawnOffset),
            velocity = dir * projectileSpeed,
            damage = bulletDamage,
            type = 1,
            remainingLifetime = bulletMaxLifetime,
            owner = gameObject
        };

        ProjectileManager.Instance.SpawnBullet(b);
    }

    public override void ReloadWeapon()
    {
        if (weaponLocked == true) return;

        weaponLocked = true;
        Tween.Delay(target: this, reloadTime, () =>
        {
            currentAmmo = maxAmmo;
            weaponLocked = false;
        });
    }

    public override void OnWeaponSwapped()
    {
        fireTimer = 0.5f;
        spinningTime = 0f;
        GoSilent();
    }

    public override void DoWhileHolding()
    {
        float dt = Time.deltaTime;
        bool isFullySpunUp = spinningTime >= delayBeforeFire;

        if (!firedLastFrame)
            spinningTime -= dt * cooldownMultiplier;
        else
            spinningTime += dt;

        spinningTime = Mathf.Clamp(spinningTime, 0f, delayBeforeFire);

        UpdateSoundState(isFullySpunUp);

        float t = spinningTime / delayBeforeFire;
        float easedT = spinCurve.Evaluate(t);
        minigunBarrels.localRotation *= Quaternion.Euler(0, easedT * barrelsSpinSpeed * dt, 0);

        firedLastFrame = false;
    }

    private void UpdateSoundState(bool wasFullySpunUp)
    {
        bool spinning = spinningTime > 0f;
        bool fullySpunUp = spinningTime >= delayBeforeFire;

        switch (soundState)
        {
            case SoundState.Silent:
                if (spinning)
                    PlayRevUp();
                break;

            case SoundState.RevingUp:
                // Rev-up clip finished or barrels are fully spun — switch to firing loop
                if (fullySpunUp || !minigunAudioSource.isPlaying)
                    PlayFiring();
                // Player released before fully spun up
                else if (!spinning)
                    PlaySpinDown();
                break;

            case SoundState.Firing:
                if (!spinning)
                    PlaySpinDown();
                break;

            case SoundState.SpinningDown:
                // Spun up again while winding down
                if (spinning && !wasFullySpunUp)
                    PlayRevUp();
                else if (spinning && wasFullySpunUp)
                    PlayFiring();
                // Spin-down clip finished naturally
                else if (!minigunAudioSource.isPlaying)
                    GoSilent();
                break;
        }
    }

    private void PlayRevUp()
    {
        if (minigunAudioSource == null || revUpClip == null) return;
        soundState = SoundState.RevingUp;
        minigunAudioSource.loop = false;
        minigunAudioSource.clip = revUpClip;
        minigunAudioSource.Play();
    }

    private void PlayFiring()
    {
        if (minigunAudioSource == null || firingClip == null) return;
        soundState = SoundState.Firing;
        minigunAudioSource.loop = true;
        minigunAudioSource.clip = firingClip;
        minigunAudioSource.Play();
    }

    private void PlaySpinDown()
    {
        if (minigunAudioSource == null || spinDownClip == null) return;
        soundState = SoundState.SpinningDown;
        minigunAudioSource.loop = false;
        minigunAudioSource.clip = spinDownClip;
        minigunAudioSource.Play();
    }

    private void GoSilent()
    {
        if (minigunAudioSource != null)
            minigunAudioSource.Stop();
        soundState = SoundState.Silent;
    }

    public override bool ChoiceToFire(float lastFiringTime)
    {
        return true;
    }
}