using System.Collections.Generic;
using IGameInterface;
using UnityEngine;

public class MapController : MonoBehaviour, IMapService, IAutoSceneService
{
    #region 인스펙터

    [Header("맵 정보 프로바이더 초기화")]
    [SerializeField] MonoBehaviour[] initialProviders;

    [Header("Enemy Spatial Grid")]
    [SerializeField, Min(0.1f)] float enemyGridCellSize = 5f;

    #endregion

    #region 필드

    readonly List<IMapInfoProvider> mapInfoProviders = new();
    readonly List<ITowerInfoProvider> towerInfoProviders = new();
    readonly List<IEnemyInfoProvider> enemyInfoProviders = new();

    readonly List<TowerInfo> towers = new();
    readonly List<EnemyInfo> enemies = new();

    readonly Dictionary<IEnemyInfoProvider, EnemyInfo> enemyInfoByProvider = new();
    readonly EnemySpatialGrid enemyGrid = new();

    int enemySyncFrame = -1;

    #endregion

    #region 프로퍼티

    public Bounds MapBounds { get; private set; }
    public Bounds CameraBounds { get; private set; }
    public bool HasBounds { get; private set; }

    public IReadOnlyList<TowerInfo> Towers => towers;
    public IReadOnlyList<EnemyInfo> Enemies => enemies;

    public int AliveTowerCount => towers.Count;
    public int AliveEnemyCount => enemies.Count;

    #endregion

    #region 생명 주기

    void Awake()
    {
        enemyGrid.SetCellSize(enemyGridCellSize);

        ((IAutoSceneService)this).RegisterSceneServices();

        RegisterInitialProviders();
        RefreshMapInfo();
        RefreshTowerInfos();
        SyncAllEnemyInfos();
    }

    void LateUpdate()
    {
        EnsureEnemySynced();
    }

