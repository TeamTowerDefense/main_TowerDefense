using IGameInterface;
using System.Collections.Generic;
using UnityEngine;

public class StageMonsterTracker : MonoBehaviour, IStageMonsterTracker, IAutoSceneService
{
    readonly HashSet<GameObject> monsters = new();
    readonly List<GameObject> buffer = new();

    #region 생명주기

    void Awake()
    {
        ((IAutoSceneService)this).RegisterSceneServices();
    }

    void OnDestroy()
    {
        ((IAutoSceneService)this).UnregisterSceneServices();
        monsters.Clear();
        buffer.Clear();
    }

    #endregion

    #region 등록

    public void Register(GameObject monsterObject)
    {
        if (monsterObject) monsters.Add(monsterObject);
    }

    public void Unregister(GameObject monsterObject)
    {
        if (monsterObject) monsters.Remove(monsterObject);
    }

    #endregion

    #region 정리

    public void DespawnAllImmediate()
    {
        buffer.Clear();

        foreach (GameObject monster in monsters)
            if (monster) buffer.Add(monster);

        foreach (GameObject monster in buffer)
            ForceDespawn(monster);

        monsters.Clear();
        buffer.Clear();
    }

    void ForceDespawn(GameObject monster)
    {
        if (!monster) return;

        foreach (MonoBehaviour behaviour in monster.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour) behaviour.StopAllCoroutines();

        foreach (ParticleSystem particle in monster.GetComponentsInChildren<ParticleSystem>(true))
            if (particle) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (monster.TryGetComponent(out PoolableObject poolable) && ObjectPoolManager.Instance)
            ObjectPoolManager.Instance.Despawn(poolable);

        if (monster) monster.SetActive(false);
    }

    #endregion
}