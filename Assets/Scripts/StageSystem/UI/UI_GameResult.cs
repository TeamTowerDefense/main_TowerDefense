using IGameFlowInterface;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameResult : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] StageController stageController;
    [SerializeField] GameObject panelRoot;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("별 연출")]
    [SerializeField] GameObject[] starObjects;
    [SerializeField] float starDelay = 0.35f;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI killText;
    [SerializeField] TextMeshProUGUI leakText;
    [SerializeField] TextMeshProUGUI starText;

    [Header("별 조건 목록")]
    [SerializeField] Transform starConditionRoot;
    [SerializeField] UI_StageStarConditionRow starConditionRowPrefab;
    [SerializeField] bool showFallbackConditions = true;

    [Header("버튼")]
    [SerializeField] Button retryButton;
    [SerializeField] Button lobbyButton;
    [SerializeField] Button stageSelectButton;

    [Header("옵션")]
    [SerializeField] bool pauseOnShow = true;
    [SerializeField] string clearTitle = "STAGE CLEAR";
    [SerializeField] string failTitle = "GAME OVER";


    #region 필드

    readonly List<UI_StageStarConditionRow> spawnedRows = new();

    IGameFlowService gameFlowService;
    StageController subscribedController;
    StageResultContext currentResult;

    bool hasResult;
    bool pausedByPanel;
    float previousTimeScale = 1f;

    #endregion

    #region 생명주기

    void Awake()
    {
        if (!panelRoot) panelRoot = gameObject;
        if (!canvasGroup) canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        retryButton?.onClick.AddListener(OnClickRetry);
        lobbyButton?.onClick.AddListener(OnClickLobby);
        stageSelectButton?.onClick.AddListener(OnClickStageSelect);

        if (starConditionRowPrefab)
            starConditionRowPrefab.gameObject.SetActive(false);

        ResolveServices();
        SetVisible(false);
    }

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        ResolveServices();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
        RestoreTimeScale();
    }

    void OnDestroy()
    {
        retryButton?.onClick.RemoveListener(OnClickRetry);
        lobbyButton?.onClick.RemoveListener(OnClickLobby);
        stageSelectButton?.onClick.RemoveListener(OnClickStageSelect);
    }

    #endregion

    #region 구독

    void Subscribe()
    {
        if (!stageController)
            stageController = FindAnyObjectByType<StageController>();

        if (subscribedController == stageController) return;

        Unsubscribe();

        subscribedController = stageController;

        if (subscribedController != null)
            subscribedController.StageResultCreated += Show;
    }

    void Unsubscribe()
    {
        if (subscribedController == null) return;

        subscribedController.StageResultCreated -= Show;
        subscribedController = null;
    }

    #endregion

    #region 표시

    public void Show(StageResultContext result)
    {
        currentResult = result;
        hasResult = true;

        Refresh(result);
        SetVisible(true);

        if (pauseOnShow)
            PauseTime();
    }

    void Refresh(StageResultContext result)
    {
        StageDataSO stageData = stageController != null ? stageController.CurrentStageData : null;

        int starMask = StageStarEvaluator.EvaluateStarMask(stageData, result);
        int earnedStarCount = StageStarEvaluator.CountStars(starMask);
        int maxStarCount = StageStarEvaluator.GetMaxStarCount(stageData);

        if (titleText) titleText.text = result.Cleared ? clearTitle : failTitle;
        if (resultText) resultText.text = result.Cleared ? "클리어 성공" : "스테이지 실패";
        if (hpText) hpText.text = $"{result.CurrentBaseHp}";
        if (timeText) timeText.text = $"{FormatTime(result.ElapsedTime)}";
        if (killText) killText.text = $"{result.KilledEnemyCount}";
        if (leakText) leakText.text = $"{result.LeakedEnemyCount}";

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateStarsRoutine(earnedStarCount));
        }

        RefreshStarConditions(stageData, result, starMask);

        if (retryButton)
            retryButton.gameObject.SetActive(!result.Cleared);
    }

    void RefreshStarConditions(StageDataSO stageData, StageResultContext result, int starMask)
    {
        ClearStarConditionRows();

        if (!starConditionRoot || !starConditionRowPrefab) return;

        if (StageStarEvaluator.HasCustomConditions(stageData))
        {
            int count = Mathf.Min(stageData.StarConditions.Count, 32);

            for (int i = 0; i < count; i++)
            {
                StageStarConditionSO condition = stageData.StarConditions[i];
                bool achieved = (starMask & (1 << i)) != 0;

                CreateStarConditionRow(condition, achieved);
            }

            return;
        }

        if (showFallbackConditions)
            CreateFallbackStarConditionRows(result, starMask);
    }

    void CreateStarConditionRow(StageStarConditionSO condition, bool achieved)
    {
        UI_StageStarConditionRow row = Instantiate(starConditionRowPrefab, starConditionRoot);
        row.gameObject.SetActive(true);
        row.Set(condition, achieved);
        spawnedRows.Add(row);
    }

    void CreateFallbackStarConditionRows(StageResultContext result, int starMask)
    {
        CreateStarConditionRow("스테이지 클리어", "스테이지를 클리어합니다.", (starMask & (1 << 0)) != 0);
        CreateStarConditionRow("기지 체력 50% 이상", "클리어 시 기지 체력을 50% 이상 유지합니다.", (starMask & (1 << 1)) != 0);
        CreateStarConditionRow("기지 체력 100% 유지", "클리어 시 기지 체력을 모두 유지합니다.", (starMask & (1 << 2)) != 0);
    }

    void CreateStarConditionRow(string displayName, string description, bool achieved)
    {
        UI_StageStarConditionRow row = Instantiate(starConditionRowPrefab, starConditionRoot);
        row.gameObject.SetActive(true);
        row.Set(displayName, description, achieved);
        spawnedRows.Add(row);
    }

    void ClearStarConditionRows()
    {
        for (int i = spawnedRows.Count - 1; i >= 0; i--)
            if (spawnedRows[i]) Destroy(spawnedRows[i].gameObject);

        spawnedRows.Clear();
    }

    void SetVisible(bool visible)
    {
        if (panelRoot) panelRoot.SetActive(true);

        if (canvasGroup == null)
        {
            if (panelRoot) panelRoot.SetActive(visible);
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    #endregion

    #region 버튼

    void OnClickRetry()
    {
        RestoreTimeScale();
        SetVisible(false);

        ResolveServices();

        if (gameFlowService == null)
        {
            Debug.LogError("[StageResultTempPanel] IGameFlowService를 찾지 못해 재도전을 실행할 수 없습니다.", this);
            return;
        }

        gameFlowService.RetryStage();
    }

    void OnClickLobby()
    {
        SubmitAndReturn(LobbyOpenRequest.None);
    }

    void OnClickStageSelect()
    {
        SubmitAndReturn(LobbyOpenRequest.StageSelect);
    }

    void SubmitAndReturn(LobbyOpenRequest request)
    {
        if (!hasResult)
        {
            Debug.LogWarning("[StageResultTempPanel] 제출할 StageResult가 없습니다.", this);
            return;
        }

        RestoreTimeScale();
        SetVisible(false);

        ResolveServices();

        if (gameFlowService == null)
        {
            Debug.LogError("[StageResultTempPanel] IGameFlowService를 찾지 못해 결과를 제출할 수 없습니다.", this);
            return;
        }

        gameFlowService.FinishStageRun(currentResult, request);
    }


    #endregion

    #region 시간

    void PauseTime()
    {
        if (pausedByPanel) return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedByPanel = true;
    }

    void RestoreTimeScale()
    {
        if (!pausedByPanel) return;

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        pausedByPanel = false;
    }

    #endregion

    #region 유틸

    void ResolveServices()
    {
        if (gameFlowService == null)
            ServiceLocator.TryGet(out gameFlowService);
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int min = total / 60;
        int sec = total % 60;

        return $"{min:00}:{sec:00}";
    }

    #endregion

    #region 별 연출 (애니메이션)

    private IEnumerator AnimateStarsRoutine(int earnedStarCount)
    {
        foreach (var star in starObjects)
        {
            if (star != null)
            {
                star.SetActive(false);
                star.transform.localScale = Vector3.zero;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        for (int i = 0; i < earnedStarCount; i++)
        {
            if (i >= starObjects.Length || starObjects[i] == null) break;

            GameObject star = starObjects[i];
            star.SetActive(true);

            yield return StartCoroutine(PopScaleRoutine(star.transform));

            yield return new WaitForSecondsRealtime(starDelay);
        }
    }

    private IEnumerator PopScaleRoutine(Transform target)
    {
        float duration = 0.3f;
        float time = 0f;
        Vector3 defaultScale = Vector3.one;

        while (time < duration)
        {
            // TimeScale이 0일 때도 애니메이션이 재생되도록 unscaledDeltaTime 사용!
            time += Time.unscaledDeltaTime;
            float t = time / duration;

            if (t < 0.6f)
            {
                // 앞의 60% 시간 동안은 원래 크기의 1.3배까지 튀어 오름
                target.localScale = Vector3.Lerp(Vector3.zero, defaultScale * 1.3f, t / 0.6f);
            }
            else
            {
                // 나머지 40% 시간 동안 1.3배에서 원래 크기(1.0)로 안착
                target.localScale = Vector3.Lerp(defaultScale * 1.3f, defaultScale, (t - 0.6f) / 0.4f);
            }

            yield return null;
        }

        // 오차 방지를 위해 최종 스케일 고정
        target.localScale = defaultScale;
    }

    #endregion
}
