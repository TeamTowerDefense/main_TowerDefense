using System.Collections.Generic;
using IGameInterface;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour, ICameraService, IAutoSceneService
{
    [Header("카메라 모듈")]
    [SerializeField] CameraViewType defaultView = CameraViewType.TopView;
    [SerializeField] MonoBehaviour[] moduleBehaviours;

    [Header("입력")]
    [SerializeField] string inputMapName = InputMapKeys.TopView;
    [SerializeField] string moveActionName = InputActionKeys.Move;
    [SerializeField] string scrollActionName = InputActionKeys.Scroll;
    [SerializeField] string rotateActionName = InputActionKeys.PointerDelta;

    [Header("스테이지 입력 차단")]
    [SerializeField] bool blockInputOnStageEnd = true;

    [Header("우선도")]
    [SerializeField] int inactivePriority = 10;
    [SerializeField] int activePriority = 20;

    readonly Dictionary<CameraViewType, ICameraModule> modules = new();

    public CinemachineCamera CurrentCam => currentModule?.Camera;

    IInputService inputService;
    IStageService stageService;
    ICameraModule currentModule;
    bool inputBlockedByStage;

    public CameraViewType CurrentView { get; private set; }

    #region 생명 주기

    void Awake()
    {
        ((IAutoSceneService)this).RegisterSceneServices();
        CacheModules();
    }

    void Start()
    {
        ResolveServices();
        BindStageService();
        SetView(defaultView);
    }

    void Update()
    {
        ResolveServices();
        BindStageService();

        if (inputService == null || currentModule == null) return;
        if (inputBlockedByStage) return;

        Move(inputService.ReadVector2(inputMapName, moveActionName));
        Zoom(inputService.ReadVector2(inputMapName, scrollActionName).y);
        Rotate(inputService.ReadVector2(inputMapName, rotateActionName));
    }

    void OnDestroy()
    {
        UnbindStageService();
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    #endregion

    #region ICameraService

    public void SetView(CameraViewType viewType)
    {
        if (!modules.TryGetValue(viewType, out ICameraModule nextModule) || nextModule == null)
        {
            Debug.LogWarning($"[CameraController] {viewType} 카메라 모듈을 찾을 수 없습니다.", this);
            return;
        }

        currentModule?.Deactivate();
        currentModule = nextModule;
        CurrentView = viewType;

        currentModule.Activate();
        ApplyPriority(currentModule);
    }

    public void Move(Vector2 input) => currentModule?.Move(input);
    public void Zoom(float input) => currentModule?.Zoom(input);
    public void Rotate(Vector2 input) => currentModule?.Rotate(input);

    public void Focus(ICameraFocusTarget target, CameraViewType viewType = CameraViewType.TargetView)
    {
        SetView(viewType);

        if (currentModule is ICameraFocusModule focusModule)
            focusModule.Focus(target);
    }

    #endregion

    #region 스테이지 입력 차단

    void BindStageService()
    {
        if (!blockInputOnStageEnd || stageService != null) return;
        if (!ServiceLocator.TryGet(out stageService) || stageService == null) return;

        stageService.StateChanged -= OnStageStateChanged;
        stageService.StateChanged += OnStageStateChanged;

        ApplyStageInputBlock(stageService.CurrentState);
    }

    void UnbindStageService()
    {
        if (stageService == null) return;

        stageService.StateChanged -= OnStageStateChanged;
        stageService = null;
    }

    void OnStageStateChanged(StageState state)
    {
        ApplyStageInputBlock(state);
    }

    void ApplyStageInputBlock(StageState state)
    {
        inputBlockedByStage = blockInputOnStageEnd &&
            (state == StageState.StageClear || state == StageState.StageFailed);
    }

    #endregion

    #region 내부 함수

    void ResolveServices()
    {
        if (inputService == null) ServiceLocator.TryGet(out inputService);
    }

    void CacheModules()
    {
        modules.Clear();

        foreach (MonoBehaviour behaviour in moduleBehaviours)
        {
            if (behaviour is not ICameraModule module) continue;

            modules[module.ViewType] = module;
            SetPriority(module.Camera, inactivePriority);
            module.Deactivate();
        }
    }

    void ApplyPriority(ICameraModule activeModule)
    {
        foreach (ICameraModule module in modules.Values)
            SetPriority(module.Camera, module == activeModule ? activePriority : inactivePriority);
    }

    static void SetPriority(CinemachineCamera cam, int priority)
    {
        if (cam != null) cam.Priority = priority;
    }

    #endregion
}