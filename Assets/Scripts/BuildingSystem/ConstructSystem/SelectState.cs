using Unity.VisualScripting;
using UnityEngine;

public class SelectState : IConstructMode
{
    private ConstructController controller;

    public SelectState(ConstructController controller) 
    {
        this.controller = controller; 
    }

    public void OnEnter()
    {
        Debug.Log("선택 상태 진입");

        var model = controller.Model;
        var view = controller.View;

        if (model.SelectedBuilding != null)
        {
            MonoBehaviour towerMono = model.SelectedBuilding as MonoBehaviour;
            if (towerMono != null)
            {
                controller.View.ShowTowerMenu(towerMono.transform.position);
            }
        }
    }

    public void OnUpdate()
    {
    }

    public void PerformMainAction()
    {
        ConstructModel model = controller.Model;
        IBuildable hitBuilding = model.PointerHitInfo.collider?.GetComponentInParent<IBuildable>();

        // 다른 타워를 클릭했다면 타겟을 교체하고 정보 재출력
        if (hitBuilding != null && hitBuilding != model.SelectedBuilding)
        {
            model.SelectedBuilding = hitBuilding;
            OnEnter();
        }
        // 빈 땅을 클릭했다면 선택 취소
        //else if (hitBuilding == null)
        //{
        //    CancelMainAction();
        //}
    }

    // 타워 해체
    public void PerformSubAction()
    {
        ConstructModel model = controller.Model;

        Debug.Log(model.SelectedBuilding == null);
        Debug.Log(model.SelectedBuilding.BuildingData.isDestructible);

        if (model.SelectedBuilding != null && model.SelectedBuilding.BuildingData.isDestructible)
        {
            Vector2Int curCellIndex = model.SelectedBuilding.ConstructedIndex;
            MonoBehaviour towerMono = model.SelectedBuilding as MonoBehaviour;

            if (controller.resourceSystem != null)
            {
                controller.resourceSystem.Earn(model.SelectedBuilding.BuildingData.cost);
                Debug.Log($"타워 철거 완료 -> 자원 반환: {model.SelectedBuilding.BuildingData.cost}");
            }

            // 그리드 점유 해제
            model.SelectedBuilding.ConstructedGrid.RegisterOccupancy(curCellIndex, model.SelectedBuilding.GetOccupiedOffsets(), false);

            // 오브젝트 파괴 
            controller.buildSystem.DestroyBuilding(towerMono.gameObject);
            Debug.Log("타워 철거 완료 -> 자원 반환");

            // 철거 후 일반 상태로 복귀
            controller.ChangeState<IdleState>();
        }
    }

    public void CancelMainAction()
    {
        // 우클릭 시 선택 해제
        controller.ChangeState<IdleState>();
    }

    public void OnExit()
    {
        Debug.Log("타워 사거리 표시 비활성화");

        controller.View.HideTowerMenu();
        controller.Model.SelectedBuilding = null;
    }
}
