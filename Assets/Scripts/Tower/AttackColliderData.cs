using UnityEngine;

public enum AttackRangeShape
{
    Box,
    Sphere,
    Capsule
}

public class AttackColliderData : MonoBehaviour
{
    public AttackRangeShape shape;

    public Vector3 center;

    // Box
    public Vector3 size;

    // Sphere
    public float radius;

    // Capsule
    public float capsuleRadius;
    public float height;
    public int direction;
}
