using UnityEngine;

public enum ProjectileAttackType
{
    Single,
    Explosion,
    Area
}

[CreateAssetMenu(menuName = "Attack/Projectile")]
public class ProjectileData : ScriptableObject
{
    public GameObject projectilePF;

    public ProjectileAttackType attackType;

    public float projectileSpeed = 10f;
    public float explosionRadius = 1.5f;
    public float stun = 0f;
    public LayerMask targetLayer;

    public GameObject hitEffectPF;

}
