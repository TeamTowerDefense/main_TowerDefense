using UnityEngine;

public class ConstructModel
{
    // 타워 리스트
    public BuildingData[] buildingDatas { get; set; }

    // [일반/선택 상태 데이터]
    public IBuildable HoveredBuilding { get; set; }
    public IBuildable SelectedBuilding { get; set; }

    // [건설 상태 데이터]
    public BuildingData DataToBuild { get; set; }
    public GameObject PrefabToBuild { get; set; }
    public IBuildable BuildableToBuild { get; set; }

    // [레이캐스트 및 그리드 데이터]
    public RaycastHit PointerHitInfo { get; set; }
    public IGridProvider TargetGrid { get; set; }

    // [건설 위치 판별 데이터]
    public Vector3 SnappedPosition { get; set; }
    public bool IsPositionValid { get; set; }
    public LayerMask ObstacleLayerMask { get; set; }
}
