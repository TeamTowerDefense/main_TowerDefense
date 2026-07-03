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

    private Dictionary<Monster, PoolableObject> activeHitEffects = new Dictionary<Monster, PoolableObject>();

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

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        Debug.Log($"[HitBox Trigger] other={other.name}, monster={(monster == null ? "NULL" : monster.name)}");

        if (monster == null || monster.isDead) 
            return;

        float tickInterval = hitBoxData.damageInterval / Mathf.Max(0.01f, attackSpeed);


        if (monster == null)
        {
            Debug.LogError("[HitBox] damageTimers 접근 직전 monster NULL");
            return;
        }

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
        if (monster == null)
            return;

        if (hitBoxData == null)
            return;


        monster.TakeDamage(damage);

        if (hitBoxData.hitEffectData == null)
            return;

        if (ObjectPoolManager.Instance == null)
            return;

        if (activeHitEffects.ContainsKey(monster))
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

        activeHitEffects[monster] = effect;
    }

    private void OnTriggerExit(Collider other)
    {
        Monster monster = other.GetComponent<Monster>();

        if (monster != null)
            return;

        if (!activeHitEffects.TryGetValue(monster, out PoolableObject effect))
            return;

        if (effect != null)
        {
            effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
            ObjectPoolManager.Instance.Despawn(effect);
        }

        activeHitEffects.Remove(monster);
        damageTimers.Remove(monster);
    }


    public override void OnDespawned()
    {
        target = null;
        foreach (var effect in activeHitEffects.Values)
        {
            if (effect == null) continue;

            effect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
            ObjectPoolManager.Instance.Despawn(effect);
        }
        damageTimers.Clear();
        //hitBoxData = null;
        base.OnDespawned();
    }

}
