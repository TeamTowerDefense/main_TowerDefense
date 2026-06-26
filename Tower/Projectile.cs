using System;
using UnityEngine;

public class Projectile : PoolableObject
{
    private Transform target;
    private int damage;
    private ProjectileData projectileData;

    [SerializeField] private Vector3 rotationOffset;

    #region projectile 생성
    public void Initialize(Transform target, int damage, ProjectileData projectileData)
    {
        this.target = target;
        this.damage = damage;
        this.projectileData = projectileData;
    }
    #endregion

    private void Update()
    {
        if(target == null || projectileData == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;

        transform.position += direction * projectileData.projectileSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation * Quaternion.Euler(rotationOffset);
        }


        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= 0.2f)
        {
            HitTarget();
        }

    }

    #region 타겟 적중
    private void HitTarget()
    {
        Vector3 hitPoint = GetHitPoint();

        SpawnHitEffect(hitPoint);

        switch (projectileData.attackType)
        {
            case ProjectileAttackType.Single:
                SingleHit();
                break;

            case ProjectileAttackType.Explosion:
                ExplosionHit();
                break;

            case ProjectileAttackType.Area:
                AreaHit();
                break;
        }

        DespawnSelf();
    }
    #endregion

    #region 히트 Trs 가져오기
    private Vector3 GetHitPoint()
    {
        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider == null)
            targetCollider = target.GetComponentInChildren<Collider>();

        if (targetCollider == null)
            return transform.position;

        return targetCollider.ClosestPoint(transform.position);
    }
    #endregion
   
    #region 단일 공격
    private void SingleHit()
    {
        Monster monster = target.GetComponent<Monster>();

        if (monster != null)
            monster.TakeDamage(damage);
    }
    #endregion

    #region 광역 공격
    private void ExplosionHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, projectileData.explosionRadius, projectileData.targetLayer);

        foreach (Collider hit in hits)
        {
            Monster monster = hit.GetComponentInParent<Monster>();

            if (monster != null)
                monster.TakeDamage(damage);
        }
    }
    #endregion

    #region 광역 탄
    private void AreaHit()
    {
        ExplosionHit();
    }
    #endregion

    #region 히트 이펙트
    private void SpawnHitEffect(Vector3 hitPoint)
    {
        if (projectileData.hitEffectPF == null)
            return;

        
        PoolEffect effect = ObjectPoolManager.Instance.Spawn<PoolEffect>(
            projectileData.hitEffectPF,
            transform.position,
            Quaternion.identity,
            ObjectPoolManager.Instance.GetEffectParent()
        );

        if (effect != null)
            effect.Play();
    }
    #endregion

    #region 디스폰
    private void DespawnSelf()
    {
        ObjectPoolManager.Instance.Despawn(this);
    }
    #endregion
}
