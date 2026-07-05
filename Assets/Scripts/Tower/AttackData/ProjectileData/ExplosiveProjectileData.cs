using UnityEngine;

[CreateAssetMenu(menuName = "Projectile/Explosion")]
public class ExplosiveProjectileData : ProjectileData
{
    [Header("폭발 정보")]
    [SerializeField]
    private float explosionRadius;
    public float ExplosionRadius => explosionRadius;


    // 폭발 데미지 계수 
    //[SerializeField]
    //private float explosiveDamageMultiply;
    //public float ExplosiveDamageMultiply => explosiveDamageMultiply;
}
