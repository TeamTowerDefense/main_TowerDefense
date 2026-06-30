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
        if (controller.Model.PrefabData != null)
        {
            if(controller.Model.PrefabData is IHasPreview hasPreview)
            {
                controller.View.CreatePreview(hasPreview.GetPreview());
            }
        }
    }

    public void OnUpdate()
    {
        ConstructModel model = controller.Model;
        ConstructView view = controller.View;

        if (model.CurrentHit.collider == null || model.PrefabData == null)
        {
            view.HidePreview();
            return;
        }

        if(model.HoveredTower != null)
        {
            view.HidePreview();
            return;
        }

        if (model.CurrentGrid != null)
        {
            Vector3 rawPos = model.CurrentHit.point;
            // Model 데이터 갱신
            model.SnappedPos = model.CurrentGrid.GetCellCenterFromPoint(rawPos);
            model.IsValidPosition = CheckValidity(model.CurrentGrid, model.SnappedPos);
            Debug.Log(model.SnappedPos);

            // View에 갱신 지시
            view.UpdatePreview(model.SnappedPos, model.IsValidPosition);

            // 스냅된 위치에 그리드 셀이 존재하는지 확인
            bool isCellExist = model.CurrentGrid.CheckGridInPoint(model.SnappedPos);

            // 초기값은 false로 둠
            model.IsValidPosition = false;

            if (isCellExist)
            {
                // 실제 셀이 존재할 때만 방해물이 없는지 유효성 검사 (2차 검증)
                model.IsValidPosition = CheckValidity(model.CurrentGrid, model.SnappedPos);
            }

            // 유효하면 스냅 좌표, 아니면 원본 마우스 좌표 사용
            Vector3 previewRenderPos = model.IsValidPosition ? model.SnappedPos : rawPos;

            // 계산된 위치에 프리뷰 렌더링 (빨강/초록)
            view.UpdatePreview(previewRenderPos, model.IsValidPosition);
        }
        else
        {
            view.HidePreview();
        }
    }

    public void PerformMainAction()
    {
        var model = controller.Model;

        if (model.CurrentGrid != null && model.IsValidPosition)
        {
            Vector2Int curCellIndex = model.CurrentGrid.GetCellIndex(model.SnappedPos);
            Vector2 center = model.PrefabData.GetCenter(model.CurrentGrid.CellSize);
            Vector3 buildPos = model.SnappedPos + new Vector3(center.x, 0, center.y);

            // 건설 실행
            GameObject placedObj = controller.buildSystem.PlaceBuilding(model.PrefabToBuild, model.CurrentGrid, curCellIndex, buildPos, Quaternion.identity);

            placedObj.GetComponent<IBuildable>().OnPlaced();
            model.CurrentGrid.RegisterOccupancy(curCellIndex, model.PrefabData.GetOccupiedOffsets(), true);

            controller.ChangeState<IdleState>(); // 지은 후 대기 상태로
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
        foreach (var offset in controller.Model.PrefabData.GetOccupiedOffsets())
        {
            if (!grid.CheckCellValid(curCellIndex + offset, controller.Model.ObstacleLayer))
                return false;
        }
        return true;
    }
}
