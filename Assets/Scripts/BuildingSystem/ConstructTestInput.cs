using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ConstructTestInput: MonoBehaviour
{
    [SerializeField]
    private LayerMask rayHitLayer;
    [SerializeField] private ConstructController controller;
    [SerializeField] private Camera mainCamera;

    private BuildingTestInput inputActions;

    private void Awake()
    {
        inputActions = new BuildingTestInput();

        // 1. 좌클릭 (ModMainAction) 연결
        inputActions.Building.MainAction.started += ctx =>
        {
            if (CheckPointerOnUI())
                return;
            
            controller.PerformCurModeAction();
        };

        inputActions.Building.CancelAction.performed += ctx =>
        {
            controller.CancelCurModeAction();
        };

        // 2. 숫자키 1~5번 (TowerSelect) 연결
        inputActions.Building.TowerSelect.performed += OnTowerSelect;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        Vector2 mousePos = inputActions.Building.MousePos.ReadValue<Vector2>();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, rayHitLayer);

        controller.UpdateRayHitInfo(isHit, hit);
    }

    private void OnTowerSelect(InputAction.CallbackContext ctx)
    {

        if (int.TryParse(ctx.control.name, out int keyNumber))
        {
            int slotIndex = keyNumber - 1;

            controller.SelectBuildingByIndex(slotIndex);
        }
    }

    private bool CheckPointerOnUI()
    {
        // 현재 씬에 EvnetSystem이 없으면 false 반환
        if (EventSystem.current == null)
            return false;

        // 
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerEventData, results);

        return results.Count > 0;
    }
}
