using IGameInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManagerStageProvider : MonoBehaviour, IMonsterSpawnManager, IAutoSceneService
{
    [SerializeField] MonsterManager monsterManager;
    [SerializeField] int pathIndex = 0;

    Coroutine waveRoutine;

    public bool IsSpawning { get; private set; }
    public bool SpawnFinished { get; private set; } = true;

    void Awake()
    {
        if (!monsterManager) monsterManager = GetComponent<MonsterManager>();
        ((IAutoSceneService)this).RegisterSceneServices();
    }

    void OnDestroy()
    {
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    public void StartWave(MonsterSpawnDataSO spawnData)
    {
        StopWave();

        if (monsterManager == null || spawnData == null)
        {
            SpawnFinished = true;
            return;
        }

        waveRoutine = StartCoroutine(WaveRoutine(spawnData));
    }

    public void StopWave()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        IsSpawning = false;
    }

    IEnumerator WaveRoutine(MonsterSpawnDataSO spawnData)
    {
        IsSpawning = true;
        SpawnFinished = false;

        Queue<MonsterSpawnGroup> queue = spawnData.CreateQueue();

        while (queue.Count > 0)
        {
            MonsterSpawnGroup group = queue.Dequeue();

            if (group == null || group.MonsterData == null || group.Count <= 0) continue;

            yield return SpawnGroupRoutine(group);
        }

        IsSpawning = false;
        SpawnFinished = true;
    }

    IEnumerator SpawnGroupRoutine(MonsterSpawnGroup group)
    {
        PathData path = GetPath();
        if (path == null) yield break;

        path.countPerSpawn = group.Count;
        path.spawnInterval = 0.2f;

        yield return monsterManager.SpawnPathGroup(path);

        if (group.Interval > 0f) yield return new WaitForSeconds(group.Interval);
    }

    PathData GetPath()
    {
        if (monsterManager.paths == null || monsterManager.paths.Count <= 0) return null;

        int index = Mathf.Clamp(pathIndex, 0, monsterManager.paths.Count - 1);
        return monsterManager.paths[index];
    }
}