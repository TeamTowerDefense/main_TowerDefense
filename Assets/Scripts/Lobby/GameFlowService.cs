using IGameFlowInterface;
using System;
using UnityEngine;

public class GameFlowService : GlobalServiceBase, IGameFlowService
{
    [Header("씬 이름")]
    [SerializeField] string titleSceneName = "Title";
    [SerializeField] string lobbySceneName = "Lobby";

    [Header("로딩 사용")]
    [SerializeField] bool useLoadingForTitle = false;
    [SerializeField] bool useLoadingForLobby = true;
    [SerializeField] bool useLoadingForStage = true;

    [Header("로그")]
    [SerializeField] bool logOption = true;

    public event Action StageSelectOpenRequested;
    public event Action<StageResultContext, StageProgressApplyResult> StageRunFinished;

    #region 인터페이스
    public void EnterTitle()
    {
        if (logOption) Debug.Log("[GameFlowService] EnterTitle", this);

        LoadScene(titleSceneName, useLoadingForTitle);
    }

    public void EnterLobby()
    {
        if (logOption) Debug.Log("[GameFlowService] EnterLobby", this);

        LoadScene(lobbySceneName, useLoadingForLobby);
    }

    public void OpenStageSelectFromLobby()
    {
        if (logOption) Debug.Log("[GameFlowService] OpenStageSelectFromLobby", this);

        StageSelectOpenRequested?.Invoke();
    }

    public void StartStageRun()
    {
        if (!Resolve(out IStageRunContextService runContext)) return;

        if (!runContext.HasValidRun)
        {
            Debug.LogError("[GameFlowService] 시작 가능한 StageRunContext가 없습니다.", this);
            return;
        }

        if (logOption) Debug.Log($"[GameFlowService] StartStageRun: {runContext.StageId}", this);

        LoadScene(runContext.BattleSceneName, useLoadingForStage);
    }

    public void RetryStage()
    {
        if (!Resolve(out IStageRunContextService runContext)) return;

        if (!runContext.HasValidRun)
        {
            Debug.LogError("[GameFlowService] 재시도 가능한 StageRunContext가 없습니다.", this);
            return;
        }

        if (logOption) Debug.Log($"[GameFlowService] RetryStage: {runContext.StageId}", this);

        LoadScene(runContext.BattleSceneName, useLoadingForStage);
    }

    public void ExitToLobby()
    {
        if (logOption) Debug.Log("[GameFlowService] ExitToLobby", this);

        if (ServiceLocator.TryGet(out IStageRunContextService runContext)) runContext.Clear();

        LoadScene(lobbySceneName, useLoadingForLobby);
    }

    public void ExitToLobbyWithStageSelect()
    {
        if (logOption) Debug.Log("[GameFlowService] ExitToLobbyWithStageSelect", this);

        if (ServiceLocator.TryGet(out ILobbyReturnContextService lobbyReturnContext))
            lobbyReturnContext.Request(LobbyOpenRequest.StageSelect);

        if (ServiceLocator.TryGet(out IStageRunContextService runContext)) runContext.Clear();

        LoadScene(lobbySceneName, useLoadingForLobby);
    }
    public void FinishStageRun(StageResultContext result, LobbyOpenRequest lobbyRequest = LobbyOpenRequest.StageSelect)
    {
        if (!Resolve(out IStageRunContextService runContext)) return;

        StageDataSO stageData = runContext.StageData;

        if (stageData == null && ServiceLocator.TryGet(out IStageDatabaseService database))
            database.TryGetStage(result.StageId, out stageData);

        StageProgressApplyResult applyResult = default;

        if (stageData != null && ServiceLocator.TryGet(out IStageProgressService progressService))
            applyResult = progressService.ApplyResult(stageData, result);
        else
            Debug.LogWarning($"[GameFlowService] StageResult 저장 생략, StageData/StageProgressService 없음", this);

        StageRunFinished?.Invoke(result, applyResult);

        if (ServiceLocator.TryGet(out ILobbyReturnContextService lobbyReturnContext) && lobbyRequest != LobbyOpenRequest.None)
            lobbyReturnContext.Request(lobbyRequest);

        runContext.Clear();

        if (logOption) Debug.Log($"[GameFlowService] FinishStageRun: {result.StageId} / Cleared: {result.Cleared}", this);
        LoadScene(lobbySceneName, useLoadingForLobby);
    }

    #endregion

    #region 내부 유틸

    void LoadScene(string sceneName, bool useLoading)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[GameFlowService] 씬 이름이 비어 있습니다.", this);
            return;
        }

        if (!Resolve(out ISceneLoadService sceneLoadService)) return;

        if (useLoading)
            sceneLoadService.LoadSceneWithLoading(sceneName);
        else
            sceneLoadService.LoadScene(sceneName);
    }

    bool Resolve<T>(out T service) where T : class
    {
        if (ServiceLocator.TryGet(out service)) return true;

        Debug.LogError($"[GameFlowService] {typeof(T).Name} 서비스를 찾지 못했습니다.", this);
        return false;
    }

    #endregion
}
