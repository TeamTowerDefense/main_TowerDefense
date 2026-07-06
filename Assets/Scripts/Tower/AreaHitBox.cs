using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AreaHitBox : PoolableObject
{
    private int damage;
    private LayerMask monsterLayer;
    private HitBoxData hitBoxData;

    // ���� �ӵ�, ����
    private float attackSpeed;
    private float tickInterval;

    private Dictionary<Monster, float> damageTimers = new Dictionary<Monster, float>();
    private IHitBoxShapeInitializer shapeInitializer;

    private Collider Collider;

    private Transform target;

    private Dictionary<Monster, PoolableObject> activeHitEffects = new Dictionary<Monster, PoolableObject>();
    private HashSet<Monster> hitTargets = new HashSet<Monster>();

    private Dictionary<Monster, Coroutine> effectDespawnRoutines = new();

    private Tower ownerTower;

    private void Awake()
    {
        Collider = GetComponent<Collider>();
        Collider.enabled = false;
    }

    public void Initialize(Transform target, int damage, LayerMask monsterLayer, HitBoxData data, float attackSpeed, Tower owner)
    {
        this.target = target;
        this.damage = damage;
        this.monsterLayer = monsterLayer;
        this.hitBoxData = data;
        this.attackSpeed = attackSpeed;
        ownerTower = owner;

        damageTimers.Clear();
        hitTargets.Clear();

        if (Collider == null)
            Collider = GetComponent<Collider>();

        Collider.enabled = true;

        if (shapeInitializer == null)
            shapeInitializer = GetComponent<IHitBoxShapeInitializer>();

        if (shapeInitializer == null)
        {
            Debug.LogError($"{name}�� IHitBoxShapeInitializer�� �����ϴ�.");
            return;
        }

        shapeInitializer.Initialize(data);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hitBoxData.damageMode == HitBoxDamageMode.OncePerTarget)
            TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (hitBoxData.damageMode != HitBoxDamageMode.TickDamage)
            return;

        TryTickDamage(other);

    }

    private void OnTriggerExit(Collider other)
    {
        Monster monster = other.GetComponentInParent<Monster>();

        if (monster == null)
            return;

        damageTimers.Remove(monster);

        if (hitBoxData.damageMode != HitBoxDamageMode.TickDamage)
            return;

        if (!activeHitEffects.TryGetValue(monster, out PoolableObject effect))
            return;

        if (effectDespawnRoutines.TryGetValue(monster, out Coroutine routine))
        {
            StopCoroutine(routine);
            effectDespawnRoutines.Remove(monster);
        }

        effectDespawnRoutines[monster] = StartCoroutine
            (DespawnLoopEffectAfterDelay(monster, effect, hitBoxData.effectDespawnDelay));

    }

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        if (monster == null || monster.isDead)
            return;

        if(ownerTower != null)
        {
            TriggerOnHitEffects(monster);
        }

        if (hitBoxData.damageMode == HitBoxDamageMode.OncePerTarget)
        {
            if (hitTargets.Contains(monster))
                return;

            hitTargets.Add(monster);

            ApplyDamage(monster);

            ApplySlows(monster);

            SpawnOnceEffect(monster);
        }
        else if (hitBoxData.damageMode == HitBoxDamageMode.TickDamage)
        {
            SpawnOrKeepLoopEffect(monster);

            ApplyDamage(monster);
            damageTimers[monster] = GetTickInterval();
        }
    }

    private void TryTickDamage(Collider other)
    {

        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        Debug.Log($"[HitBox Trigger] other={other.name}, monster={(monster == null ? "NULL" : monster.name)}");

        if (monster == null || monster.isDead)
            return;

        SpawnOrKeepLoopEffect(monster);

        if (!damageTimers.ContainsKey(monster))
        {
            ApplyDamage(monster);
            damageTimers[monster] = tickInterval;
            return;
        }


        damageTimers[monster] -= Time.deltaTime;

        if (damageTimers[monster] <= 0f)
        {
            ApplyDamage(monster);
            damageTimers[monster] = tickInterval;
        }
    }

    private void ApplyDamage(Monster monster)
    {
        if (monster == null || hitBoxData == null)
            return;

        monster.TakeDamage(damage);
    }

    private void ApplySlows(Monster monster)
    {
        if (monster == null)
            return;

        if (hitBoxData.debuffs == null || hitBoxData.debuffs.Count == 0)
            return;

        MonsterStatus status = monster.GetComponent<MonsterStatus>();

        if (status == null)
            return;

    }


    private void SpawnOrKeepLoopEffect(Monster monster)
    {
        if (activeHitEffects.ContainsKey(monster))
        {
            if(effectDespawnRoutines.TryGetValue(monster, out Coroutine routine))
            {
                StopCoroutine(routine);
                effectDespawnRoutines.Remove(monster);
            }

            return;
        }

        PoolableObject effect = SpawnHitEffect(monster);

        if (effect == null)
            return;

        activeHitEffects.Add(monster, effect);
    }

    private void SpawnOnceEffect(Monster monster)
    {
        PoolableObject effect = SpawnHitEffect(monster);

        if (effect == null) 
            return;

        // TODO:
        // Monster �� ���ο� ���ӽð� �޼��� �޾ƿ� ��
        // ������ �̷� �������� ������.
        MonsterStatus status = monster.GetComponent<MonsterStatus>();
           

         float lifeTime = 1f;

        EffectLifeTimeDespawner despawner = effect.GetComponent<EffectLifeTimeDespawner>();

        if (despawner != null)
            despawner.StartLifeTime(lifeTime);
        else
            Debug.LogWarning($"{effect.name}�� EffectLifetimeDespawner�� �����ϴ�.");
    }

    private PoolableObject SpawnHitEffect(Monster monster)
    {
        if (monster == null)
            return null;

        if (hitBoxData.hitEffectData == null)
            return null;

        if (ObjectPoolManager.Instance == null)
            return null;

        GameObject effectPF = ObjectPoolManager.Instance.GetEffect(hitBoxData.hitEffectData.effectID);

        if (effectPF == null)
            return null;

        PoolableObject effect = ObjectPoolManager.Instance.Spawn<PoolableObject>(
            effectPF,
            monster.transform.position,
            Quaternion.identity,
            ObjectPoolManager.Instance.GetEffectParent()
        );

        if (effect == null)
            return null;

        effect.transform.SetParent(monster.transform);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        return effect;
    }

    private IEnumerator DespawnLoopEffectAfterDelay(Monster monster, PoolableObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null && ObjectPoolManager.Instance != null)
        {
            effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
            ObjectPoolManager.Instance.Despawn(effect);
        }

        activeHitEffects.Remove(monster);
        effectDespawnRoutines.Remove(monster);
    }

    private float GetTickInterval()
    {
        return hitBoxData.damageInterval / Math.Max(0.01f, attackSpeed);
    }

    public void DisableHitCollider()
    {
        if (Collider != null)
            Collider.enabled = false;
    }

    public override void OnDespawned()
    {
        target = null;

        foreach (var routine in effectDespawnRoutines.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        damageTimers.Clear();
        hitTargets.Clear();
        effectDespawnRoutines.Clear();

        foreach(var pair in activeHitEffects)
        {
            PoolableObject effect = pair.Value;

            if(effect == null)
                continue;

            effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
            ObjectPoolManager.Instance.Despawn(effect);
        }

        activeHitEffects.Clear();
        //hitBoxData = null;
        base.OnDespawned();
    }


    #region 히트 시 키워드 적용
    protected void TriggerOnHitEffects(Monster targetMonster)
    {
        if (ownerTower == null || targetMonster == null) return;

        KeywordController monsterKW = targetMonster.GetComponent<KeywordController>();
        KeywordController towerKW = ownerTower.GetComponent<KeywordController>();

        if (monsterKW == null) Debug.Log("에러: 몬스터한테 KeywordController가 없습니다!");
        if (towerKW == null) Debug.Log("에러: 타워한테 KeywordController가 없습니다!");

        if (monsterKW != null && towerKW != null)
        {
            var onHitModifiers = towerKW.GetKeywords<IOnHitModifier>();

            Debug.Log($"타워가 가진 적중 특성 개수: {onHitModifiers.Count}");

            foreach (var mod in onHitModifiers)
            {
                mod.OnHit(ownerTower, monsterKW);
                Debug.Log("슬로우 묻히기 성공!");
            }
        }
    }
    #endregion
}
