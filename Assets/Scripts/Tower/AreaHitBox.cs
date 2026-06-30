using System.Collections.Generic;
using UnityEngine;

public class AreaHitBox : PoolableObject
{
    private int damage;
    private LayerMask monsterLayer;
    private HitBoxAttackData hitBoxData;

    private Dictionary<Monster, float> damageTimers = new Dictionary<Monster, float>();

    private BoxCollider boxCollider;

    private Transform target;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public void Initialize(Transform target, int damage, LayerMask monsterLayer, HitBoxAttackData data)
    {
        this.target = target;
        this.damage = damage;
        this.monsterLayer = monsterLayer;
        this.hitBoxData = data;

        damageTimers.Clear();

        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        boxCollider.isTrigger = true;
        boxCollider.size = data.boxSize;
        boxCollider.center = data.boxCenter;
    } 

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & monsterLayer) == 0)
            return;

        Monster monster = other.GetComponentInParent<Monster>();

        if (monster == null) 
            return;

        if (!damageTimers.ContainsKey(monster))
        {
            monster.TakeDamage(damage);
            damageTimers[monster] = hitBoxData.damageInterval;
            return;
        }

        damageTimers[monster] -= Time.deltaTime;

        if (damageTimers[monster] <= 0f)
        {
            monster.TakeDamage(damage);
            damageTimers[monster] = hitBoxData.damageInterval;
        }

    }

    public override void OnDespawned()
    {
        target = null;
        damageTimers.Clear();
        //hitBoxData = null;

        base.OnDespawned();
    }

}
