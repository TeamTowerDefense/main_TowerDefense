using UnityEngine;

[CreateAssetMenu(menuName = "Projectile/Explosion")]
public class ExplosiveProjectileData : ProjectileData
{
    [Header("폭발 정보")]
    [SerializeField]
    private float explosionRadius;
    public float ExplosionRadius => explosionRadius;

    [Header("범위 내 몬스터 Hit Effect")]
    public float targetHitEffectYOffset = 0.5f;
    public EffectData targetHitEffectData;      // 맞은 몬스터별 이펙트


    [Header("이펙트 생명주기")]
    public float targetHitEffectLifetime = 1f;
    // 폭발 데미지 계수 
    //[SerializeField]
    //private float explosiveDamageMultiply;
    //public float ExplosiveDamageMultiply => explosiveDamageMultiply;
}
