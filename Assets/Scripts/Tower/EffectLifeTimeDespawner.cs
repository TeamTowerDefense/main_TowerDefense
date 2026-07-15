using System.Collections;
using UnityEngine;

public class EffectLifeTimeDespawner : MonoBehaviour
{
    private PoolableObject poolableObject;
    private Coroutine despawnRoutine;

    private void Awake()
    {
        poolableObject = GetComponent<PoolableObject>();
    }

    public void StartLifeTime(float lifeTime)
    {
        Debug.Log($"[EffectPool] Lifetime Ω√¿€: {name}, {lifeTime}");

        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
        }

        despawnRoutine = StartCoroutine(DespawnAfterTime(lifeTime));
    }

    private IEnumerator DespawnAfterTime(float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());
        ObjectPoolManager.Instance.Despawn(poolableObject);
    }

}
