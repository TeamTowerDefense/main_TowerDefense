using UnityEngine;

[CreateAssetMenu(menuName = "Attack/Hitbox/BoxHitbox")]
public class BoxHitboxData : HitBoxData
{
    [Header("Box")]
    public Vector3 boxSize = new Vector3(1.5f, 1.5f, 6f);
}