    void OnDestroy()
    {
        enemyGrid.Clear();
        enemyInfoByProvider.Clear();

        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    #endregion

    #region 등록

    public void Register(object provider)
    {
        if (IsNull(provider)) return;

        if (provider is IMapInfoProvider mapInfo) RegisterMapInfo(mapInfo);
        if (provider is ITowerInfoProvider towerInfo) RegisterTowerInfo(towerInfo);
        if (provider is IEnemyInfoProvider enemyInfo) RegisterEnemyInfo(enemyInfo);
    }

    public void Unregister(object provider)
    {
        if (IsNull(provider)) return;

        if (provider is IMapInfoProvider mapInfo) UnregisterMapInfo(mapInfo);
        if (provider is ITowerInfoProvider towerInfo) UnregisterTowerInfo(towerInfo);
        if (provider is IEnemyInfoProvider enemyInfo) UnregisterEnemyInfo(enemyInfo);
    }

    #endregion

    #region 조회

    public Vector3 ClampCameraPosition(Vector3 position)
    {
        if (!HasBounds) return position;

        position.x = Mathf.Clamp(position.x, CameraBounds.min.x, CameraBounds.max.x);
        position.z = Mathf.Clamp(position.z, CameraBounds.min.z, CameraBounds.max.z);
        return position;
    }

    public bool ContainsWorldPosition(Vector3 worldPos)
        => HasBounds && MapBounds.Contains(worldPos);

    public bool TryGetEnemy(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo enemy)
    {
        EnsureEnemySynced();
        return enemyGrid.TryGetEnemy(origin, range, mode, out enemy);
    }

    #endregion

    #region Map Info

    void RegisterMapInfo(IMapInfoProvider provider)
    {
        if (!TryAdd(mapInfoProviders, provider)) return;
        RefreshMapInfo();
    }

    void UnregisterMapInfo(IMapInfoProvider provider)
    {
        if (!TryRemove(mapInfoProviders, provider)) return;
        RefreshMapInfo();
    }

    void RefreshMapInfo()
    {
        HasBounds = false;
        MapBounds = default;
        CameraBounds = default;

        for (int i = mapInfoProviders.Count - 1; i >= 0; i--)
        {
            IMapInfoProvider provider = mapInfoProviders[i];

            if (IsNull(provider))
            {
                mapInfoProviders.RemoveAt(i);
                continue;
            }

            if (!provider.TryGetInfo(out MapInfo info) || info == null || !info.HasBounds)
                continue;

            MapBounds = info.MapBounds;
            CameraBounds = info.CameraBounds;
            HasBounds = true;
            return;
        }
    }

    #endregion

    #region Tower Info

    void RegisterTowerInfo(ITowerInfoProvider provider)
    {
        if (!TryAdd(towerInfoProviders, provider)) return;
        RefreshTowerInfos();
    }

    void UnregisterTowerInfo(ITowerInfoProvider provider)
    {
        if (!TryRemove(towerInfoProviders, provider)) return;
        RefreshTowerInfos();
    }

    void RefreshTowerInfos()
    {
        towers.Clear();

        for (int i = towerInfoProviders.Count - 1; i >= 0; i--)
        {
            ITowerInfoProvider provider = towerInfoProviders[i];

            if (IsNull(provider))
            {
                towerInfoProviders.RemoveAt(i);
                continue;
            }

            if (provider.TryGetInfo(out TowerInfo info) && IsValidTower(info))
                towers.Add(info);
        }
    }

    bool IsValidTower(TowerInfo info)
        => info != null && info.IsAlive && info.IsPlaced && info.Transform;

    #endregion

    #region Enemy Info

    void RegisterEnemyInfo(IEnemyInfoProvider provider)
    {
        if (!TryAdd(enemyInfoProviders, provider)) return;

        SyncEnemyInfo(provider);
        enemySyncFrame = -1;
    }

    void UnregisterEnemyInfo(IEnemyInfoProvider provider)
    {
        if (!TryRemove(enemyInfoProviders, provider)) return;

        RemoveTrackedEnemy(provider);
        enemySyncFrame = -1;
    }

    void EnsureEnemySynced()
    {
        if (enemySyncFrame == Time.frameCount) return;

        SyncAllEnemyInfos();
        enemySyncFrame = Time.frameCount;
    }

    void SyncAllEnemyInfos()
    {
        for (int i = enemyInfoProviders.Count - 1; i >= 0; i--)
        {
            IEnemyInfoProvider provider = enemyInfoProviders[i];

            if (IsNull(provider))
            {
                RemoveTrackedEnemy(provider);
                enemyInfoProviders.RemoveAt(i);
                continue;
            }

            SyncEnemyInfo(provider);
        }
    }

    void SyncEnemyInfo(IEnemyInfoProvider provider)
    {
        if (provider == null) return;

        if (!provider.TryGetInfo(out EnemyInfo info) || info == null || !info.IsAlive)
        {
            RemoveTrackedEnemy(provider);
            return;
        }

        if (enemyInfoByProvider.TryGetValue(provider, out EnemyInfo oldInfo))
        {
            if (!ReferenceEquals(oldInfo, info))
            {
                enemyGrid.Remove(oldInfo);
                enemies.Remove(oldInfo);

                enemyInfoByProvider[provider] = info;
                enemies.Add(info);
            }

            enemyGrid.AddOrUpdate(info);
            return;
        }

        enemyInfoByProvider.Add(provider, info);
        enemies.Add(info);
        enemyGrid.AddOrUpdate(info);
    }

    void RemoveTrackedEnemy(IEnemyInfoProvider provider)
    {
        if (provider == null || !enemyInfoByProvider.TryGetValue(provider, out EnemyInfo info))
            return;

        enemyGrid.Remove(info);
        enemies.Remove(info);
        enemyInfoByProvider.Remove(provider);
    }

    #endregion

    #region 내부 함수

    void RegisterInitialProviders()
    {
        if (initialProviders == null) return;

        foreach (MonoBehaviour provider in initialProviders)
            Register(provider);
    }

    static bool TryAdd<T>(List<T> list, T value) where T : class
    {
        if (value == null || list.Contains(value)) return false;

        list.Add(value);
        return true;
    }

    static bool TryRemove<T>(List<T> list, T value) where T : class
        => value != null && list.Remove(value);

    static bool IsNull(object target)
        => target == null || target is Object unityObject && unityObject == null;

    #endregion
}