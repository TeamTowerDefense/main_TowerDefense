
using IGameInterface;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Tower : BuildingBase
{
    private TowerData towerData => buildingData as TowerData;  

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Projectile Data")]
    [SerializeField] private ProjectileData projectileData;
    [SerializeField] private HitBoxData hitBoxAttackData;

    [Header("Target")]
    [SerializeField] private LayerMask monsterLayer;

    [Header("Rotation")]
    [SerializeField] private Transform rotateBody;
    [SerializeField] private float rotateSpeed = 10f;



    private float attackTimer;
    private bool isAttacking;
    private ITowerTargetFinder targetFinder;

    private void Awake()
    {
        if (towerData == null)
        {
            Debug.LogError($"{name} : TowerData 없음");
            enabled = false;
            return;
        }

        switch (towerData.attackMode)
        {
            case TowerAttackMode.Projectile:

                if (towerData.projectileData == null)
                {
                    Debug.LogError($"{name} : ProjectileData 없음");
                    enabled = false;
                }

                break;

            case TowerAttackMode.HitBox:

                if (towerData.hitBoxAttackData == null)
                {
                    Debug.LogError($"{name} : HitBoxAttackData 없음");
                    enabled = false;
                }

                break;
        }

        targetFinder = GetComponent<ITowerTargetFinder>();
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        Transform target = FindTarget();

        if (target == null)
            return;

        RotateToTarget(target);

        float finalAttackInterval =
        towerData.attackInterval / Mathf.Max(0.01f, towerData.attackSpeed);

        if (attackTimer >= finalAttackInterval)
        {
            attackTimer = 0f;
            Attack(target);
        }

    }

    #region 타켓 추적 메서드
    /*
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
    }*/
    // 임시 변경
    private Transform FindTarget()
    {
        if (targetFinder != null &&
            targetFinder.TryGetTarget(transform.position, towerData.attackRange, out EnemyInfo enemy))
            return enemy.Transform;

        return null;
    }
    #endregion

    #region 공격 메서드
    private void Attack(Transform target)
    {
        switch (towerData.attackMode)
        {
            case TowerAttackMode.Projectile:
                ShootProjectile(target);
                break;

            case TowerAttackMode.HitBox:
                if (!isAttacking)
                    StartCoroutine(UseHitBoxAttack(target));
                break;
        }

    }
    #endregion

 

    #region 투사체 발사
    private async Task ShootProjectile(Transform target)
    {

        if (ObjectPoolManager.Instance == null)
        {
            //Debug.LogError("ObjectPoolManager.Instance가 null입니다. 씬에 ObjectPoolManager 오브젝트가 없습니다.");
            return;
        }

        if (firePoint == null)
            return;

        ProjectileData data = towerData.projectileData;

        GameObject prefab = ObjectPoolManager.Instance.GetProjectile(
            towerData.projectileData.projectileID
            );

        //Debug.Log($"[Projectile] 로드 결과 = {(prefab == null ? "NULL" : prefab.name)}");

        if (prefab == null)
            return;
        //Debug.Log($"Spawn 시도 : {prefab.name}");
        Projectile projectile = ObjectPoolManager.Instance.Spawn<Projectile>
            (prefab, firePoint.position, firePoint.rotation, ObjectPoolManager.Instance.GetProjectileParent());

        if (projectile == null)
        {
            //Debug.LogError($"{prefab.name}에 Projectile 컴포넌트가 없음");
            return;
        }
        projectile.Initialize(target, towerData.damage, towerData.projectileData);

    }
    #endregion

    #region 히트박사 발사
    private IEnumerator UseHitBoxAttack(Transform target)
    {
        if (ObjectPoolManager.Instance == null)
        {
            //Debug.LogError("ObjectPoolManager.Instance가 null입니다.");
            yield break;
        }

        if (towerData.hitBoxAttackData == null)
        {
            //Debug.LogError($"{name} : hitBoxAttackData 없음");
            yield break;
        }

        isAttacking = true;
        HitBoxData data = towerData.hitBoxAttackData;

        GameObject prefab = ObjectPoolManager.Instance.GetHitBox(
             towerData.hitBoxAttackData.hitBoxID
        );

        //Debug.Log($"[HitBox] 로드 결과 = {(prefab == null ? "NULL" : prefab.name)}");

        if (prefab == null)
        {
            //Debug.LogError($"{name} : hitBoxPrefab 없음");
            yield break;
        }

        AreaHitBox hitBox = ObjectPoolManager.Instance.Spawn<AreaHitBox>(
            prefab,
            firePoint.position,
            firePoint.rotation,
            ObjectPoolManager.Instance.GetEffectParent()
            );

        if (hitBox == null)
        {
            //Debug.LogError($"{prefab.name} : AreaHitBox Spawn 실패");
            yield break;
        }

        hitBox.transform.SetParent(firePoint);
        hitBox.transform.localPosition = Vector3.zero;
        hitBox.transform.localRotation = Quaternion.identity;


        hitBox.Initialize(
            target,
            towerData.damage,
            towerData.monsterLayer,
            towerData.hitBoxAttackData,
            towerData.attackSpeed
        );

        float timer = 0f;

        while (timer < towerData.hitBoxAttackData.activeTime)
        {
            if (target == null)
                break;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > towerData.attackRange)
                break;

            RotateToTarget(target);

            timer += Time.deltaTime;
            yield return null;
        }

        hitBox.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());

        ObjectPoolManager.Instance.Despawn(hitBox);
        isAttacking = false;
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
    private void OnDrawGizmos()
    {
        if (towerData == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, towerData.attackRange);
    }
    #endregion
}
