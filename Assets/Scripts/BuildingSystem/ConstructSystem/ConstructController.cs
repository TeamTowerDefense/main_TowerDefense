using System.Collections.Generic;
using System;
using UnityEngine;
using IGameInterface;

public class ConstructController : MonoBehaviour
{
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IResourceSystem> resourceInterfaceRef;
    public IResourceSystem resourceSystem;
    [SerializeField]
    private InterfaceReference<IStageService> stageInterfactRef;
    public IStageService stageService;
    public BuildSystem buildSystem;

    [Header("셀 검사 레이어")]
    [SerializeField]
    private LayerMask obstacleLayer;

    [Header("타워 프리팹 목록")]
    [SerializeField] private BuildingData[] BuildingDeck;

    [Header("퀵슬롯 뷰")]
    [SerializeField]
    private UI_TowerQuickSlot quickSlotView;

    public ConstructModel Model { get; private set; }
    public ConstructView View { get; private set; }

    private Dictionary<Type, IConstructMode> modeDict = new Dictionary<Type, IConstructMode>();
    private IConstructMode currentMode;
    private bool isConstructMode = false;
    private Collider lastHitCollider;

    private void Awake()
    {
        Model = new ConstructModel();
        Model.ObstacleLayer = obstacleLayer;
        Model.buildingDatas = BuildingDeck;

        View = GetComponent<ConstructView>();
    }

    private void Start()
    {
        InitController();
    }

    private void Update()
    {
        if (isConstructMode) currentMode?.OnUpdate();
    }

    private void OnDestroy()
    {
        if (quickSlotView != null)
        {
            quickSlotView.OnSlotSelected -= SelectBuildingByIndex;
        }

        if (View != null && View.towerInteractUI != null)
        {
            View.towerInteractUI.OnDestroyClicked -= PerformCurModeSubAction;
        }
    }

    private void InitController()
    {
        AddStates();

        if (stageInterfactRef != null)
        {
            stageService = stageInterfactRef.Value;
        }

        if (resourceInterfaceRef != null)
        {
            resourceSystem = resourceInterfaceRef.Value;
        }

        if (quickSlotView != null)
        {
            quickSlotView.SetupUI(Model.buildingDatas, resourceSystem);
            quickSlotView.OnSlotSelected += SelectBuildingByIndex;
        }

        if (View.towerInteractUI != null)
        {
            View.towerInteractUI.OnDestroyClicked += PerformCurModeSubAction;
        }


        if (stageService != null && buildSystem != null)
        {
            int maxLimitFromStage = stageService.TowerLimit;
            Debug.Log(maxLimitFromStage);
            buildSystem.SetTowerLimit(10);
        }

        SetConstructMode(true);
    }

    public void UpdateRayHitInfo(bool isRayHitted, RaycastHit rayHitInfo)
    {
        if (isRayHitted)
        {
            Model.CurrentHit = rayHitInfo;
            if (rayHitInfo.collider != lastHitCollider)
            {
                lastHitCollider = rayHitInfo.collider;
                Model.CurrentGrid = lastHitCollider.GetComponentInParent<IGridProvider>();
                Model.HoveredTower = lastHitCollider.GetComponentInParent<IBuildable>();
            }
        }
        else
        {
            Model.CurrentHit = default;
            Model.CurrentGrid = null;
            Model.HoveredTower = null;
            lastHitCollider = null;
        }
    }


    public void SelectBuildingByIndex(int slotIndex)
    {
        BuildingData[] buildings = Model.buildingDatas;

        if (buildings == null || slotIndex < 0 || slotIndex >= buildings.Length) return;
        if (buildings[slotIndex] == null) return;

        BuildingData selectedBuilding = Model.buildingDatas[slotIndex];

        if (!resourceSystem.CanAfford(selectedBuilding.cost))
        {
            return;
        }

        bool buildingSelected = SelectBuilding(selectedBuilding);
        if (buildingSelected)
        {
            quickSlotView.UpdateHighlight(slotIndex);
        }
    }

    public bool SelectBuilding(BuildingData buildingData)
    {
        if (buildingData == null || !isConstructMode) 
            return false;
        if(buildingData.buildingPrefab == null)
            return false;
        if(!buildingData.buildingPrefab.TryGetComponent(out IBuildable prefabData))
            return false;

        // Model에 데이터 저장 후 상태 전환
        Model.BuildingData = buildingData;
        Model.PrefabToBuild = buildingData.buildingPrefab;
        Model.PrefabData = prefabData;

        ChangeState<BuildState>();

        return true;
    }

    private void AddStates()
    {
        modeDict.Add(typeof(IdleState), new IdleState(this));
        modeDict.Add(typeof(BuildState), new BuildState(this));
        modeDict.Add(typeof(SelectState), new SelectState(this));
    }

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
    public void PerformCurModeSubAction()
    {
        Debug.Log("SubAsction");
        currentMode?.PerformSubAction();
    }

    public void ClearSlotHighlight()
    {
        if (quickSlotView != null)
        {
            quickSlotView.ClearHighlight();
        }
    }
}
