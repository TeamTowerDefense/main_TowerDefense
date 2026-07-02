using IGameInterface;
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
    private IInputService inputService;

    private void Awake()
    {
        InitInternal();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        InitExternal();
    }

    private void Update()
    {
        Vector2 mousePos = inputActions.Building.MousePos.ReadValue<Vector2>();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, rayHitLayer);

        controller.UpdateRayHitInfo(isHit, hit);
    }

    public void InitInternal()
    {
        inputActions = new BuildingTestInput();

        inputActions.Building.MainAction.started += ctx =>
        {
            if (CheckPointerOnUI()) return;
            controller.PerformCurModeAction();
        };
        inputActions.Building.CancelAction.performed += ctx => { controller.CancelCurModeAction(); };
        inputActions.Building.TowerSelect.performed += HandleTowerSelectInput;
    }

    public void InitExternal()
    {
        inputService = GameInputService.Instance;

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

    private void HandleMainActionInput()
    {
        if (CheckPointerOnUI())
            return;

        controller.PerformCurModeAction();
    }

    private void HandleSubActionInput()
    {
        controller.CancelCurModeAction();
    }

    private void HandleTowerSelectInput(InputAction.CallbackContext ctx)
    {

        if (int.TryParse(ctx.control.name, out int keyNumber))
        {
            int slotIndex = keyNumber - 1;

            controller.SelectBuildingFromSlot(slotIndex);
        }
    }
}
