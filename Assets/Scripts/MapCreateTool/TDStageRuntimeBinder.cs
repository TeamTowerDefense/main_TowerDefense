using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class TDStageRuntimeBinder : MonoBehaviour
{
    [Header("스테이지")]
    [SerializeField] StageDataSO stageData;
    [SerializeField] StageController stageController;

    [Header("맵")]
    [SerializeField] TDMapRoot mapRoot;
    [SerializeField] GridGenerator gridGenerator;
    [SerializeField] GridDrawer gridDrawer;
    [SerializeField] MapBoundsInfoProvider boundsProvider;
    [SerializeField] MapController mapController;
    [SerializeField] BoxCollider gridBounds;
    [SerializeField] BoxCollider cameraBounds;

    [Header("몬스터")]
    [SerializeField] MonsterManager monsterManager;
    [SerializeField] MonsterManagerStageProvider monsterStageProvider;
    [SerializeField] StageMonsterTracker monsterTracker;

    [Header("건설")]
    [SerializeField] BuildSystem buildSystem;

    [Header("로그")]
    [SerializeField] bool logBindingResult = true;

    public TDMapRoot MapRoot => mapRoot;
    public MonsterManager MonsterManager => monsterManager;
    public StageDataSO StageData => stageData;

    #region 생명주기

    void Awake()
    {
        ResolveReferences();
        BindImmediate();
    }

    IEnumerator Start()
    {
        yield return null;

        ResolveReferences();
        BindImmediate();
        RegisterMapProvider();
        ApplyTowerLimit();

        if (logBindingResult) LogResult();
    }

    #endregion

    #region 외부 설정

    public void Configure(
        StageDataSO newStageData,
        StageController newStageController,
        TDMapRoot newMapRoot,
        MonsterManager newMonsterManager,
        MonsterManagerStageProvider newMonsterStageProvider,
        StageMonsterTracker newMonsterTracker,
        BuildSystem newBuildSystem)
    {
        stageData = newStageData;
        stageController = newStageController;
        mapRoot = newMapRoot;
        monsterManager = newMonsterManager;
        monsterStageProvider = newMonsterStageProvider;
        monsterTracker = newMonsterTracker;
        buildSystem = newBuildSystem;

        ResolveMapRuntimeReferences();
        BindImmediate();
    }

    [ContextMenu("런타임 참조 다시 연결")]
    public void BindNow()
    {
        ResolveReferences();
        BindImmediate();

        if (!Application.isPlaying) return;

        RegisterMapProvider();
        ApplyTowerLimit();

        if (logBindingResult) LogResult();
    }

    #endregion

    #region 즉시 연결

    void BindImmediate()
    {
        BindMapRoot();
        BindMonsterPath();
    }

    void BindMapRoot()
    {
        if (!mapRoot) return;

        mapRoot.FindHierarchyReferences();
        mapRoot.RefreshEndpointReferences();
        mapRoot.RebuildCellCache();
        mapRoot.AlignWaypointsToSurface();

        TDMapBuildGridProvider buildGridProvider =
            mapRoot.GetComponent<TDMapBuildGridProvider>();

        if (!buildGridProvider)
            buildGridProvider =
                mapRoot.gameObject.AddComponent<TDMapBuildGridProvider>();

        buildGridProvider.Configure(mapRoot);

        if (gridDrawer)
        {
            Vector3 localPosition = gridDrawer.transform.localPosition;
            localPosition.y =
                mapRoot.TileY +
                mapRoot.SurfaceYOffset +
                0.02f;

            gridDrawer.transform.localPosition = localPosition;
        }

        if (gridBounds || cameraBounds)
            mapRoot.SetBounds(gridBounds, gridBounds, cameraBounds);

        mapRoot.SetRuntimeComponents(
            gridGenerator,
            gridDrawer,
            boundsProvider);
    }

    void BindMonsterPath()
    {
        if (!monsterManager || !mapRoot) return;

        monsterManager.tileSize = Mathf.Max(0.1f, mapRoot.CellSize);
        monsterManager.paths ??= new List<PathData>();

        if (monsterManager.paths.Count == 0)
            monsterManager.paths.Add(new PathData());

        PathData path = monsterManager.paths[0] ?? new PathData();
        monsterManager.paths[0] = path;

        path.pathName = ResolveStageId();
        path.waypoints ??= new List<Transform>();
        path.waypoints.Clear();

        for (int i = 0; i < mapRoot.Waypoints.Count; i++)
        {
            Transform waypoint = mapRoot.Waypoints[i];
            if (waypoint) path.waypoints.Add(waypoint);
        }

        if (monsterManager.paths.Count > 1)
            monsterManager.paths.RemoveRange(
                1,
                monsterManager.paths.Count - 1);

        if (path.waypoints.Count < 2)
        {
            Debug.LogError(
                "[TDStageRuntimeBinder] MonsterManager에 연결할 Waypoint가 2개 미만입니다.",
                this);
        }
    }

    #endregion

    #region 지연 연결

    void RegisterMapProvider()
    {
        if (!mapController || !boundsProvider) return;
        mapController.Register(boundsProvider);
    }

    void ApplyTowerLimit()
    {
        if (!buildSystem) return;

        StageDataSO currentData =
            stageController && stageController.CurrentStageData
                ? stageController.CurrentStageData
                : stageData;

        if (currentData)
            buildSystem.SetTowerLimit(Mathf.Max(0, currentData.TowerLimit));
    }

    #endregion

    #region 참조 검색

    void ResolveReferences()
    {
        mapRoot ??= FindSceneComponent<TDMapRoot>();
        stageController ??= FindSceneComponent<StageController>();
        monsterManager ??= FindSceneComponent<MonsterManager>();
        monsterStageProvider ??= FindSceneComponent<MonsterManagerStageProvider>();
        monsterTracker ??= FindSceneComponent<StageMonsterTracker>();
        buildSystem ??= FindSceneComponent<BuildSystem>();

        ResolveMapRuntimeReferences();
    }

    void ResolveMapRuntimeReferences()
    {
        if (!mapRoot) return;

        gridGenerator =
            mapRoot.GridGenerator
                ? mapRoot.GridGenerator
                : mapRoot.GetComponentInChildren<GridGenerator>(true);

        gridDrawer =
            mapRoot.GridDrawer
                ? mapRoot.GridDrawer
                : mapRoot.GetComponentInChildren<GridDrawer>(true);

        boundsProvider =
            mapRoot.MapBoundsInfoProvider
                ? mapRoot.MapBoundsInfoProvider
                : mapRoot.GetComponentInChildren<MapBoundsInfoProvider>(true);

        mapController =
            mapRoot.GetComponentInChildren<MapController>(true);

        gridBounds =
            mapRoot.GridBounds
                ? mapRoot.GridBounds
                : FindNamedBoxCollider("GridBound");

        cameraBounds =
            mapRoot.CameraBounds as BoxCollider;

        if (!cameraBounds)
            cameraBounds = FindNamedBoxCollider("CameraBound");

        mapRoot.SetBounds(
            gridBounds,
            gridBounds,
            cameraBounds);

        mapRoot.SetRuntimeComponents(
            gridGenerator,
            gridDrawer,
            boundsProvider);
    }

    BoxCollider FindNamedBoxCollider(string objectName)
    {
        if (!mapRoot) return null;

        BoxCollider[] colliders =
            mapRoot.GetComponentsInChildren<BoxCollider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            BoxCollider collider = colliders[i];

            if (collider && collider.name == objectName)
                return collider;
        }

        return null;
    }

    T FindSceneComponent<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component &&
                component.gameObject.scene == gameObject.scene)
                return component;
        }

        return null;
    }

    string ResolveStageId()
    {
        StageDataSO currentData =
            stageController && stageController.CurrentStageData
                ? stageController.CurrentStageData
                : stageData;

        if (currentData &&
            !string.IsNullOrWhiteSpace(currentData.StageId))
            return currentData.StageId;

        return mapRoot ? mapRoot.name : gameObject.scene.name;
    }

    #endregion

    #region 로그

    void LogResult()
    {
        int waypointCount =
            monsterManager?.paths != null &&
            monsterManager.paths.Count > 0
                ? monsterManager.paths[0]?.waypoints?.Count ?? 0
                : 0;

        Debug.Log(
            $"[TDStageRuntimeBinder] 런타임 연결 완료\n" +
            $"Stage: {(stageController ? stageController.name : "없음")}\n" +
            $"Map: {(mapRoot ? mapRoot.name : "없음")}\n" +
            $"MonsterManager: {(monsterManager ? monsterManager.name : "없음")}\n" +
            $"Waypoints: {waypointCount}\n" +
            $"GridGenerator: {(gridGenerator ? gridGenerator.name : "없음")}\n" +
            $"GridDrawer: {(gridDrawer ? gridDrawer.name : "없음")}\n" +
            $"MapController: {(mapController ? mapController.name : "없음")}\n" +
            $"BoundsProvider: {(boundsProvider ? boundsProvider.name : "없음")}\n" +
            $"GridBound: {(gridBounds ? gridBounds.name : "없음")}\n" +
            $"CameraBound: {(cameraBounds ? cameraBounds.name : "없음")}",
            this);
    }

    #endregion
}