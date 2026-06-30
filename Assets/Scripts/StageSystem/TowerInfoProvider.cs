using IGameInterface;
using UnityEngine;

public class TowerInfoProvider : MonoBehaviour, ITowerInfoProvider
{
    [Header("포탑 정보")]
    [SerializeField] Transform targetTransform;
    [SerializeField] bool isAlive = true;

    bool registered;

    #region 생명 주기
    void Reset()
    {
        targetTransform = transform;
    }

    void OnEnable()
    {
        TryRegister();
    }

    void Start()
    {
        TryRegister();
    }

    void Update()
    {
        TryRegister();
    }

    void OnDisable()
    {
        if (registered && ServiceLocator.TryGet(out IMapService mapService))
            mapService.Unregister(this);

        registered = false;
    }
    #endregion

    public bool TryGetTowerInfo(out TowerInfo info)
    {
        Transform target = targetTransform != null ? targetTransform : transform;
        info = new TowerInfo(target, target.position, isAlive && isActiveAndEnabled);
        return info.IsAlive;
    }

    void TryRegister()
    {
        if (registered) return;
        if (!ServiceLocator.TryGet(out IMapService mapService)) return;

        mapService.Register(this);
        registered = true;
    }
}
