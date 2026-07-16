using IGameFlowInterface;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyStageSelectPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;

    [Header("스테이지 전광판")]
    [SerializeField] LobbyStageInfoView stageInfoView;
    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button startButton;
    [SerializeField] TMP_Text stageIndexText;

    [Header("잠금 표시")]
    [SerializeField] GameObject lockRoot;
    [SerializeField] TMP_Text lockMessageText;

    [Header("초기 설정")]
    [SerializeField] bool hideOnAwake;

    readonly List<StageDataSO> stages = new();

    int selectedIndex = -1;
    bool initialized;

    IGameFlowService gameFlowService;
    IStageRunContextService runContextService;
    IStageProgressService progressService;
    IStageDatabaseService stageDatabaseService;
    ILobbyReturnContextService lobbyReturnContext;

    bool gameFlowSubscribed;
    bool databaseSubscribed;

    public StageDataSO SelectedStage =>
        selectedIndex >= 0 && selectedIndex < stages.Count
            ? stages[selectedIndex]
            : null;

    #region 생명주기

    void Awake()
    {
        BindButtons();

        if (hideOnAwake && panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        ResolveServices();
        SubscribeEvents();
    }

    void Start()
    {
        ResolveServices();
        SubscribeEvents();

        if (stageDatabaseService?.IsLoaded == true)
            InitializeStages();

        if (lobbyReturnContext != null &&
            lobbyReturnContext.Consume() == LobbyOpenRequest.StageSelect)
        {
            Open();
        }
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void OnDestroy()
    {
        UnbindButtons();
        UnsubscribeEvents();
    }

    #endregion

    #region 버튼

    void BindButtons()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (previousButton != null) previousButton.onClick.AddListener(SelectPrevious);
        if (nextButton != null) nextButton.onClick.AddListener(SelectNext);
        if (startButton != null) startButton.onClick.AddListener(StartSelectedStage);
    }

    void UnbindButtons()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (previousButton != null) previousButton.onClick.RemoveListener(SelectPrevious);
        if (nextButton != null) nextButton.onClick.RemoveListener(SelectNext);
        if (startButton != null) startButton.onClick.RemoveListener(StartSelectedStage);
    }

    public void SelectPrevious() => MoveSelection(-1);

    public void SelectNext() => MoveSelection(1);

    void MoveSelection(int direction)
    {
        if (stages.Count == 0) return;

        int nextIndex = Mathf.Clamp(
            selectedIndex + direction,
            0,
            stages.Count - 1);

        if (nextIndex == selectedIndex) return;

        selectedIndex = nextIndex;
        RefreshSelectedStage();
    }

    #endregion

    #region 패널

    public void Open()
    {
        ResolveServices();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (stageDatabaseService?.IsLoaded == true)
        {
            if (!initialized) InitializeStages();
            else RefreshSelectedStage();
        }
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        ResolveServices();

        if (stageDatabaseService?.IsLoaded != true)
        {
            RefreshEmpty();
            return;
        }

        if (!initialized)
            InitializeStages();
        else
            RefreshSelectedStage();
    }

    #endregion

    #region 스테이지 목록

    void OnStageDatabaseLoaded()
    {
        InitializeStages();
    }

    void InitializeStages()
    {
        ResolveServices();

        string previousStageId = SelectedStage != null
            ? SelectedStage.StageId
            : string.Empty;

        stages.Clear();

        if (stageDatabaseService?.Stages != null)
        {
            stages.AddRange(
                stageDatabaseService.Stages
                    .Where(stage => stage != null)
                    .OrderBy(stage => stage.LobbyOrder)
                    .ThenBy(stage => stage.StageId));
        }

        if (stages.Count == 0)
        {
            RefreshEmpty();
            return;
        }

        if (!initialized)
        {
            selectedIndex = FindLatestUnlockedIndex();
        }
        else
        {
            int previousIndex = stages.FindIndex(
                stage => stage.StageId == previousStageId);

            selectedIndex = previousIndex >= 0
                ? previousIndex
                : FindLatestUnlockedIndex();
        }

        initialized = true;
        RefreshSelectedStage();
    }

    int FindLatestUnlockedIndex()
    {
        for (int i = stages.Count - 1; i >= 0; i--)
        {
            if (IsUnlocked(stages[i]))
                return i;
        }

        return 0;
    }

    bool IsUnlocked(StageDataSO stageData) =>
        stageData != null &&
        (progressService == null ||
         progressService.IsStageUnlocked(stageData));

    #endregion

    #region 표시

    void RefreshSelectedStage()
    {
        StageDataSO stageData = SelectedStage;

        if (stageData == null)
        {
            RefreshEmpty();
            return;
        }

        bool unlocked = IsUnlocked(stageData);

        int starMask = progressService != null
            ? progressService.GetStarMask(stageData.StageId)
            : 0;

        if (stageInfoView != null)
            stageInfoView.Bind(stageData, starMask);

        if (stageIndexText != null)
            stageIndexText.text = $"{selectedIndex + 1} / {stages.Count}";

        if (lockRoot != null)
            lockRoot.SetActive(!unlocked);

        if (lockMessageText != null)
        {
            lockMessageText.text = unlocked
                ? string.Empty
                : "이전 스테이지를 클리어하면 해금됩니다.";
        }

        if (previousButton != null)
            previousButton.interactable = selectedIndex > 0;

        if (nextButton != null)
            nextButton.interactable = selectedIndex < stages.Count - 1;

        if (startButton != null)
            startButton.interactable = unlocked;
    }

    void RefreshEmpty()
    {
        selectedIndex = -1;

        if (stageInfoView != null)
            stageInfoView.Clear();

        if (stageIndexText != null)
            stageIndexText.text = "0 / 0";

        if (lockRoot != null)
            lockRoot.SetActive(false);

        if (previousButton != null)
            previousButton.interactable = false;

        if (nextButton != null)
            nextButton.interactable = false;

        if (startButton != null)
            startButton.interactable = false;
    }

    #endregion

    #region 스테이지 시작

    void StartSelectedStage()
    {
        StageDataSO stageData = SelectedStage;
        if (stageData == null) return;

        ResolveServices();

        if (!IsUnlocked(stageData))
        {
            Debug.LogWarning(
                $"[LobbyStageSelectPanel] 잠긴 스테이지입니다: {stageData.StageId}",
                this);

            RefreshSelectedStage();
            return;
        }

        if (runContextService == null)
        {
            Debug.LogError(
                "[LobbyStageSelectPanel] IStageRunContextService를 찾지 못했습니다.",
                this);

            return;
        }

        if (gameFlowService == null)
        {
            Debug.LogError(
                "[LobbyStageSelectPanel] IGameFlowService를 찾지 못했습니다.",
                this);

            return;
        }

        runContextService.SetStage(stageData);
        runContextService.SetLoadout(new StageLoadoutContext());

        gameFlowService.StartStageRun();
    }

    #endregion

    #region 서비스

    void ResolveServices()
    {
        if (gameFlowService == null)
            ServiceLocator.TryGet(out gameFlowService);

        if (runContextService == null)
            ServiceLocator.TryGet(out runContextService);

        if (progressService == null)
            ServiceLocator.TryGet(out progressService);

        if (stageDatabaseService == null)
            ServiceLocator.TryGet(out stageDatabaseService);

        if (lobbyReturnContext == null)
            ServiceLocator.TryGet(out lobbyReturnContext);
    }

    void SubscribeEvents()
    {
        if (!gameFlowSubscribed && gameFlowService != null)
        {
            gameFlowService.StageSelectOpenRequested += Open;
            gameFlowSubscribed = true;
        }

        if (!databaseSubscribed && stageDatabaseService != null)
        {
            stageDatabaseService.Loaded += OnStageDatabaseLoaded;
            databaseSubscribed = true;
        }
    }

    void UnsubscribeEvents()
    {
        if (gameFlowSubscribed && gameFlowService != null)
        {
            gameFlowService.StageSelectOpenRequested -= Open;
            gameFlowSubscribed = false;
        }

        if (databaseSubscribed && stageDatabaseService != null)
        {
            stageDatabaseService.Loaded -= OnStageDatabaseLoaded;
            databaseSubscribed = false;
        }
    }

    #endregion
}