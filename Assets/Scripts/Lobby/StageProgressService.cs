using IGameFlowInterface;
using IGameInterface;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageProgressService : GlobalServiceBase, IStageProgressService
{
    #region 인스펙터

    [Header("저장")]
    [SerializeField] string saveDataName = "StageProgress";

    [Header("해금")]
    [SerializeField] bool unlockFirstStageByDefault = true;

    [Header("로그")]
    [SerializeField] bool logSaveLoad = true;

    #endregion

    #region 필드

    bool isLoaded;

    ISaveService saveService;
    IStageDatabaseService stageDatabaseService;

    StageProgressSaveData saveData = new();

    #endregion

    #region 프로퍼티

    public bool IsLoaded => isLoaded;

    #endregion

    #region 생명주기

    protected override void OnAwake()
    {
        Load();
    }

    #endregion

    #region 로드 / 저장

    public void Load()
    {
        if (!ResolveSaveService())
        {
            Debug.LogWarning("[StageProgressService] ISaveService를 찾지 못했습니다. 나중에 다시 Load가 필요합니다.", this);
            saveData = new StageProgressSaveData();
            isLoaded = false;
            return;
        }

        saveData = saveService.LoadOrCreate<StageProgressSaveData>(saveDataName);
        saveData.records ??= new List<StageProgressRecord>();

        RemoveInvalidRecords();

        isLoaded = true;

        if (logSaveLoad)
            Debug.Log($"[StageProgressService] Load 완료: {saveData.records.Count}개 기록", this);
    }

    public void Save()
    {
        if (!ResolveSaveService())
        {
            Debug.LogWarning("[StageProgressService] 저장 실패: ISaveService를 찾지 못했습니다.", this);
            return;
        }

        saveData ??= new StageProgressSaveData();
        saveData.records ??= new List<StageProgressRecord>();

        saveService.Save(saveDataName, saveData);

        if (logSaveLoad)
            Debug.Log("[StageProgressService] Save 완료", this);
    }

    #endregion

    #region 조회

    public StageProgressRecord GetRecord(string stageId)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(stageId)) return null;

        return saveData.records.FirstOrDefault(record => record.stageId == stageId);
    }

    public int GetStarMask(string stageId) => GetRecord(stageId)?.starMask ?? 0;

    public bool IsStageCleared(string stageId) => GetRecord(stageId)?.isCleared ?? false;

    public bool IsStageUnlocked(StageDataSO stageData)
    {
        if (stageData == null || string.IsNullOrWhiteSpace(stageData.StageId)) return false;

        EnsureLoaded();

        if (IsStageCleared(stageData.StageId)) return true;
        if (unlockFirstStageByDefault && IsFirstStage(stageData)) return true;
        if (!ResolveStageDatabaseService()) return false;

        foreach (StageProgressRecord record in saveData.records)
        {
            if (record == null || !record.isCleared) continue;
            if (!stageDatabaseService.TryGetStage(record.stageId, out StageDataSO clearedStage)) continue;
            if (clearedStage.NextStageIds == null) continue;

            if (clearedStage.NextStageIds.Contains(stageData.StageId)) return true;
        }

        return false;
    }

    #endregion

    #region 결과 반영

    public StageProgressApplyResult ApplyResult(StageDataSO stageData, StageResultContext result)
    {
        EnsureLoaded();

        if (stageData == null || string.IsNullOrWhiteSpace(stageData.StageId))
        {
            Debug.LogError("[StageProgressService] ApplyResult 실패: StageDataSO 또는 StageId가 없습니다.", this);
            return new StageProgressApplyResult(null, 0, 0, false, false);
        }

        StageProgressRecord record = GetOrCreateRecord(stageData.StageId);

        int previousStarMask = record.starMask;
        bool wasCleared = record.isCleared;

        bool progressChanged = false;

        if (result.Cleared)
        {
            int earnedStarMask = StageStarEvaluator.EvaluateStarMask(stageData, result);
            int mergedStarMask = record.starMask | earnedStarMask;

            record.isCleared = true;
            record.clearCount++;

            if (mergedStarMask != record.starMask)
            {
                record.starMask = mergedStarMask;
                progressChanged = true;
            }

            if (result.BaseHpRate > record.bestBaseHpRate)
            {
                record.bestBaseHpRate = result.BaseHpRate;
                progressChanged = true;
            }

            if (result.ElapsedTime > 0f && (record.bestClearTime < 0f || result.ElapsedTime < record.bestClearTime))
            {
                record.bestClearTime = result.ElapsedTime;
                progressChanged = true;
            }

            if (!wasCleared)
                progressChanged = true;

            // clearCount도 기록이므로 클리어하면 저장 대상으로 본다.
            progressChanged = true;
        }

        if (progressChanged)
            Save();

        return new StageProgressApplyResult(
            record,
            previousStarMask,
            record.starMask,
            result.Cleared && !wasCleared,
            progressChanged);
    }

    #endregion

    #region 내부 유틸

    void EnsureLoaded()
    {
        if (!isLoaded)
            Load();

        saveData ??= new StageProgressSaveData();
        saveData.records ??= new List<StageProgressRecord>();
    }

    StageProgressRecord GetOrCreateRecord(string stageId)
    {
        StageProgressRecord record = GetRecord(stageId);

        if (record != null)
            return record;

        record = new StageProgressRecord { stageId = stageId };
        saveData.records.Add(record);

        return record;
    }

    void RemoveInvalidRecords()
    {
        saveData.records.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.stageId));
    }

    bool IsFirstStage(StageDataSO stageData)
    {
        if (!ResolveStageDatabaseService()) return false;

        StageDataSO firstStage = stageDatabaseService.GetFirstStage();

        return firstStage != null && firstStage.StageId == stageData.StageId;
    }

    bool ResolveSaveService()
    {
        if (saveService != null) return true;
        return ServiceLocator.TryGet(out saveService);
    }

    bool ResolveStageDatabaseService()
    {
        if (stageDatabaseService != null) return true;
        return ServiceLocator.TryGet(out stageDatabaseService);
    }

    #endregion
}