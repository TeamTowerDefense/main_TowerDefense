using UnityEngine;

public class ConstructModel
{
    // 타워 리스트
    public BuildingData[] buildingDatas { get; set; }

    // [일반/선택 상태 데이터]
    public IBuildable HoveredTower { get; set; }
    public IBuildable SelectedTower { get; set; }

    // [건설 상태 데이터]
    public BuildingData BuildingData { get; set; }
    public GameObject PrefabToBuild { get; set; }
    public IBuildable PrefabData { get; set; }

    // [레이캐스트 및 그리드 데이터]
    public RaycastHit CurrentHit { get; set; }
    public IGridProvider CurrentGrid { get; set; }

    // [건설 위치 판별 데이터]
    public Vector3 SnappedPos { get; set; }
    public bool IsValidPosition { get; set; }
    public LayerMask ObstacleLayer { get; set; }
}
