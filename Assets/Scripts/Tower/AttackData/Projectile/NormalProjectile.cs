using UnityEngine;

public class NormalProjectile : Projectile
{
    protected override void OnHit()
    {
        SingleHit();
    }

    #region 단일 공격
    private void SingleHit()
    {
        Monster monster = target.GetComponent<Monster>();

        if (monster != null)
        {
            TriggerOnHitEffects(monster);
            monster.TakeDamage(damage);
        }

    }
    #endregion
}
