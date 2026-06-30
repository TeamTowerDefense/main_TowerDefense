using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;

public class ConstructController : MonoBehaviour
{
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IResourceSystem> resourceInterfaceRef;
    public IResourceSystem resourceSystem;
    public BuildSystem buildSystem;

    [Header("셀 검사 레이어")]
    [SerializeField]
    private LayerMask obstacleLayer;

    [Header("타워 프리팹 목록")]
    [SerializeField] private BuildingData[] TowerDeck;

    [Header("퀵슬롯 뷰")]
    [SerializeField]
    private UI_TowerQuickSlot quickSlotView;

    public ConstructModel Model { get; private set; }
    public ConstructView View { get; private set; }

    private Dictionary<Type, IConstructMode> modeDict = new Dictionary<Type, IConstructMode>();
    private IConstructMode currentMode;
    private bool isConstructMod = false;
    private Collider lastHitCollider;

    private void Awake()
    {
        Model = new ConstructModel();
        Model.ObstacleLayer = obstacleLayer;
        Model.towerDatas = TowerDeck;

        View = GetComponent<ConstructView>();
    }

    private void Start()
    {
        modeDict.Add(typeof(IdleState), new IdleState(this));
        modeDict.Add(typeof(BuildState), new BuildState(this));
        modeDict.Add(typeof(SelectState), new SelectState(this));

        if(quickSlotView != null)
        {
            quickSlotView.SetupUI(Model.towerDatas);
            quickSlotView.OnSlotSelected += SelectBuildingByIndex;
        }

        if (View.towerInteractUI != null)
        {
            View.towerInteractUI.OnDestroyClicked += PerformCurModeSubAction;
        }

        if(resourceInterfaceRef != null)
        {
            resourceSystem = resourceInterfaceRef.Value;
        }

        SetConstructMod(true);
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

    private void Update()
    {
        if (isConstructMod) currentMode?.OnUpdate();
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
        BuildingData[] towers = Model.towerDatas;

        if (towers == null || slotIndex < 0 || slotIndex >= towers.Length) return;
        if (towers[slotIndex] == null) return;

        BuildingData selectedBuilding = Model.towerDatas[slotIndex];

        if (resourceSystem != null && !resourceSystem.CanAfford(selectedBuilding.cost))
        {
            Debug.LogWarning($"자원이 부족하여 {selectedBuilding.buildingName}을 선택할 수 없습니다.");
            return;
        }

        bool buildingSelected = SelectBuilding(selectedBuilding);
        if (buildingSelected)
        {
            quickSlotView.UpdateHighlight(slotIndex);
        }
    }

    public bool SelectBuilding(BuildingData towerData)
    {
        if (towerData == null || !isConstructMod) 
            return false;
        if(towerData.buildingPrefab == null)
            return false;
        if(!towerData.buildingPrefab.TryGetComponent(out IBuildable prefabData))
            return false;

        // Model에 데이터 저장 후 상태 전환
        Model.TowerData = towerData;
        Model.PrefabToBuild = towerData.buildingPrefab;
        Model.PrefabData = prefabData;

        ChangeState<BuildState>();

        return true;
    }

    public void ChangeState<T>() where T : class, IConstructMode
    {
        if (!isConstructMod) return;

        if (modeDict.TryGetValue(typeof(T), out IConstructMode newMode))
        {
            currentMode?.OnExit();
            currentMode = newMode;
            currentMode?.OnEnter();
        }
    }

    public void SetConstructMod(bool isActivate)
    {
        isConstructMod = isActivate;
        if (isConstructMod) ChangeState<IdleState>();
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
