using UnityEngine;

public abstract class Projectile : PoolableObject
{
    private ProjectileData projectileData;
    protected int damage;
    protected float attackSpeed;
    protected Transform target;

    [SerializeField] private Vector3 rotationOffset;

    #region projectile 생성
    public virtual void Initialize(Transform target, int damage, ProjectileData projectileData)
    {
        this.target = target;
        this.damage = damage;
        this.projectileData = projectileData;

        //Debug.Log(
        //      $"[Projectile Init] Data={projectileData.name}, " +
        //      $"HitEffectID={projectileData.hitEffectID}"
        //);
    }
    #endregion

    private void Update()
    {
        if (target == null || projectileData == null)
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

        OnHit();

        DespawnSelf();
    }

    protected abstract void OnHit();
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

    #region 히트 이펙트
    private void SpawnHitEffect(Vector3 hitPoint)
    {
        int effectID = projectileData.hitEffectID;

        //Debug.Log($"[HitEffect] Spawn 요청 ID={effectID}");

        if (effectID <= 0)
        {
            //Debug.LogError("[HitEffect] effectID가 0 이하");
            return;
        }

        GameObject prefab = ObjectPoolManager.Instance.GetEffect(projectileData.hitEffectID);


        if (prefab == null)
        {
            //Debug.LogError($"HitEffect 없음 ID : {projectileData.hitEffectID}");
            return;
        }


        PoolEffect effect = ObjectPoolManager.Instance.Spawn<PoolEffect>(
            prefab,
            hitPoint,
            Quaternion.identity,
            ObjectPoolManager.Instance.GetEffectParent()
        );

        //Debug.Log($"[HitEffect] Spawn 결과 = {(effect == null ? "NULL" : effect.name)}");

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
