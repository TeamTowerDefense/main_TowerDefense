using System.Collections.Generic;
using System;
using UnityEngine;
using IGameInterface;

public class ConstructController : MonoBehaviour
{
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IResourceSystem> resourceInterfaceRef;
    [SerializeField]
    private InterfaceReference<IStageService> stageInterfactRef;
    public BuildSystem buildSystem;

    public IResourceSystem resourceSystem {get; private set;}
    public IStageService stageService { get; private set;}

    [Header("셀 검사 레이어")]
    [SerializeField]
    private LayerMask obstacleLayer;

    [Header("타워 프리팹 목록")]
    [SerializeField] 
    private BuildingData[] buildingDeck;

    public ConstructModel Model { get; private set; }
    public ConstructView View { get; private set; }

    private Dictionary<Type, IConstructMode> modeDict = new Dictionary<Type, IConstructMode>();
    private IConstructMode currentMode;
    private bool isConstructMode = false;
    private Collider lastHitCollider;

    private void Awake()
    {
        InitInternal();
    }

    private void Start()
    {
        InitExternal();
    }

    private void Update()
    {
        if (isConstructMode) currentMode?.OnUpdate();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    #region 초기화 

    public void InitInternal()
    {
        InitializeMVC();
        InitializeStateMachine();
        Debug.Log("ConstructController: 내부 초기화 완료");
    }

    public void InitExternal()
    {
        BindExternalSystems();
        SetupUI();
        ApplyStageRules();
        SetConstructMode(true);
        Debug.Log("ConstructController: 외부 연동 완료");
    }

    private void InitializeMVC()
    {
        Model = new ConstructModel
        {
            ObstacleLayerMask = obstacleLayer,
            buildingDatas = buildingDeck
        };
        View = GetComponent<ConstructView>();
    }

    private void BindExternalSystems()
    {
        if (resourceInterfaceRef != null) resourceSystem = resourceInterfaceRef.Value;
        if (stageInterfactRef != null) stageService = stageInterfactRef.Value;
    }

    private void InitializeStateMachine()
    {
        modeDict.Clear();
        modeDict.Add(typeof(IdleState), new IdleState(this));
        modeDict.Add(typeof(BuildState), new BuildState(this));
        modeDict.Add(typeof(SelectState), new SelectState(this));
    }

    private void SetupUI()
    {
        View.InitializeQuickSlot(Model.buildingDatas, resourceSystem, SelectBuildingFromSlot);
        View.InitalizeTowerInteractUI(PerformCurModeSubAction);
    }

    private void UnbindEvents()
    {
        View.UnbindQuickSlot(SelectBuildingFromSlot);
        View.UnbindTowerInteractUI(PerformCurModeSubAction);
    }

    private void ApplyStageRules()
    {
        if (stageService != null && buildSystem != null)
        {
            int maxLimit = stageService.TowerLimit;
            buildSystem.SetTowerLimit(10); // 임시 수, 글로벌 이벤트 시스템 제작 후 적용
        }
    }
    #endregion

    #region 상태 머신 제어
    public void ChangeState<T>() where T : class, IConstructMode
    {
        if (!isConstructMode) return;

        if (modeDict.TryGetValue(typeof(T), out IConstructMode newMode))
        {
            currentMode?.OnExit();
            currentMode = newMode;
            currentMode?.OnEnter();
        }
    }

    public void SetConstructMode(bool isActivate)
    {
        isConstructMode = isActivate;
        if (isConstructMode) ChangeState<IdleState>();
        else { currentMode?.OnExit(); currentMode = null; }
    }

    public void PerformCurModeAction() => currentMode?.PerformMainAction();
    public void CancelCurModeAction() => currentMode?.CancelMainAction();
    public void PerformCurModeSubAction() => currentMode?.PerformSubAction();
    #endregion

    #region 입력 데이터 갱신
    public void UpdateRayHitInfo(bool isRayHitted, RaycastHit rayHitInfo)
    {
        if (isRayHitted)
        {
            Model.PointerHitInfo = rayHitInfo;
            if (rayHitInfo.collider != lastHitCollider)
            {
                lastHitCollider = rayHitInfo.collider;
                Model.TargetGrid = lastHitCollider.GetComponentInParent<IGridProvider>();
                Model.HoveredBuilding = lastHitCollider.GetComponentInParent<IBuildable>();
            }
        }
        else
        {
            ClearRayHitInfo();
        }
    }

    private void ClearRayHitInfo()
    {
        Model.PointerHitInfo = default;
        Model.TargetGrid = null;
        Model.HoveredBuilding = null;
        lastHitCollider = null;
    }

    #endregion

    #region 건축 로직

    public void SelectBuildingFromSlot(int slotIndex)
    {
        BuildingData[] deck = Model.buildingDatas;

        if (!IsValidSlotIndex(slotIndex, deck)) return;

        BuildingData selectedBuilding = deck[slotIndex];

        if (!CanSelectBuilding(selectedBuilding)) return;

        if (ApplyBuildingSelection(selectedBuilding))
        {
            View.UpdateQuickSlotHighlight(slotIndex);
        }
    }

    private bool IsValidSlotIndex(int index, BuildingData[] deck)
    {
        if (deck == null)
            return false;
        if (index < 0 || index >= deck.Length)
            return false;
        if (deck[index] == null)
            return false;

        return true;
    }

    private bool CanSelectBuilding(BuildingData data)
    {
        if (resourceSystem != null && !resourceSystem.CanAfford(data.cost))
        {
            Debug.LogWarning("자원이 부족합니다.");
            return false;
        }

        if (buildSystem != null && !buildSystem.CanBuildTower())
        {
            Debug.LogWarning("건설 최대 한도에 도달했습니다.");
            return false;
        }

        return true;
    }

    private bool ApplyBuildingSelection(BuildingData buildingData)
    {
        if (buildingData == null || !isConstructMode) 
            return false;
        if (buildingData.buildingPrefab == null) 
            return false;
        if (!buildingData.buildingPrefab.TryGetComponent(out IBuildable prefabData)) 
            return false;

        Model.DataToBuild = buildingData;
        Model.PrefabToBuild = buildingData.buildingPrefab;
        Model.BuildableToBuild = prefabData;

        ChangeState<BuildState>();
        return true;
    }

    #endregion 

    public void ClearSlotHighlight()
    {
        View.ClearQuickSlotHighlight();
    }
}
