using System.Collections;
using UnityEngine;

public class PoolEffect : PoolableObject
{
    private ParticleSystem[] particles;
    private Coroutine despawnRoutine;

    private void Awake()
    {
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play()
    {
        foreach (ParticleSystem ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        if (despawnRoutine != null)
            StopCoroutine(despawnRoutine);

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

    public override void OnDespawned()
    {
        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        base.OnDespawned();
    }
}