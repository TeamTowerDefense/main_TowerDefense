using System.Collections;
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
                StartCoroutine(SpawnTargetHitEffect(monster));
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

    #region ¸ó½ºÅÍ ±¤¿ª ÇÇÇØ ÀÌÆåÆ®
    private IEnumerator SpawnTargetHitEffect(Monster monster)
    {
        Debug.Log($"[Launcher] SpawnTargetHitEffect : {monster.name}");
        Debug.Log($"EffectData = {explosiveData.targetHitEffectData}");
        if (monster == null)
            yield return null;

        if (ObjectPoolManager.Instance == null)
            yield return null;

        //int effectID = explosiveData.hitEffectID;
        int effectID = explosiveData.targetHitEffectData.effectID;
        Debug.Log($"EffectID = {effectID}");
        GameObject effectPF = ObjectPoolManager.Instance.GetEffect(effectID);

        if (effectPF == null)
        {
            Debug.LogWarning($"[Launcher] Target HitEffect ¾øÀ½. ID={effectID}");
            yield return null;
        }

        Vector3 spawnPosition = monster.transform.position + Vector3.up * explosiveData.targetHitEffectYOffset;

        PoolableObject effect = ObjectPoolManager.Instance.Spawn<PoolableObject>
            (effectPF, spawnPosition, Quaternion.identity, ObjectPoolManager.Instance.GetEffectParent());

        if (effect == null)
            yield return null;

        yield return new WaitForSeconds(0.3f);

        effect.transform.SetParent(monster.transform);
        effect.transform.localPosition = Vector3.up * explosiveData.targetHitEffectYOffset;
        effect.transform.localRotation = Quaternion.identity;

        EffectLifeTimeDespawner lifeTimeDespawner = effect.GetComponent<EffectLifeTimeDespawner>();

        if (lifeTimeDespawner != null)
        {
            float lifeTime = Mathf.Max(0.1f, explosiveData.targetHitEffectLifetime);

            lifeTimeDespawner.StartLifeTime(lifeTime);
        }

    }
    #endregion
}
