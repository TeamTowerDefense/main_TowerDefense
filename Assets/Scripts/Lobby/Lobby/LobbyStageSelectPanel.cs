using IGameFlowInterface;
using IGameInterface;
using UnityEngine;
using UnityEngine.UI;

public class LobbyStageSelectPanel : MonoBehaviour
{
    [Header("팝업")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;

    [Header("스테이지 버튼")]
    [SerializeField] StageSelectEntryView[] entries;

    [Header("테스트 옵션")]
    [SerializeField] bool hideOnAwake = true;
    [SerializeField] bool startStageImmediatelyForTest = true;


    IGameFlowService gameFlowService;
    IStageRunContextService runContextService;
    IStageProgressService progressService;
    ILobbyReturnContextService lobbyReturnContext;

    #region 생명주기

    void Awake()
    {
        BindButtons();

        if (hideOnAwake && panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Start()
    {
        ResolveServices();
        BindEntryCallbacks();
        Refresh();

        if (lobbyReturnContext != null && lobbyReturnContext.Consume() == LobbyOpenRequest.StageSelect)
            Open();
    }

    void OnEnable()
    {
        ResolveServices();

        if (gameFlowService != null)
            gameFlowService.StageSelectOpenRequested += Open;
    }

    void OnDisable()
    {
        if (gameFlowService != null)
            gameFlowService.StageSelectOpenRequested -= Open;
    }

    void OnDestroy()
    {
        UnbindButtons();
    }

    #endregion

    #region 버튼 바인딩

    void BindButtons()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void UnbindButtons()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    void BindEntryCallbacks()
    {
        if (entries == null) return;

        foreach (StageSelectEntryView entry in entries)
        {
            if (entry == null) continue;
            entry.SetClickCallback(OnClickStage);
        }
    }

    #endregion

    #region 팝업

    public void Open()
    {
        ResolveServices();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        ResolveServices();

        if (entries == null) return;

        foreach (StageSelectEntryView entry in entries)
        {
            if (entry == null) continue;
            entry.Refresh(progressService);
        }
    }

    #endregion

    #region 스테이지 선택

    void OnClickStage(StageDataSO stageData)
    {
        if (stageData == null) return;

        ResolveServices();

        if (progressService != null && !progressService.IsStageUnlocked(stageData))
        {
            Debug.LogWarning($"[LobbyStageSelectPanel] 잠긴 스테이지입니다: {stageData.StageId}", this);
            Refresh();
            return;
        }

        if (runContextService == null)
        {
            Debug.LogError("[LobbyStageSelectPanel] IStageRunContextService를 찾지 못했습니다.", this);
            return;
        }

        runContextService.SetStage(stageData);
        runContextService.SetLoadout(new StageLoadoutContext());

        if (!startStageImmediatelyForTest)
        {
            Debug.Log($"[LobbyStageSelectPanel] 선택됨: {stageData.StageId}. 이후 PreBattleSetupPopup 연결 예정.", this);
            return;
        }

        if (gameFlowService == null)
        {
            Debug.LogError("[LobbyStageSelectPanel] IGameFlowService를 찾지 못했습니다.", this);
            return;
        }

        gameFlowService.StartStageRun();
    }

    #endregion

    #region 서비스

    void ResolveServices()
    {
        if (gameFlowService == null) ServiceLocator.TryGet(out gameFlowService);
        if (runContextService == null) ServiceLocator.TryGet(out runContextService);
        if (progressService == null) ServiceLocator.TryGet(out progressService);
        if (lobbyReturnContext == null) ServiceLocator.TryGet(out lobbyReturnContext);
    }

    #endregion
}