using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AreaHitBox : PoolableObject
{
    private int damage;
    private LayerMask monsterLayer;
    private HitBoxData hitBoxData;

    // 공격 속도, 간격
    private float attackSpeed;
    private float tickInterval;

    private Dictionary<Monster, float> damageTimers = new Dictionary<Monster, float>();
    private IHitBoxShapeInitializer shapeInitializer;

    private Collider Collider;

    private Transform target;
    private HashSet<Monster> hitTargets = new HashSet<Monster>();

    private Dictionary<Monster, List<PoolableObject>> activeHitEffects = new Dictionary<Monster, List<PoolableObject>>();

    private void Awake()
    {
        Collider = GetComponent<Collider>();
    }

    public void Initialize(Transform target, int damage, LayerMask monsterLayer, HitBoxData data, float attackSpeed)
    {
        this.target = target;
        this.damage = damage;
        this.monsterLayer = monsterLayer;
        this.hitBoxData = data;
        this.attackSpeed = attackSpeed;

        damageTimers.Clear();
        hitTargets.Clear();

        if (Collider == null)
            Collider = GetComponent<Collider>();

        if (shapeInitializer == null)
            shapeInitializer = GetComponent<IHitBoxShapeInitializer>();

        if (shapeInitializer == null)
        {
            Debug.LogError($"{name}에 IHitBoxShapeInitializer가 없습니다.");
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
    }

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        if (monster == null || monster.isDead)
            return;

        if (hitTargets.Contains(monster))
            return;

        hitTargets.Add(monster);

        ApplyDamage(monster);
    }


    private void TryTickDamage(Collider other)
    {

        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        Debug.Log($"[HitBox Trigger] other={other.name}, monster={(monster == null ? "NULL" : monster.name)}");

        if (monster == null || monster.isDead)
            return;

        float tickInterval = hitBoxData.damageInterval / Mathf.Max(0.01f, attackSpeed);

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
        SpawnHitEffect(monster);
    }

    private void SpawnHitEffect(Monster monster)
    {
        if (monster == null)
            return;

        if (hitBoxData.hitEffectData == null)
            return;

        if (ObjectPoolManager.Instance == null)
            return;

        GameObject effectPF = ObjectPoolManager.Instance.GetEffect(hitBoxData.hitEffectData.effectID);


        if (effectPF == null)
            return;

        PoolableObject effect = ObjectPoolManager.Instance.Spawn<PoolableObject>(
            effectPF,
            monster.transform.position,
            Quaternion.identity,
            ObjectPoolManager.Instance.GetEffectParent()
        );

        if (effect == null)
            return;

        effect.transform.SetParent(monster.transform);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        float lifeTime = hitBoxData.hitEffectData.lifeTime;

        StartCoroutine(DespawnEffectAfterTime(effect, lifeTime));
    }

    private IEnumerator DespawnEffectAfterTime(PoolableObject effect, float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);

        if (effect == null)
            yield break;

        if (ObjectPoolManager.Instance == null)
            yield break;

        effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
        ObjectPoolManager.Instance.Despawn(effect);
    }

    public void DisableHitCollider()
    {
        if (Collider != null)
            Collider.enabled = false;
    }

    public override void OnDespawned()
    {
        target = null;
        damageTimers.Clear();
        hitTargets.Clear();

        //hitBoxData = null;
        base.OnDespawned();
    }

}
