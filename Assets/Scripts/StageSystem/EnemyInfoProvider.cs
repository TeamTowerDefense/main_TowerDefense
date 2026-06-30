using IGameInterface;
using UnityEngine;

public class EnemyInfoProvider : MonoBehaviour, IEnemyInfoProvider
{
    [Header("적 정보")]
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

    public bool TryGetEnemyInfo(out EnemyInfo info)
    {
        Transform target = targetTransform != null ? targetTransform : transform;
        info = new EnemyInfo(target, target.position, isAlive && isActiveAndEnabled);
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
