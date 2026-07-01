using IGameInterface;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputService : SingletonBase<GameInputService>, IInputService, IAutoSceneService
{
    [Header("인풋 시스템")]
    [SerializeField] InputActionAsset inputActions;

    [Header("시작 입력 모드")]
    [SerializeField] GameInputMode startMode = GameInputMode.TopView;

    [Header("시작 시 활성화할 액션맵")]
    [SerializeField]
    List<string> startEnabledMaps = new()
    {
        InputMapKeys.TopView,
        InputMapKeys.UI
    };

    [Header("공통 포인터 액션")]
    [SerializeField] string pointerMapName = InputMapKeys.TopView;
    [SerializeField] string pointerPositionActionName = InputActionKeys.PointerPosition;
    [SerializeField] string pointerDeltaActionName = InputActionKeys.PointerDelta;
    [SerializeField] string scrollActionName = InputActionKeys.Scroll;

    [Header("옵션")]
    [SerializeField] bool disableAllOnEnable = true;
    [SerializeField] bool disableAllOnDisable = true;
    [SerializeField] bool logInputEvent;

    readonly Dictionary<string, InputActionMap> mapLookup = new();
    readonly Dictionary<string, InputAction> actionLookup = new();
    readonly List<InputAction> cachedActions = new();

    bool cached;
    bool bound;

    public InputActionAsset Actions => inputActions;
    public GameInputMode CurrentMode { get; private set; } = GameInputMode.None;

    public Vector2 PointerScreenPosition => ReadVector2(pointerMapName, pointerPositionActionName, Mouse.current?.position.ReadValue() ?? Vector2.zero);
    public Vector2 PointerDelta => ReadVector2(pointerMapName, pointerDeltaActionName);
    public Vector2 ScrollDelta => ReadVector2(pointerMapName, scrollActionName);

    public event Action<GameInputEvent> Started;
    public event Action<GameInputEvent> Performed;
    public event Action<GameInputEvent> Canceled;
    public event Action<GameInputEvent> AnyTriggered;

    #region 생명 주기


    protected override void Awake()
    {
        base.Awake();
        CacheInputActions();
        ((IAutoSceneService)this).RegisterSceneServices();
    }

    void OnEnable()
    {
        CacheInputActions();
        BindInputEvents();

        if (disableAllOnEnable) DisableAllMaps();

        EnableMaps(startEnabledMaps.ToArray());
        SetMode(startMode);
    }

    void OnDisable()
    {
        UnbindInputEvents();

        if (disableAllOnDisable) DisableAllMaps();
    }

    void OnDestroy()
    {
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    public bool EnableMap(string mapName)
    {
        if (!TryGetMap(mapName, out var map)) return false;

        map.Enable();
        return true;
    }

    public bool DisableMap(string mapName)
    {
        if (!TryGetMap(mapName, out var map)) return false;

        map.Disable();
        return true;
    }

    public void EnableOnly(string mapName)
    {
        DisableAllMaps();
        EnableMap(mapName);
    }

    public void EnableMaps(params string[] mapNames)
    {
        if (mapNames == null) return;

        foreach (string mapName in mapNames) EnableMap(mapName);
    }

    public void DisableAllMaps()
    {
        if (inputActions == null) return;

        foreach (InputActionMap map in inputActions.actionMaps) map.Disable();
    }
    #endregion

    #region 액션 맵
    public bool TryGetAction(string mapName, string actionName, out InputAction action)
    {
        
        CacheInputActions();

        return actionLookup.TryGetValue(MakeActionKey(mapName, actionName), out action) && action != null;
    }
    public void SetMode(GameInputMode mode)
    {
        DisableAllMaps();

        switch (mode)
        {
            case GameInputMode.TopView:
                EnableMap(InputMapKeys.Common);
                EnableMap(InputMapKeys.TopView);
                EnableMap(InputMapKeys.UI);
                break;

            case GameInputMode.Character:
                EnableMap(InputMapKeys.Common);
                EnableMap(InputMapKeys.Player);
                EnableMap(InputMapKeys.UI);
                break;

            case GameInputMode.UIOnly:
                EnableMap(InputMapKeys.UI);
                break;

            case GameInputMode.None:
            default:
                break;
        }

        CurrentMode = mode;
    }
    #endregion

    #region 입력값 리딩
    public bool IsPressed(string mapName, string actoinName)
        => TryGetAction(mapName, actoinName, out var action) && action.enabled && action.IsPressed();
    public bool WasPressedThisFrame(string mapName, string actionName)
        => TryGetAction(mapName, actionName, out var action) && action.enabled && action.WasPressedThisFrame();
    public bool WasReleasedThisFrame(string mapName, string actionName)
        => TryGetAction(mapName, actionName, out var action) && action.enabled && action.WasReleasedThisFrame();
    public float ReadFloat(string mapName, string actionName) => ReadFloat(mapName, actionName, 0f);
    public Vector2 ReadVector2(string mapName, string actionName) => ReadVector2(mapName, actionName, Vector2.zero);
    public Vector3 ReadVector3(string mapName, string actionName) => ReadVector3(mapName, actionName, Vector3.zero);
    float ReadFloat(string mapName, string actionName, float fallback)
        => TryGetAction(mapName, actionName, out var action) && action.enabled ? action.ReadValue<float>() : fallback;
    Vector2 ReadVector2(string mapName, string actionName, Vector2 fallback)
        => TryGetAction(mapName, actionName, out var action) && action.enabled ? action.ReadValue<Vector2>() : fallback;
    Vector3 ReadVector3(string mapName, string actionName, Vector3 fallback)
        => TryGetAction(mapName, actionName, out var action) && action.enabled ? action.ReadValue<Vector3>() : fallback;
    #endregion

    #region 초기화, 바인딩
    void CacheInputActions()
    {
        if (cached) return;

        mapLookup.Clear();
        actionLookup.Clear();
        cachedActions.Clear();

        if (inputActions == null)
        {
            Debug.LogError("[GameInputService] InputActionAsset이 비어 있습니다.", this);
            return;
        }

        foreach (InputActionMap map in inputActions.actionMaps)
        {
            mapLookup[map.name] = map;

            foreach (InputAction action in map.actions)
            {
                actionLookup[MakeActionKey(map.name, action.name)] = action;
                cachedActions.Add(action);
            }
        }

        cached = true;
    }
    void BindInputEvents()
    {
        if (bound) return;

        foreach (InputAction action in cachedActions)
        {
            action.started += HandleStarted;
            action.performed += HandlePerformed;
            action.canceled += HandleCanceled;
        }

        bound = true;
    }

    void UnbindInputEvents()
    {
        if (!bound) return;

        foreach (InputAction action in cachedActions)
        {
            action.started -= HandleStarted;
            action.performed -= HandlePerformed;
            action.canceled -= HandleCanceled;
        }

        bound = false;
    }
    #endregion

    #region 내부 함수
    bool TryGetMap(string mapName, out InputActionMap map)
    {
        CacheInputActions();
        return mapLookup.TryGetValue(mapName, out map) && map != null;
    }
    void HandleStarted(InputAction.CallbackContext context)
    {
        GameInputEvent inputEvent = new(context);

        if (logInputEvent) Debug.Log($"[Input Started] {inputEvent.FullName}");

        Started?.Invoke(inputEvent);
        AnyTriggered?.Invoke(inputEvent);
    }
    void HandlePerformed(InputAction.CallbackContext context)
    {
        GameInputEvent inputEvent = new(context);

        if (logInputEvent)  Debug.Log($"[Input Performed] {inputEvent.FullName}");

        Performed?.Invoke(inputEvent);
        AnyTriggered?.Invoke(inputEvent);
    }
    void HandleCanceled(InputAction.CallbackContext context)
    {
        GameInputEvent inputEvent = new(context);

        if (logInputEvent) Debug.Log($"[Input Canceled] {inputEvent.FullName}");

        Canceled?.Invoke(inputEvent);
        AnyTriggered?.Invoke(inputEvent);
    }
    static string MakeActionKey(string mapName, string actionName) => $"{mapName}/{actionName}";
    #endregion
}

