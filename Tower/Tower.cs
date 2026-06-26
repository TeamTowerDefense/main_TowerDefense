using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tower Data")]
    [SerializeField] private TowerData towerData;

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Projectile Data")]
    [SerializeField] private ProjectileData projectileData;
    

    [Header("Target")]
    [SerializeField] private LayerMask monsterLayer;

    [Header("Rotation")]
    [SerializeField] private Transform rotateBody;
    [SerializeField] private float rotateSpeed = 10f;

    private float attackTimer;

    private void Update()
    {
        //Debug.Log($"{name} Tower Update 실행");


        if (towerData == null || towerData.projectileData == null)
            return;

        attackTimer += Time.deltaTime;

        Transform target = FindTarget();

        if (target == null)
        {
            //Debug.Log("타겟 없음");
            return;
        }
        //Debug.Log($"타겟 발견 : {target.name}");
        RotateToTarget(target);

        if(attackTimer >= towerData.attackInterval)
        {
            attackTimer = 0;
            Shoot(target);
        }

    }

    #region 타켓 추적 메서드
    private Transform FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere
            (transform.position, towerData.attackRange, towerData.monsterLayer);

        if (hits.Length == 0)
            return null;

        Transform closetTarget = null;
        float closestDistance = float.MaxValue;

        foreach(Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closetTarget = hit.transform;
            }
        }

        return closetTarget;
    }
    #endregion

    #region 발사 메서드
    private void Shoot(Transform target)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("ObjectPoolManager.Instance가 null입니다. 씬에 ObjectPoolManager 오브젝트가 없습니다.");
            return;
        }

        if(firePoint == null)
            return;

        GameObject prefab = towerData.projectileData.projectilePF;

        if (prefab == null)
            return;
        //Debug.Log($"Spawn 시도 : {prefab.name}");
        Projectile projectile = ObjectPoolManager.Instance.Spawn<Projectile>
            (prefab, firePoint.position, firePoint.rotation, ObjectPoolManager.Instance.GetProjectileParent());

        if (projectile == null)
            return;

        projectile.Initialize(
            target,
            towerData.damage,
            towerData.projectileData
        );

        if(projectile == null)
        {
            Debug.LogError($"{prefab.name}에 Projectile 컴포넌트가 없음");
            return;
        }

        projectile.Initialize(target, towerData.damage, towerData.projectileData);
    }
    #endregion

    #region 타겟 추적 메서드
    private void RotateToTarget(Transform target)
    {
        Transform body = rotateBody != null ? rotateBody : transform;

        Vector3 dir = target.position - body.position;
        dir.y = 0f;

        if (dir == Vector3.zero)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);

        body.rotation = Quaternion.Slerp(body.rotation, lookRotation, Time.deltaTime * rotateSpeed);
    }
    #endregion

    #region 범위 표시 Gizmos
    private void OnDrawGizmosSelected()
    {
        if (towerData == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, towerData.attackRange);
    }
    #endregion
}
