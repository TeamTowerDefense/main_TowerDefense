using IGameFlowInterface;
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
        if (hpText) hpText.text = $"기지 체력:\n{result.CurrentBaseHp} / {result.MaxBaseHp}";
        if (timeText) timeText.text = $"진행 시간: {FormatTime(result.ElapsedTime)}";
        if (killText) killText.text = $"처치 수: {result.KilledEnemyCount}";
        if (leakText) leakText.text = $"누수 수: {result.LeakedEnemyCount}";
        if (starText) starText.text = $"획득 별: {earnedStarCount} / {maxStarCount}";

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
}
