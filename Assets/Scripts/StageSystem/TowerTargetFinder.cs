using IGameInterface;
using UnityEngine;

public class TowerTargetFinder : MonoBehaviour, ITowerTargetFinder
{
    [SerializeField] float searchInterval = 0.15f;
    [SerializeField] EnemyTargetMode targetMode = EnemyTargetMode.ClosestToTower;

    IMapService mapService;
    EnemyInfo currentTarget;
    float nextSearchTime;

    public EnemyInfo CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    public EnemyTargetMode ChaseMode => targetMode;

    void Start()
    {
        ServiceLocator.TryGet(out mapService);
    }

    public bool TryGetTarget(Vector3 origin, float range, out EnemyInfo target)
        => TryGetTarget(origin, range, targetMode, out target);

    public bool TryGetTarget(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo target)
    {
        if (IsValidTarget(currentTarget, origin, range))
        {
            target = currentTarget;
            return true;
        }

        currentTarget = null;

        if (Time.time < nextSearchTime)
        {
            target = null;
            return false;
        }

        nextSearchTime = Time.time + searchInterval;

        if (mapService == null && !ServiceLocator.TryGet(out mapService))
        {
            target = null;
            return false;
        }

        if (mapService.TryGetEnemy(origin, range, mode, out target))
        {
            currentTarget = target;
            return true;
        }

        target = null;
        return false;
    }

    public bool IsValidTarget(EnemyInfo target, Vector3 origin, float range)
    {
        if (target == null || !target.CanBeTargeted || !target.Transform) return false;

        Vector3 diff = target.Position - origin;
        diff.y = 0f;

        return diff.sqrMagnitude <= range * range;
    }

    public void SetChaseMode(EnemyTargetMode mode) => targetMode = mode;
    public void ClearTarget()
    {
        currentTarget = null;
        nextSearchTime = 0f;
    }
}