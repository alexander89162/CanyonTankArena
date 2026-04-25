using UnityEngine;

public struct Bullet
{
    public Vector3 position;
    public Vector3 velocity;

    public float remainingLifetime;
    public float damage;

    public byte type;
    public GameObject owner;
}