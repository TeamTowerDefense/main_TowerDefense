using UnityEngine;

[CreateAssetMenu(menuName = "Projectile/Normal")]
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
            monster.TakeDamage(damage);
    }
    #endregion
}
