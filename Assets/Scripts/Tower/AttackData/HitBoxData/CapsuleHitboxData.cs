using UnityEngine;

[CreateAssetMenu(menuName = "Attack/Hitbox/CapsuleHitBox")]
public class CapsuleHitboxData : HitBoxData
{
    [Header("Capsule")]
    public float capsuleRadius = 1f;
    public float capsuleHeight = 1f;
    // 0 = X, 1 = Y, 2 = Z
    public int capsuleDirection = 0;
}
