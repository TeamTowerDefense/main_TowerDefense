using IGameFlowInterface;
using IGameInterface;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class GameBootstrapper : MonoBehaviour
{
    #region 인스펙터

    [Header("시작 흐름")]
    [SerializeField] bool autoEnterTitleOnStart = true;
    [SerializeField] float startDelay = 0f;

    [Header("서비스 준비 대기")]
    [SerializeField] bool waitGlobalServices = true;
    [SerializeField] float serviceReadyTimeout = 5f;

    [Header("옵션")]
    [SerializeField] bool destroyAfterStartFlow = true;

    [Header("로그")]
    [SerializeField] bool logBootstrap = true;

    #endregion

    #region 생명주기
    IEnumerator Start()
    {
        yield return null;

        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

        if (waitGlobalServices) yield return WaitGlobalServicesReady();

        if (logBootstrap) Debug.Log("[GameBootstrapper] Bootstrap Ready", this);

        if (autoEnterTitleOnStart) EnterTitle();

        if (destroyAfterStartFlow)
            Destroy(gameObject);
    }

    #endregion

    #region 준비 대기

    IEnumerator WaitGlobalServicesReady()
    {
        float elapsed = 0f;

        while (elapsed < serviceReadyTimeout)
        {
            bool databaseReady = IsStageDatabaseReady();
            bool progressReady = IsStageProgressReady();

            if (databaseReady && progressReady)
            {
                if (logBootstrap) Debug.Log("[GameBootstrapper] Global Services Ready", this);

                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[GameBootstrapper] Global Service 준비 대기 시간이 초과되었습니다. 그래도 진행합니다.", this);
    }

    bool IsStageDatabaseReady()
    {
        if (!ServiceLocator.TryGet(out IStageDatabaseService databaseService)) return false;

        return databaseService.IsLoaded;
    }

    bool IsStageProgressReady()
    {
        if (!ServiceLocator.TryGet(out IStageProgressService progressService)) return false;

        return progressService.IsLoaded;
    }

    #endregion

    #region 흐름

    public void EnterTitle()
    {
        if (ServiceLocator.TryGet(out IGameFlowService gameFlowService))
        {
            gameFlowService.EnterTitle();
            return;
        }

        Debug.LogWarning("[GameBootstrapper] IGameFlowService를 찾지 못했습니다.", this);
    }

    #endregion
}