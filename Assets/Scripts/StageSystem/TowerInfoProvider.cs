using IGameInterface;
using UnityEngine;

public class TowerInfoProvider : MonoBehaviour, ITowerInfoProvider
{
    [Header("포탑 정보")]
    [SerializeField] Transform targetTransform;
    [SerializeField] bool isAlive = true;
    [SerializeField] bool isPlaced = true;

    IMapService mapService;
    TowerInfo info;
    bool registered;


    public TowerInfo Info
    {
        get
        {
            EnsureInfo();
            RefreshInfo();
            return info;
        }
    }

    #region 생명 주기

    void Reset()
    {
        targetTransform = transform;
    }

    void Awake()
    {
        EnsureInfo();
    }

    void OnEnable()
    {
        EnsureInfo();
        RefreshInfo();
        TryRegister();
    }

    void Start()
    {
        TryRegister();
    }

    void LateUpdate()
    {
        if (!registered) TryRegister();
    }

    void OnDisable()
    {
        TryUnregister();

        if (info != null)
            info.IsAlive = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!targetTransform) targetTransform = transform;
        EnsureInfo();
        RefreshInfo();
    }
#endif

    #endregion

    #region Info Provider

    public bool TryGetInfo(out TowerInfo result)
    {
        EnsureInfo();
        RefreshInfo();

        result = info;
        return IsValidInfo(info);
    }

    public bool TryGetTowerInfo(out TowerInfo result)
        => TryGetInfo(out result);

    #endregion

    #region 외부 제어

    public void SetAlive(bool value)
    {
        isAlive = value;
        RefreshInfo();
    }

    public void SetPlaced(bool value)
    {
        isPlaced = value;
        RefreshInfo();
    }

    #endregion

    #region 내부 함수

    void EnsureInfo()
    {
        if (info != null) return;

        Transform target = GetTarget();
        info = new TowerInfo(target, target.position, isAlive && isActiveAndEnabled, isPlaced);
    }

    void RefreshInfo()
    {
        if (info == null) return;

        Transform target = GetTarget();

        info.Transform = target;
        info.IsAlive = isAlive && isActiveAndEnabled;
        info.IsPlaced = isPlaced;
    }

    Transform GetTarget() => targetTransform ? targetTransform : transform;
    bool IsValidInfo(TowerInfo value) => value != null && value.IsAlive && value.IsPlaced && value.Transform;

    void TryRegister()
    {
        if (registered) return;
        if (!ServiceLocator.TryGet(out mapService)) return;

        mapService.Register(this);
        registered = true;
    }

    void TryUnregister()
    {
        if (!registered) return;

        if (mapService != null) mapService.Unregister(this);
        else if (ServiceLocator.TryGet(out IMapService service)) service.Unregister(this);

        mapService = null;
        registered = false;
    }

    #endregion
}