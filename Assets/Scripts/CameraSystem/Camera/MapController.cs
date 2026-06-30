using IGameInterface;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour, IMapService, IAutoSceneService
{
    [Header("맵 정보 프로바이더 초기화")]
    [SerializeField] MonoBehaviour[] initialProviders;

    [Header("적 그리드")]
    [SerializeField, Min(0.1f)] float enemyGridCellSize = 5f;

    readonly List<IMapInfoProvider> mapInfoProviders = new();
    readonly List<ITowerInfoProvider> towerInfoProviders = new();
    readonly List<IEnemyInfoProvider> enemyInfoProviders = new();

    readonly List<TowerInfo> towers = new();
    readonly List<EnemyInfo> enemies = new();

    readonly Dictionary<IEnemyInfoProvider, EnemyInfo> enemyInfoByProvider = new();
    readonly EnemySpatialGrid enemyGrid = new();

    public Bounds MapBounds { get; private set; }
    public Bounds CameraBounds { get; private set; }
    public bool HasBounds { get; private set; }

    public IReadOnlyList<TowerInfo> Towers => towers;
    public IReadOnlyList<EnemyInfo> Enemies => enemies;

    public int AliveTowerCount => towers.Count;
    public int AliveEnemyCount => enemies.Count;

    #region 생명 주기
    void Awake()
    {
        ((IAutoSceneService)this).RegisterSceneServices();
        RegisterInitialProviders();
        RefreshAll();
    }

    void LateUpdate()
    {
        RefreshAll();
    }

    void OnDestroy()
    {
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

    public bool ContainsWorldPosition(Vector3 worldPos) => HasBounds && MapBounds.Contains(worldPos);

    public bool TryGetEnemy(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo enemyInfo)
    {
        EnsureEnemySync();
        return enemyGrid.TryGetEnemy(origin, range, mode, out enemyInfo);
    }
    #endregion

    #region MapInfo 함수
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

            if (!provider.TryGetInfo(out MapInfo info) || info == null || !info.HasBounds) continue;

            MapBounds = info.MapBounds;
            CameraBounds = info.CameraBounds;
            HasBounds = true;
            return;
        }
    }

    #endregion

    #region 타워 정보
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

    #region 에너미 정보
    void RegisterEnemyInfo(IEnemyInfoProvider provider)
    {

    }
    void UnregisterEnemyInfo(IEnemyInfoProvider provider)
    {

    }
    void EnsureEnemySync()
    {

    }
    void SyncAllEnemyInfos()
    {

    }
    void SyncEnemyInfo(IEnemyInfoProvider provider)
    {
        if (provider == null) return;
        if (!provider.TryGetInfo(out EnemyInfo info) || !info.IsAlive)
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
        if (provider == null || !enemyInfoByProvider.TryGetValue(provider, out EnemyInfo info)) return;

        enemyGrid.Remove(info);
        enemies.Remove(info);
        enemyInfoByProvider.Remove(provider);
    }
    #endregion

    #region 내부 유틸
    void RegisterInitialProviders()
    {
        if (initialProviders == null) return;

        foreach (MonoBehaviour provider in initialProviders) Register(provider);
    }
    static bool TryAdd<T>(List<T> list, T value) where T : class
    {
        if (value == null || list.Contains(value)) return false;

        list.Add(value);
        return true;
    }
    static bool TryRemove<T>(List<T> list, T value) where T : class => value != null && list.Remove(value);
    static bool IsNull(object target) => target == null || target is UnityEngine.Object unityObj && unityObj == null;
    #endregion

}
