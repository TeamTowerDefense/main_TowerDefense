<<<<<<< Updated upstream
Ôªøusing IGameFlowInterface;
=======
using IGameFlowInterface;
using System.Collections;
>>>>>>> Stashed changes
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameResult : MonoBehaviour
{
    [Header("Ï∞∏Ï°∞")]
    [SerializeField] StageController stageController;
    [SerializeField] GameObject panelRoot;
    [SerializeField] CanvasGroup canvasGroup;

<<<<<<< Updated upstream
    [Header("ÌÖçÏä§Ìä∏")]
=======
    [Header("∫∞ ø¨√‚")]
    [SerializeField] GameObject[] starObjects;
    [SerializeField] float starDelay = 0.35f;

    [Header("≈ÿΩ∫∆Æ")]
>>>>>>> Stashed changes
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI killText;
    [SerializeField] TextMeshProUGUI leakText;
    [SerializeField] TextMeshProUGUI starText;

    [Header("Î≥Ñ Ï°∞Í±¥ Î™©Î°ù")]
    [SerializeField] Transform starConditionRoot;
    [SerializeField] UI_StageStarConditionRow starConditionRowPrefab;
    [SerializeField] bool showFallbackConditions = true;

    [Header("Î≤ÑÌäº")]
    [SerializeField] Button retryButton;
    [SerializeField] Button lobbyButton;
    [SerializeField] Button stageSelectButton;

    [Header("ÏòµÏÖò")]
    [SerializeField] bool pauseOnShow = true;
    [SerializeField] string clearTitle = "STAGE CLEAR";
    [SerializeField] string failTitle = "GAME OVER";


    #region ÌïÑÎìú

    readonly List<UI_StageStarConditionRow> spawnedRows = new();

    IGameFlowService gameFlowService;
    StageController subscribedController;
    StageResultContext currentResult;

    bool hasResult;
    bool pausedByPanel;
    float previousTimeScale = 1f;

    #endregion

    #region ÏÉùÎ™ÖÏ£ºÍ∏∞

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

    #region Íµ¨ÎèÖ

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

    #region ÌëúÏãú

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
<<<<<<< Updated upstream
        if (resultText) resultText.text = result.Cleared ? "ÌÅ¥Î¶¨Ïñ¥ ÏÑ±Í≥µ" : "Ïä§ÌÖåÏù¥ÏßÄ Ïã§Ìå®";
        if (hpText) hpText.text = $"Í∏∞ÏßÄ Ï≤¥Î†•:\n{result.CurrentBaseHp} / {result.MaxBaseHp}";
        if (timeText) timeText.text = $"ÏßÑÌñâ ÏãúÍ∞Ñ: {FormatTime(result.ElapsedTime)}";
        if (killText) killText.text = $"Ï≤òÏπò Ïàò: {result.KilledEnemyCount}";
        if (leakText) leakText.text = $"ÎàÑÏàò Ïàò: {result.LeakedEnemyCount}";
        if (starText) starText.text = $"ÌöçÎìù Î≥Ñ: {earnedStarCount} / {maxStarCount}";
=======
        if (resultText) resultText.text = result.Cleared ? "≈¨∏ÆæÓ º∫∞¯" : "Ω∫≈◊¿Ã¡ˆ Ω«∆–";
        if (hpText) hpText.text = $"{result.CurrentBaseHp}";
        if (timeText) timeText.text = $"{FormatTime(result.ElapsedTime)}";
        if (killText) killText.text = $"{result.KilledEnemyCount}";
        if (leakText) leakText.text = $"{result.LeakedEnemyCount}";

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateStarsRoutine(earnedStarCount));
        }
>>>>>>> Stashed changes

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
        CreateStarConditionRow("Ïä§ÌÖåÏù¥ÏßÄ ÌÅ¥Î¶¨Ïñ¥", "Ïä§ÌÖåÏù¥ÏßÄÎ•º ÌÅ¥Î¶¨Ïñ¥Ìï©ÎãàÎã§.", (starMask & (1 << 0)) != 0);
        CreateStarConditionRow("Í∏∞ÏßÄ Ï≤¥Î†• 50% Ïù¥ÏÉÅ", "ÌÅ¥Î¶¨Ïñ¥ Ïãú Í∏∞ÏßÄ Ï≤¥Î†•ÏùÑ 50% Ïù¥ÏÉÅ Ïú†ÏßÄÌï©ÎãàÎã§.", (starMask & (1 << 1)) != 0);
        CreateStarConditionRow("Í∏∞ÏßÄ Ï≤¥Î†• 100% Ïú†ÏßÄ", "ÌÅ¥Î¶¨Ïñ¥ Ïãú Í∏∞ÏßÄ Ï≤¥Î†•ÏùÑ Î™®Îëê Ïú†ÏßÄÌï©ÎãàÎã§.", (starMask & (1 << 2)) != 0);
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

    #region Î≤ÑÌäº

    void OnClickRetry()
    {
        RestoreTimeScale();
        SetVisible(false);

        ResolveServices();

        if (gameFlowService == null)
        {
            Debug.LogError("[StageResultTempPanel] IGameFlowServiceÎ•º Ï∞æÏßÄ Î™ªÌï¥ Ïû¨ÎèÑÏ†ÑÏùÑ Ïã§ÌñâÌï† Ïàò ÏóÜÏäµÎãàÎã§.", this);
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
            Debug.LogWarning("[StageResultTempPanel] Ï†úÏ∂úÌï† StageResultÍ∞Ä ÏóÜÏäµÎãàÎã§.", this);
            return;
        }

        RestoreTimeScale();
        SetVisible(false);

        ResolveServices();

        if (gameFlowService == null)
        {
            Debug.LogError("[StageResultTempPanel] IGameFlowServiceÎ•º Ï∞æÏßÄ Î™ªÌï¥ Í≤∞Í≥ºÎ•º Ï†úÏ∂úÌï† Ïàò ÏóÜÏäµÎãàÎã§.", this);
            return;
        }

        gameFlowService.FinishStageRun(currentResult, request);
    }

    #endregion

    #region ÏãúÍ∞Ñ

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

    #region Ïú†Ìã∏

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

    #region ∫∞ ø¨√‚ (æ÷¥œ∏ﬁ¿Ãº«)

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
            // TimeScale¿Ã 0¿œ ∂ßµµ æ÷¥œ∏ﬁ¿Ãº«¿Ã ¿Áª˝µ«µµ∑œ unscaledDeltaTime ªÁøÎ!
            time += Time.unscaledDeltaTime;
            float t = time / duration;

            if (t < 0.6f)
            {
                // æ’¿« 60% Ω√∞£ µøæ»¿∫ ø¯∑° ≈©±‚¿« 1.3πË±Ó¡ˆ ∆¢æÓ ø¿∏ß
                target.localScale = Vector3.Lerp(Vector3.zero, defaultScale * 1.3f, t / 0.6f);
            }
            else
            {
                // ≥™∏”¡ˆ 40% Ω√∞£ µøæ» 1.3πËø°º≠ ø¯∑° ≈©±‚(1.0)∑Œ æ»¬¯
                target.localScale = Vector3.Lerp(defaultScale * 1.3f, defaultScale, (t - 0.6f) / 0.4f);
            }

            yield return null;
        }

        // ø¿¬˜ πÊ¡ˆ∏¶ ¿ß«ÿ √÷¡æ Ω∫ƒ…¿œ ∞Ì¡§
        target.localScale = defaultScale;
    }

    #endregion
}
