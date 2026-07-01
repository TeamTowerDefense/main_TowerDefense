using UnityEngine;

public class BuildState : IConstructMode
{
    private ConstructController controller;

    public BuildState(ConstructController controller) 
    {
        this.controller = controller; 
    }

    public void OnEnter()
    {
        Debug.Log("건설 상태 진입");

        // Model에서 프리팹 데이터를 꺼내서 View에게 프리뷰를 만들라고 지시
        if (controller.Model.BuildableToBuild != null)
        {
            if(controller.Model.BuildableToBuild is IHasPreview hasPreview)
            {
                controller.View.CreatePreview(hasPreview.GetPreview());
            }
        }
    }

    public void OnUpdate()
    {
        ConstructModel model = controller.Model;
        ConstructView view = controller.View;

        if (model.PointerHitInfo.collider == null || model.BuildableToBuild == null)
        {
            view.HidePreview();
            return;
        }

        if(model.HoveredBuilding != null)
        {
            view.HidePreview();
            return;
        }

        if (model.TargetGrid != null)
        {
            Vector3 rawPos = model.PointerHitInfo.point;
            // Model 데이터 갱신
            model.SnappedPosition = model.TargetGrid.GetCellCenterFromPoint(rawPos);
            model.IsPositionValid = CheckValidity(model.TargetGrid, model.SnappedPosition);
            Debug.Log(model.SnappedPosition);

            // View에 갱신 지시
            view.UpdatePreview(model.SnappedPosition, model.IsPositionValid);

            // 스냅된 위치에 그리드 셀이 존재하는지 확인
            bool isCellExist = model.TargetGrid.CheckGridInPoint(model.SnappedPosition);

            // 초기값은 false로 둠
            model.IsPositionValid = false;

            if (isCellExist)
            {
                bool canAfford = controller.resourceSystem == null || controller.resourceSystem.CanAfford(model.DataToBuild.cost);
                bool canBuild = controller.buildSystem == null || controller.buildSystem.CanBuildTower();
                bool isSpaceValid = CheckValidity(model.TargetGrid, model.SnappedPosition);

                Debug.Log(canAfford + ", " + canBuild + ", " + isSpaceValid);

                model.IsPositionValid = canAfford && canBuild && isSpaceValid;
            }

            // 유효하면 스냅 좌표, 아니면 원본 마우스 좌표 사용
            Vector3 previewRenderPos = model.IsPositionValid ? model.SnappedPosition : rawPos;

            // 계산된 위치에 프리뷰 렌더링 (빨강/초록)
            view.UpdatePreview(previewRenderPos, model.IsPositionValid);
        }
        else
        {
            view.HidePreview();
        }
    }

    public void PerformMainAction()
    {
        var model = controller.Model;

        if (model.TargetGrid != null && model.IsPositionValid)
        {
            Vector2Int curCellIndex = model.TargetGrid.GetCellIndex(model.SnappedPosition);
            Vector2 center = model.BuildableToBuild.GetCenter(model.TargetGrid.CellSize);
            Vector3 buildPos = model.SnappedPosition + new Vector3(center.x, 0, center.y);

            if(controller.resourceSystem != null && controller.resourceSystem.Spend(model.DataToBuild.cost))
            {
                // 건설 실행
                GameObject placedObj = controller.buildSystem.PlaceBuilding(model.PrefabToBuild, model.TargetGrid, curCellIndex, buildPos, Quaternion.identity);

                placedObj.GetComponent<IBuildable>().OnPlaced();
                model.TargetGrid.RegisterOccupancy(curCellIndex, model.BuildableToBuild.GetOccupiedOffsets(), true);

                controller.ChangeState<IdleState>(); // 지은 후 대기 상태로
            }
        }
    }

    public void CancelMainAction() => controller.ChangeState<IdleState>();
    public void PerformSubAction() { }

    public void OnExit()
    {
        controller.View.HidePreview();
    }

    // 내부 로직 (모델 데이터 검증)
    private bool CheckValidity(IGridProvider grid, Vector3 pos)
    {
        Vector2Int curCellIndex = grid.GetCellIndex(pos);
        foreach (var offset in controller.Model.BuildableToBuild.GetOccupiedOffsets())
        {
            if (!grid.CheckCellValid(curCellIndex + offset, controller.Model.ObstacleLayerMask))
                return false;
        }
        return true;
    }
}
