using IGameInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManagerStageProvider : MonoBehaviour, IMonsterSpawnManager, IAutoSceneService
{
    [SerializeField] MonsterManager monsterManager;
    [SerializeField] int pathIndex = 0;
    [SerializeField] bool useUnscaledSpawnDelay = false;

    Coroutine waveRoutine;
    IStageService stageService;
    IStageMonsterTracker monsterTracker;

    public bool IsSpawning { get; private set; }
    public bool SpawnFinished { get; private set; } = true;

    #region 생명주기

    void Awake()
    {
        if (!monsterManager) monsterManager = GetComponent<MonsterManager>();
        ((IAutoSceneService)this).RegisterSceneServices();
    }

    void Start()
    {
        BindServices();
    }

    void OnDestroy()
    {
        UnbindStageService();
        StopWave();
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    #endregion

    #region 웨이브

    public void StartWave(MonsterSpawnDataSO spawnData)
    {
        StopWave();

        if (!monsterManager || spawnData == null || spawnData.IsEmpty)
        {
            IsSpawning = false;
            SpawnFinished = true;
            return;
        }

        BindServices();

        IsSpawning = true;
        SpawnFinished = false;
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
        SpawnFinished = true;
    }

    IEnumerator WaveRoutine(MonsterSpawnDataSO spawnData)
    {
        Queue<MonsterSpawnGroup> queue = spawnData.CreateQueue();

        while (queue.Count > 0)
        {
            MonsterSpawnGroup group = queue.Dequeue();
            if (!CanSpawnGroup(group)) continue;

            yield return SpawnGroupRoutine(group);
        }

        waveRoutine = null;
        IsSpawning = false;
        SpawnFinished = true;
    }

    IEnumerator SpawnGroupRoutine(MonsterSpawnGroup group)
    {
        PathData path = GetPath();
        if (path == null) yield break;

        if (group.StartDelay > 0f)
            yield return Wait(group.StartDelay);

        foreach (MonsterSpawnElement element in group.Elements)
        {
            if(element == null) 
                continue;
            if(element.MonsterData == null) 
                continue;

            path.monsterData = element.MonsterData;
            for (int i = 0; i < element.Count; i++)
            {
                monsterManager.SpawnPathGroup(path.monsterData, path, 1, 0f);
            }

            if (group.elementInterval > 0f)
                yield return Wait(group.elementInterval);
        }

        if (group.Interval > 0f)
            yield return Wait(group.Interval);
    }

    IEnumerator Wait(float seconds)
    {
        if (useUnscaledSpawnDelay) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }

    bool CanSpawnGroup(MonsterSpawnGroup group)
    {
        if (group == null) 
            return false;
        if (group.Elements == null)
            return false;
        if (group.Elements.Count == 0) 
            return false;
        return true;
    }

    PathData GetPath()
    {
        if (!monsterManager || monsterManager.paths == null || monsterManager.paths.Count <= 0) return null;

        int index = Mathf.Clamp(pathIndex, 0, monsterManager.paths.Count - 1);
        return monsterManager.paths[index];
    }

    #endregion

    #region 스테이지 종료

    void BindServices()
    {
        ServiceLocator.TryGet(out monsterTracker);

        if (stageService != null) return;
        if (!ServiceLocator.TryGet(out stageService) || stageService == null) return;

        stageService.StateChanged -= OnStageStateChanged;
        stageService.StateChanged += OnStageStateChanged;
    }

    void UnbindStageService()
    {
        if (stageService == null) return;

        stageService.StateChanged -= OnStageStateChanged;
        stageService = null;
    }

    void OnStageStateChanged(StageState state)
    {
        if (state != StageState.StageClear && state != StageState.StageFailed) return;

        StopWave();

        ServiceLocator.TryGet(out monsterTracker);
        monsterTracker?.DespawnAllImmediate();
    }

    #endregion
}