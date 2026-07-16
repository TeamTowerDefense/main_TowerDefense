using System.Collections;
using UnityEngine;

public class PoolEffect : PoolableObject
{
    private ParticleSystem[] particles;
    private Coroutine despawnRoutine;

    private void Awake()
    {
        CacheParticles();
    }

    private void CacheParticles()
    {
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play()
    {
        if (particles == null || particles.Length == 0)
            CacheParticles();

        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }

        foreach (ParticleSystem ps in particles)
        {

            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.Clear(true);
            ps.Play(true);
        }

        despawnRoutine = StartCoroutine(DespawnAfterPlay());
    }

    private IEnumerator DespawnAfterPlay()
    {
        float maxDuration = 0f;

        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            float duration = main.duration + main.startLifetime.constantMax;
            maxDuration = Mathf.Max(maxDuration, duration);
        }

        yield return new WaitForSeconds(maxDuration);

        ObjectPoolManager.Instance.Despawn(this);
    }
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning(
                $"[PoolEffect] ObjectPoolManager 없음: {name}"
            );
            return;
        }

        Transform effectParent =
            ObjectPoolManager.Instance.GetEffectParent();

        // 몬스터 자식에서 PoolManager EffectParent로 복귀
        transform.SetParent(effectParent, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        ObjectPoolManager.Instance.Despawn(this);
    }

    public override void OnDespawned()
    {
        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }

        if (particles == null || particles.Length == 0)
            CacheParticles();


        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (ObjectPoolManager.Instance != null)
        {
            Transform effectParent =
                ObjectPoolManager.Instance.GetEffectParent();

            if (transform.parent != effectParent)
            {
                transform.SetParent(effectParent, false);
            }
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        //transform.localScale = Vector3.one;

        base.OnDespawned();
    }
}