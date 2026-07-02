using UnityEngine;
using UnityEngine.AddressableAssets;
public enum ProjectileAttackType
{
    Single,
    Explosion,
    Area
}

[CreateAssetMenu(menuName = "Attack/Projectile")]
public class ProjectileData : ScriptableObject
{
    [Header("Addressable")]
    public string label;

    public ProjectileAttackType attackType;
    public int projectileID;
    public int hitEffectID;

    public float projectileSpeed = 10f;
    public float explosionRadius = 1.5f;
    public float stun = 0f;
    public LayerMask targetLayer;


}
