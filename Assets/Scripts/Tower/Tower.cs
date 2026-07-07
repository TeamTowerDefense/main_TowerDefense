
using IGameInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : BuildingBase
{
    private TowerData towerData => buildingData as TowerData;  

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Target")]
    [SerializeField] private LayerMask monsterLayer;

    [Header("Rotation")]
    [SerializeField] private Transform rotateBody;
    [SerializeField] private float rotateSpeed = 10f;

    private float attackTimer;
    private bool isAttacking;
    private ITowerTargetFinder targetFinder;

    private KeywordController keywordController;
    private Dictionary<StatType, RuntimeStat> stats = new Dictionary<StatType, RuntimeStat>();
    private List<IStatModifier> cachedModifiers = new List<IStatModifier>();

    private void Awake()
    {
        if (towerData == null)
        {
            Debug.LogError($"{name} : TowerData 없음");
            enabled = false;
            return;
        }

        if(!towerData.CheckAttackData())
        {
            enabled = false;
        }

        if (towerData != null)
        {
            foreach (var kvp in towerData.GetInitialStats())
            {
                stats[kvp.Key] = new RuntimeStat(kvp.Value);
            }
        }

        keywordController = GetComponent<KeywordController>();
        targetFinder = GetComponent<ITowerTargetFinder>();

        if(keywordController != null)
            keywordController.OnKeywordChanged += UpdateAllStats;
    }

    private void OnDestroy()
    {
        if (keywordController != null)
            keywordController.OnKeywordChanged -= UpdateAllStats;
    }

    private void Start()
    {
        if (towerData.defaultKeywords != null)
        {
            foreach (KeywordData kw in towerData.defaultKeywords)
                keywordController.AddKeyword(kw);
        }
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
        int finalDamage = Mathf.RoundToInt(GetStat(StatType.AttackDamage));

        List<IDamageModifier> damageModifiers = keywordController.GetKeywords<IDamageModifier>();
        foreach (var mod in damageModifiers)
        {
            finalDamage = mod.ModifyDamage(finalDamage, target);
        }

        if (towerData.attackMechanism is ProjectileData projData)
        {
            ShootProjectile(target, projData);
        }
        // 2. 만약 장착된 부품이 '히트박스' 타입이라면?
        else if (towerData.attackMechanism is HitBoxData hitBoxData)
        {
            if (isAttacking)
                return;

            if (hitBoxData.isMortar)
                StartCoroutine(UseMortarHitBoxAttack(target, hitBoxData));
            else
                StartCoroutine(UseHitBoxAttack(target, hitBoxData));
        }

    }
    #endregion

    #region 투사체 발사
    private void ShootProjectile(Transform target, ProjectileData projectileData)
    {

        if (ObjectPoolManager.Instance == null)
        {
            //Debug.LogError("ObjectPoolManager.Instance가 null입니다. 씬에 ObjectPoolManager 오브젝트가 없습니다.");
            return;
        }

        if (firePoint == null)
            return;

        GameObject prefab = ObjectPoolManager.Instance.GetProjectile(projectileData.projectileID);

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
        projectile.Initialize(target, towerData.damage, projectileData, this);

    }
    #endregion

    #region 히트박사 발사
    private IEnumerator UseHitBoxAttack(Transform target, HitBoxData hitBoxData)
    {
        if (ObjectPoolManager.Instance == null)
        {
            //Debug.LogError("ObjectPoolManager.Instance가 null입니다.");
            yield break;
        }

        if (hitBoxData == null)
        {
            //Debug.LogError($"{name} : hitBoxAttackData 없음");
            yield break;
        }

        isAttacking = true;

        GameObject prefab = ObjectPoolManager.Instance.GetHitBox(hitBoxData.hitBoxID);

        if (prefab == null)
        {
            Debug.LogError($"[HitBox] 프리팹 없음 ID: {hitBoxData.hitBoxID}, Data: {hitBoxData.name}");
            isAttacking = false;
            yield break;
        }

        Debug.Log($"[HitBox] 로드 결과 = {(prefab == null ? "NULL" : prefab.name)}");
        
        AreaHitBox hitBox = ObjectPoolManager.Instance.Spawn<AreaHitBox>(
            prefab,
            firePoint.position,
            firePoint.rotation,
            ObjectPoolManager.Instance.GetEffectParent()
        );

        if (hitBox == null)
        {
            //Debug.LogError($"{prefab.name} : AreaHitBox Spawn 실패");
            isAttacking = false;
            yield break;
        }

        hitBox.transform.SetParent(firePoint);
        hitBox.transform.localPosition = Vector3.zero;
        hitBox.transform.localRotation = Quaternion.identity;


        hitBox.Initialize(
            target,
            towerData.damage,
            towerData.monsterLayer,
            hitBoxData,
            towerData.attackSpeed,
            this
        );

        //float timer = 0f;
        if (hitBoxData.damageMode == HitBoxDamageMode.TickDamage)
        {
            while (target != null)
            {
                Monster monster = target.GetComponent<Monster>();

                if (monster != null && monster.isDead)
                    break;

                float distance = Vector3.Distance(transform.position, target.position);

                if (distance > towerData.attackRange)
                    break;

                RotateToTarget(target);
                yield return null;
            }
        }
        else if (hitBoxData.damageMode == HitBoxDamageMode.OncePerTarget)
        {
            yield return new WaitForSeconds(hitBoxData.colliderActiveTime);

            hitBox.DisableHitCollider();

            float remainTime = Mathf.Max(0f, hitBoxData.activeTime - hitBoxData.colliderActiveTime);

            if (remainTime > 0f)
                yield return new WaitForSeconds(remainTime);
        }

        hitBox.DisableHitCollider();
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

    #region 곡사포 메서드
    private IEnumerator UseMortarHitBoxAttack(Transform target, HitBoxData hitBoxData)
    {
        if (ObjectPoolManager.Instance == null)
            yield break;

        if (target == null || hitBoxData == null)
            yield break;

        isAttacking = true;

        Vector3 impactPosition = target.position;

        RotateToTarget(transform);

        SpawnLunchEffect(impactPosition, hitBoxData);

        SpawnWarningEffect(impactPosition, hitBoxData);

        yield return new WaitForSeconds(hitBoxData.impactDelay);

        SpawnMortarHitBox(impactPosition, hitBoxData);

        isAttacking = false;
    }
    #endregion

    #region 곡사포 발사 메서드
    private void SpawnLunchEffect(Vector3 impactPosition, HitBoxData hitBoxData)
    {
        if (hitBoxData.launchEffectID < 0)
            return;

        if (hitBoxData.hitEffectData == null)
        {
            Debug.LogWarning("[Mortar] hitEffectData NULL");
            return;
        }
        Debug.Log($"[Mortar] impact effectID = {hitBoxData.hitEffectData.effectID}");

        GameObject effectPF = ObjectPoolManager.Instance.GetEffect(hitBoxData.launchEffectID);

        if (effectPF == null)
        {
            Debug.LogWarning($"[Mortar] impact effect prefab NULL ID={hitBoxData.hitEffectData.effectID}");
            return;
        }

        Vector3 startPos = firePoint.position;
        Vector3 dir = impactPosition - startPos;

        PoolableObject effect = ObjectPoolManager.Instance.Spawn<PoolableObject>
            (effectPF, startPos, Quaternion.LookRotation(dir.normalized), ObjectPoolManager.Instance.GetEffectParent());

        if (effect == null) 
            return;

        LineRenderer line = effect.GetComponent<LineRenderer>();

        if (line != null)
        {
            line.SetPosition(0, startPos);
            line.SetPosition(1, impactPosition);
        }
        
        EffectLifeTimeDespawner despawner = effect.GetComponent<EffectLifeTimeDespawner>();

        if (despawner != null)
            despawner.StartLifeTime(hitBoxData.impactDelay);
    }
    #endregion

    #region 착탄 범위 표시 메서드
    private void SpawnWarningEffect(Vector3 impactPosition, HitBoxData hitBoxData)
    {
        if (hitBoxData.warningEffectID < 0)
            return;

        GameObject effectPF = ObjectPoolManager.Instance.GetEffect(hitBoxData.warningEffectID);

        if (effectPF == null)
            return;

        Vector3 effectPos = impactPosition + Vector3.up * 0.2f;

        PoolableObject effect = ObjectPoolManager.Instance.Spawn<PoolableObject>
            (effectPF, effectPos, Quaternion.identity, ObjectPoolManager.Instance.GetEffectParent());

        if (effect == null) 
            return;

        effect.transform.position = effectPos;
        effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());

        EffectLifeTimeDespawner despawner = effect.GetComponent<EffectLifeTimeDespawner>();

        if (despawner != null)
            despawner.StartLifeTime(hitBoxData.impactDelay + 0.2f);
    }
    #endregion

    #region 착탄 HitBox 생성 메서드
    private void SpawnMortarHitBox(Vector3 impactPosition, HitBoxData hitBoxData)
    {
        GameObject prefab = ObjectPoolManager.Instance.GetHitBox(hitBoxData.hitBoxID);
    
        if (prefab == null) 
        {
            Debug.LogError($"[MortarHitBox] 프리팹 없음 ID : {hitBoxData.hitBoxID}");
            return; 
        }

        Vector3 effectPos = impactPosition + Vector3.up * 0.2f;

        AreaHitBox hitBox = ObjectPoolManager.Instance.Spawn<AreaHitBox>
            (prefab, effectPos, Quaternion.identity, ObjectPoolManager.Instance.GetEffectParent());

        if (hitBox == null) 
            return;

        hitBox.transform.position = effectPos;
        hitBox.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());

        hitBox.Initialize(null, towerData.damage, towerData.monsterLayer, hitBoxData, towerData.attackSpeed, this);

        StartCoroutine(DespawnMortarHitBox(hitBox, hitBoxData.hitColliderActiveTime));
    }

    private IEnumerator DespawnMortarHitBox(AreaHitBox hitBox, float activeTime)
    {
        yield return new WaitForSeconds(activeTime);

        if (hitBox == null)
            yield break;

        hitBox.DisableHitCollider();

        hitBox.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
        ObjectPoolManager.Instance.Despawn(hitBox);
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

    #region 스텟
    public float GetStat(StatType type) => stats.TryGetValue(type, out var stat) ? stat.CurrentValue : 0f;

    private void UpdateAllStats()
    {
        // IStatModifer를 상속하는 모든 키워드 저장
        List<IStatModifier> allModifiers = keywordController.GetKeywords<IStatModifier>();

        // 2. 스탯 서랍장(Dictionary)을 순회합니다.
        foreach (var kvp in stats)
        {
            // LINQ의 .ToList() 역할을 할 빈 리스트를 직접 만듭니다.
            cachedModifiers.Clear();

            // LINQ의 .Where(...) 역할을 할 수동 반복문을 돌립니다.
            foreach (var modifier in allModifiers)
            {
                // 모디파이어의 타겟 스탯이 현재 순회 중인 스탯(kvp.Key)과 같다면
                if (modifier.TargetStat == kvp.Key)
                {
                    // 리스트에 추가합니다.
                    cachedModifiers.Add(modifier);
                }
            }

            // 완성된 리스트를 재계산 함수로 넘겨줍니다.
            kvp.Value.RecalculateStat(cachedModifiers);
        }

    }
    #endregion

}
