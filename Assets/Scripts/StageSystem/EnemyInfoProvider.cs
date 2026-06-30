using IGameInterface;
using UnityEngine;

public class EnemyInfoProvider : MonoBehaviour, IEnemyInfoProvider, IEnemyInfoWriter
{

    [Header("적 정보")]
    [SerializeField] Transform targetTransform;
    [SerializeField] bool isAlive = true;
    [SerializeField] bool isTargetable = true;

    [Header("타겟 / 진행도")]
    [SerializeField, Range(0f, 1f)] float pathProgress;
    [SerializeField] MonoBehaviour attackTargetSource;

    IMapService mapService;
    IAttackTarget attackTarget;
    EnemyInfo info;
    bool registered;

    public EnemyInfo Info
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
        attackTargetSource = GetComponent<MonoBehaviour>();
        CacheAttackTarget();
    }

    void Awake()
    {
        CacheAttackTarget();
        EnsureInfo();
    }

    void OnEnable()
    {
        CacheAttackTarget();
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
        CacheAttackTarget();
        EnsureInfo();
        RefreshInfo();
    }
#endif

    #endregion

    #region Info Provider

    public bool TryGetInfo(out EnemyInfo result)
    {
        EnsureInfo();
        RefreshInfo();

        result = info;
        return IsValidInfo(info);
    }

    public bool TryGetEnemyInfo(out EnemyInfo result) => TryGetInfo(out result);

    #endregion

    #region 외부 제어

    public void SetAlive(bool value)
    {
        isAlive = value;
        RefreshInfo();
    }

    public void SetTargetable(bool value)
    {
        isTargetable = value;
        RefreshInfo();
    }

    public void SetPathProgress(float value)
    {
        pathProgress = Mathf.Clamp01(value);
        RefreshInfo();
    }

    public void SetAttackTarget(IAttackTarget target)
    {
        attackTarget = target;
        RefreshInfo();
    }

    #endregion

    #region 내부 함수

    void EnsureInfo()
    {
        if (info != null) return;

        Transform target = GetTarget();

        info = new EnemyInfo(
            target,
            target.position,
            isAlive && isActiveAndEnabled,
            isTargetable,
            pathProgress,
            attackTarget);
    }

    void RefreshInfo()
    {
        if (info == null) return;

        Transform target = GetTarget();

        info.Transform = target;
        info.IsAlive = isAlive && isActiveAndEnabled;
        info.IsTargetable = isTargetable;
        info.PathProgress = pathProgress;
        info.AttackTarget = attackTarget;
    }

    void CacheAttackTarget()
    {
        attackTarget = null;

        if (attackTargetSource is IAttackTarget target)
        {
            attackTarget = target;
            return;
        }

        if (TryGetComponent(out IAttackTarget selfTarget))
            attackTarget = selfTarget;
        else if (GetComponentInParent<IAttackTarget>() is IAttackTarget parentTarget)
            attackTarget = parentTarget;
    }

    Transform GetTarget()=> targetTransform ? targetTransform : transform;
    bool IsValidInfo(EnemyInfo value) => value != null && value.CanBeTargeted;

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