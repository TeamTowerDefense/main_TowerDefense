using IGameInterface;
using System;
using UnityEngine;

public class StageController : MonoBehaviour, IStageService, IAutoSceneService
{
    [Header("스테이지 데이터")]
    [SerializeField] StageDataSO stageData;

    [Header("시작 옵션")]
    [SerializeField] bool autoStartStage = true;
    [SerializeField] bool autoStartWave = false;

    IMapService mapService;
    IMonsterSpawnManager spawnManager;

    int currentWaveIndex = -1;

    public StageState CurrentState { get; private set; } = StageState.None;
    public int CurrentBaseHp { get; private set; }
    public int MaxBaseHp { get; private set; }
    public int TowerLimit { get; private set; }

    public int CurrentWaveIndex => currentWaveIndex;
    public int WaveCount => stageData?.Waves?.Count ?? 0;
    public StageWaveEntry CurrentWave => IsValidWaveIndex(currentWaveIndex) ? stageData.Waves[currentWaveIndex] : null;

    public event Action<StageState> StateChanged;
    public event Action<int, int> BaseHpChanged;

    #region 생명주기

    void Awake()
    {
        ((IAutoSceneService)this).RegisterSceneServices();
        ResolveServices();
    }

    void Start()
    {
        if (!autoStartStage) return;

        StartStage();

        if (autoStartWave)
            StartWave();
    }

    void Update()
    {
        if (CurrentState == StageState.Playing)
            CheckStageEnd();
    }

    void OnDestroy()
    {
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    #endregion

    #region 인터페이스

    public void StartStage()
    {
        if (!HasValidStageData())
        {
            Debug.LogError("[StageController] StageDataSO 또는 Waves가 없습니다.", this);
            SetState(StageState.None);
            return;
        }

        ResolveServices();

        currentWaveIndex = -1;

        MaxBaseHp = Mathf.Max(1, stageData.BaseHp);
        CurrentBaseHp = MaxBaseHp;
        TowerLimit = Mathf.Max(0, stageData.TowerLimit);

        BaseHpChanged?.Invoke(CurrentBaseHp, MaxBaseHp);
        SetState(StageState.Prepare);
    }

    public void StartWave()
    {
        if (CurrentState != StageState.Prepare && CurrentState != StageState.WaveClear)
            return;

        if (!HasValidStageData())
        {
            Debug.LogError("[StageController] 시작 가능한 웨이브 데이터가 없습니다.", this);
            return;
        }

        int nextWaveIndex = currentWaveIndex + 1;

        if (!IsValidWaveIndex(nextWaveIndex))
        {
            ClearStage();
            return;
        }

        StageWaveEntry wave = stageData.Waves[nextWaveIndex];

        if (wave == null || wave.SpawnData == null)
        {
            Debug.LogError($"[StageController] Wave {nextWaveIndex}의 SpawnData가 없습니다.", this);
            return;
        }

        ResolveServices();

        if (spawnManager == null)
        {
            Debug.LogError("[StageController] IMonsterSpawnManager를 찾지 못했습니다.", this);
            return;
        }

        currentWaveIndex = nextWaveIndex;
        spawnManager.StartWave(wave.SpawnData);

        SetState(StageState.Playing);
    }

    public void TakeBaseDamage(int damage)
    {
        if (damage <= 0) return;
        if (CurrentState == StageState.StageClear || CurrentState == StageState.StageFailed) return;

        CurrentBaseHp = Mathf.Max(0, CurrentBaseHp - damage);
        BaseHpChanged?.Invoke(CurrentBaseHp, MaxBaseHp);

        if (CurrentBaseHp <= 0)
            FailStage();
    }

    #endregion

    #region 내부 유틸

    void CheckStageEnd()
    {
        ResolveServices();

        if (CurrentBaseHp <= 0)
        {
            FailStage();
            return;
        }

        if (spawnManager == null || mapService == null) return;
        if (!spawnManager.SpawnFinished) return;
        if (mapService.AliveEnemyCount > 0) return;

        CompleteWave();
    }

    void CompleteWave()
    {
        spawnManager?.StopWave();

        bool isLastWave = currentWaveIndex >= WaveCount - 1;

        if (isLastWave)
        {
            ClearStage();
            return;
        }

        SetState(StageState.WaveClear);
    }

    void ClearStage()
    {
        if (CurrentState == StageState.StageClear) return;

        spawnManager?.StopWave();
        SetState(StageState.StageClear);
    }

    void FailStage()
    {
        if (CurrentState == StageState.StageFailed) return;

        spawnManager?.StopWave();
        SetState(StageState.StageFailed);
    }

    void SetState(StageState state)
    {
        if (CurrentState == state) return;

        CurrentState = state;
        StateChanged?.Invoke(CurrentState);

        Debug.Log($"[StageController] State => {CurrentState}", this);
    }

    void ResolveServices()
    {
        if (mapService == null) ServiceLocator.TryGet(out mapService);
        if (spawnManager == null) ServiceLocator.TryGet(out spawnManager);
    }

    bool HasValidStageData() => stageData != null && stageData.Waves != null && stageData.Waves.Count > 0;
    bool IsValidWaveIndex(int index) => stageData?.Waves != null && index >= 0 && index < stageData.Waves.Count;

    #endregion
}