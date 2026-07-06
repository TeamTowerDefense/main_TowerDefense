using UnityEngine;

public class ExplosiveProjectile : Projectile
{
    private ExplosiveProjectileData explosiveData;

    public override void Initialize(Transform target, int damage, ProjectileData projectileData, Tower owner)
    {
        base.Initialize(target, damage, projectileData, owner);

        explosiveData = projectileData as ExplosiveProjectileData;
    }

    protected override void OnHit()
    {
        ExplosionHit();
    }

    #region Æø¹ß
    private void ExplosionHit()
    {
        DrawExplosionDebug(transform.position, explosiveData.ExplosionRadius);
        Collider[] hits = Physics.OverlapSphere(transform.position, explosiveData.ExplosionRadius, explosiveData.targetLayer);

        foreach (Collider hit in hits)
        {
            Monster monster = hit.GetComponentInParent<Monster>();

            if (monster != null)
            {
                TriggerOnHitEffects(monster);
                monster.TakeDamage(damage);
            }
        }
    }

    private void DrawExplosionDebug(Vector3 center, float radius)
    {
        const int segments = 32;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * Mathf.PI * 2 / segments;
            float angle2 = (i + 1) * Mathf.PI * 2 / segments;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

            Debug.DrawLine(p1, p2, Color.red, 2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (explosiveData == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosiveData.ExplosionRadius);
    }

    #endregion
}
