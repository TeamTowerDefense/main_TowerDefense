using IGameFlowInterface;
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
    [SerializeField] TextMeshProUGUI starText;

    [Header("버튼")]
    [SerializeField] Button retryButton;
    [SerializeField] Button lobbyButton;
    [SerializeField] Button stageSelectButton;

    [Header("옵션")]
    [SerializeField] bool pauseOnShow = true;
    [SerializeField] string clearTitle = "STAGE CLEAR";
    [SerializeField] string failTitle = "GAME OVER";

    IGameFlowService gameFlowService;
    StageController subscribedController;
    StageResultContext currentResult;
    bool hasResult;
    bool pausedByPanel;
    float previouseTimeScale = 1f;

    #region 생명주기
    private void Awake()
    {
        if (!panelRoot) panelRoot = gameObject;
        if (!canvasGroup) canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        retryButton?.onClick.AddListener(OnClickRetry);
        lobbyButton?.onClick.AddListener(OnClickLobby);
        stageSelectButton?.onClick.AddListener(OnClickStageSelect);

        ResolveServices();
        SetVisible(false);
    }
    #endregion

    #region 표시
    public void Show(StageResultContext result)
    {
        currentResult = result;
        hasResult = true;

        Refresh(result);
        SetVisible(true);

        if (pauseOnShow) PauseTime();
    }
    void Refresh(StageResultContext result)
    {
        StageDataSO stageData = stageController != null ? stageController.CurrentStageData : null;
        int starMask = StageStarEvaluator.EvaluateStarMask(stageData, result);
        int earnedStarCount = StageStarEvaluator.CountStars(starMask);
        int maxStarCount = stageData?.StarConditions?.Count > 0 ? Mathf.Min(stageData.StarConditions.Count, 32) : 3;

        if (titleText) titleText.text = result.Cleared ? clearTitle : failTitle;
        if (resultText) resultText.text = result.Cleared ? "클리어 성공" : "스테이지 실패";
        if (hpText) hpText.text = $"기지 체력: {result.CurrentBaseHp} / {result.MaxBaseHp}";
        if (timeText) timeText.text = $"진행 시간: {FormatTime(result.ElapsedTime)}";
        if (killText) killText.text = $"처치 수: {result.KilledEnemyCount}";
        if (starText) starText.text = result.Cleared ? $"획득 별: {earnedStarCount} / 3" : "획득 별: 0 / 3";

        if (retryButton) retryButton.gameObject.SetActive(!result.Cleared);
    }
    void SetVisible(bool visible)
    {
        if (panelRoot) panelRoot.SetActive(true);
        if (canvasGroup == null)
        {
            if (panelRoot) panelRoot.SetActive(visible);
            return;
        }

        canvasGroup.alpha = visible ? 1 : 0;
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
    void OnClickLobby() => SubminAndReturn(LobbyOpenRequest.None);
    void OnClickStageSelect() => SubminAndReturn(LobbyOpenRequest.StageSelect);
    void SubminAndReturn(LobbyOpenRequest request)
    {
        if (!hasResult)
        {
            Debug.LogWarning($"[StageResulPanel] 제출할 StageResult가 없습니다", this);
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

    #region 시간 제어
    void PauseTime()
    {
        if (pausedByPanel) return;

        previouseTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedByPanel = true;
    }
    void RestoreTimeScale()
    {
        if (!pausedByPanel) return;

        Time.timeScale = previouseTimeScale <= 0f ? 1f : previouseTimeScale;
        pausedByPanel = false;
    }
    #endregion

    #region 유틸
    void ResolveServices()
    {
        if (gameFlowService == null) ServiceLocator.TryGet(out gameFlowService);
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
