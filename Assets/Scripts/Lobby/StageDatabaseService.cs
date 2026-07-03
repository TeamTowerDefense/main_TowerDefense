using IGameFlowInterface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageDatabaseService : GlobalServiceBase, IStageDatabaseService
{
    [Header("Addressables")]
    [SerializeField] string stageDataLabel = "StageData";

    [Header("로그")]
    [SerializeField] bool logLoadResult = true;


    readonly List<StageDataSO> stages = new();
    readonly Dictionary<string, StageDataSO> stageDataTable = new();

    AsyncOperationHandle<IList<StageDataSO>> loadHandle;

    public event Action Loaded;

    public bool IsLoaded { get; private set; }
    public IReadOnlyList<StageDataSO> Stages => stages;


    #region 생명주기

    protected override void OnAwake()
    {
        _ = LoadStageData();
    }

    protected override void OnDestroy()
    {
        if (loadHandle.IsValid())
            Addressables.Release(loadHandle);
    }

    #endregion

    #region 인터페이스

    public StageDataSO GetFirstStage() => stages.Count > 0 ? stages[0] : null;

    public bool TryGetStage(string stageId, out StageDataSO stageData)
    {
        stageData = null;

        if (string.IsNullOrWhiteSpace(stageId)) return false;
        if (stageDataTable.Count <= 0) return false;

        return stageDataTable.TryGetValue(stageId, out stageData);
    }

    #endregion

    #region 로드

    async Task LoadStageData()
    {
        IsLoaded = false;

        stages.Clear();
        stageDataTable.Clear();

        loadHandle = Addressables.LoadAssetsAsync<StageDataSO>(stageDataLabel, null);
        await loadHandle.Task;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[StageDatabaseService] StageData 로드 실패. Label: {stageDataLabel}", this);
            CompleteLoad();
            return;
        }

        foreach (StageDataSO stage in loadHandle.Result)
            TryAddStage(stage);

        stages.Sort((a, b) => string.CompareOrdinal(a.StageId, b.StageId));

        if (logLoadResult)
            Debug.Log($"[StageDatabaseService] StageData 로드 완료: {stages.Count}개", this);

        CompleteLoad();
    }

    void CompleteLoad()
    {
        IsLoaded = true;
        Loaded?.Invoke();
    }

    bool TryAddStage(StageDataSO stage)
    {
        if (stage == null) return false;

        if (string.IsNullOrWhiteSpace(stage.StageId))
        {
            Debug.LogWarning("[StageDatabaseService] StageId가 비어 있는 StageDataSO가 있습니다.", stage);
            return false;
        }

        if (stageDataTable.ContainsKey(stage.StageId))
        {
            Debug.LogWarning($"[StageDatabaseService] 중복 StageId 감지: {stage.StageId}", stage);
            return false;
        }

        stages.Add(stage);
        stageDataTable.Add(stage.StageId, stage);

        return true;
    }

    #endregion
}