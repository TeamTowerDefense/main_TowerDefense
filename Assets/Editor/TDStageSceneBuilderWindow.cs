#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class TDStageSceneBuilderWindow : EditorWindow
{
    const string DefaultOutputFolder = "Assets/Scenes/Stages";
    const string StageDataLabel = "StageData";

    static readonly string[] ExcludedRootNames =
    {
        "ObjectPoolManager",
        "LevelDesignTool",
        "TestTile",
        "d",
        "TDStageRuntime"
    };

    [SerializeField] SceneAsset prototypeScene;
    [SerializeField] GameObject mapPrefab;
    [SerializeField] StageDataSO stageData;
    [SerializeField] DefaultAsset outputFolder;

    [SerializeField] bool overwriteExisting = true;
    [SerializeField] bool addToBuildSettings = true;
    [SerializeField] bool registerStageDataAddressable = false;

    Vector2 scroll;

    [MenuItem("Tools/TD Map/스테이지 씬 생성기")]
    static void Open()
    {
        TDStageSceneBuilderWindow window = GetWindow<TDStageSceneBuilderWindow>();
        window.titleContent = new GUIContent("TD Stage Builder");
        window.minSize = new Vector2(430f, 430f);
        window.Show();
    }

    void OnEnable()
    {
        EnsureFolder(DefaultOutputFolder);
        outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultOutputFolder);
        prototypeScene ??= FindPrototypeScene();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("스테이지 씬 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Prototype 씬을 복제한 뒤 기존 맵, d(Map Runtime), 씬 내부 ObjectPoolManager를 제거하고, " +
            "선택한 Map Prefab 내부 MapRuntime과 StageDataSO를 연결합니다.\n\n" +
            "생성되는 씬 파일명은 StageDataSO.StageId와 동일하게 유지됩니다.",
            MessageType.Info);

        EditorGUILayout.Space(6f);

        prototypeScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Prototype Scene",
            prototypeScene,
            typeof(SceneAsset),
            false);

        mapPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Map Prefab",
            mapPrefab,
            typeof(GameObject),
            false);

        stageData = (StageDataSO)EditorGUILayout.ObjectField(
            "Stage Data",
            stageData,
            typeof(StageDataSO),
            false);

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "생성 폴더",
            outputFolder,
            typeof(DefaultAsset),
            false);

        EditorGUILayout.Space(6f);

        overwriteExisting = EditorGUILayout.Toggle("기존 씬 덮어쓰기", overwriteExisting);
        addToBuildSettings = EditorGUILayout.Toggle("Build Settings 자동 등록", addToBuildSettings);
        registerStageDataAddressable = EditorGUILayout.Toggle("StageData 라벨 자동 등록", registerStageDataAddressable);

        EditorGUILayout.Space(8f);
        DrawPreview();
        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!CanBuild()))
        {
            if (GUILayout.Button("스테이지 씬 생성 또는 갱신", GUILayout.Height(36f)))
                BuildStageScene();
        }

        EditorGUILayout.EndScrollView();
    }

    #region 생성

    void BuildStageScene()
    {
        if (!ValidateInput(out TDMapRoot prefabMapRoot, out string sceneName, out string targetPath)) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string prototypePath = AssetDatabase.GetAssetPath(prototypeScene);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath))
        {
            if (string.Equals(SceneManager.GetActiveScene().path, targetPath, StringComparison.OrdinalIgnoreCase))
                EditorSceneManager.OpenScene(prototypePath, OpenSceneMode.Single);

            if (!overwriteExisting)
            {
                EditorUtility.DisplayDialog(
                    "TD Stage Builder",
                    $"이미 같은 이름의 씬이 있습니다.\n\n{targetPath}",
                    "확인");
                return;
            }

            bool replace = EditorUtility.DisplayDialog(
                "TD Stage Builder",
                $"기존 씬을 Prototype 기준으로 다시 생성합니다.\n\n{targetPath}",
                "덮어쓰기",
                "취소");

            if (!replace) return;
            if (!AssetDatabase.DeleteAsset(targetPath))
            {
                Debug.LogError($"[TD Stage Builder] 기존 씬 삭제 실패: {targetPath}");
                return;
            }
        }

        SyncStageData(sceneName);

        if (!AssetDatabase.CopyAsset(prototypePath, targetPath))
        {
            Debug.LogError(
                $"[TD Stage Builder] Prototype 씬 복제 실패\n" +
                $"원본: {prototypePath}\n대상: {targetPath}");
            return;
        }

        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);

        Scene scene = default;

        try
        {
            scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);

            RemovePrototypeOnlyObjects(scene);

            GameObject mapInstance = InstantiateMapPrefab(scene, sceneName);
            TDMapRoot mapRoot = mapInstance.GetComponent<TDMapRoot>();

            ConfigureStageController(scene);
            ConfigureRuntimeSystems(scene, mapRoot);
            ConfigureCameraStart(scene, mapRoot);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, targetPath))
                throw new InvalidOperationException($"씬 저장에 실패했습니다: {targetPath}");

            if (addToBuildSettings) AddSceneToBuildSettings(targetPath);
            if (registerStageDataAddressable) RegisterStageDataAddressable();

            AssetDatabase.SaveAssets();

            SceneAsset createdScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath);
            Selection.activeObject = createdScene;
            EditorGUIUtility.PingObject(createdScene);

            Debug.Log(
                $"[TD Stage Builder] 스테이지 씬 생성 완료\n" +
                $"Scene: {sceneName}\n" +
                $"StageData: {stageData.name}\n" +
                $"Map: {mapPrefab.name}\n" +
                $"Path: {targetPath}",
                createdScene);

            EditorUtility.DisplayDialog(
                "TD Stage Builder",
                $"스테이지 씬 생성이 완료되었습니다.\n\n" +
                $"Scene: {sceneName}\n" +
                $"Map: {mapPrefab.name}\n" +
                $"StageData: {stageData.name}",
                "확인");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "TD Stage Builder 실패",
                exception.Message,
                "확인");
        }
    }

    GameObject InstantiateMapPrefab(Scene scene, string sceneName)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(mapPrefab, scene) as GameObject;

        if (!instance)
            throw new InvalidOperationException("Map Prefab 인스턴스 생성에 실패했습니다.");

        instance.name = $"Map_{sceneName}";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        TDMapRoot mapRoot = instance.GetComponent<TDMapRoot>();

        if (!mapRoot)
            throw new InvalidOperationException("생성된 Map Prefab에 TDMapRoot가 없습니다.");

        mapRoot.FindHierarchyReferences();
        mapRoot.RefreshEndpointReferences();
        mapRoot.RebuildCellCache();
        RebuildWaypoints(mapRoot);

        return instance;
    }

    #endregion

    #region Prototype 정리

    static void RemovePrototypeOnlyObjects(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        HashSet<GameObject> rootsToRemove = new();
        List<GameObject> nestedMapsToRemove = new();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (!root) continue;

            if (IsExcludedRoot(root.name) || ContainsLegacyMapRuntime(root))
            {
                rootsToRemove.Add(root);
                continue;
            }

            TDMapRoot[] oldMapRoots = root.GetComponentsInChildren<TDMapRoot>(true);

            for (int j = 0; j < oldMapRoots.Length; j++)
                if (oldMapRoots[j]) nestedMapsToRemove.Add(oldMapRoots[j].gameObject);
        }

        for (int i = nestedMapsToRemove.Count - 1; i >= 0; i--)
        {
            GameObject target = nestedMapsToRemove[i];
            if (target && !HasRemovedAncestor(target.transform, rootsToRemove)) Object.DestroyImmediate(target);
        }

        foreach (GameObject root in rootsToRemove)
            if (root) Object.DestroyImmediate(root);

        EditorSceneManager.MarkSceneDirty(scene);
        ValidatePrototypeCleanup(scene);
    }

    static bool ContainsLegacyMapRuntime(GameObject root)
    {
        if (!root) return false;

        return root.GetComponentInChildren<GridGenerator>(true) ||
               root.GetComponentInChildren<GridDrawer>(true) ||
               root.GetComponentInChildren<MapBoundsInfoProvider>(true) ||
               root.GetComponentInChildren<MapController>(true);
    }

    static bool HasRemovedAncestor(Transform target, HashSet<GameObject> rootsToRemove)
    {
        for (Transform current = target; current; current = current.parent)
            if (rootsToRemove.Contains(current.gameObject)) return true;

        return false;
    }

    static void ValidatePrototypeCleanup(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        List<string> leftovers = new();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (!root) continue;

            if (IsExcludedRoot(root.name) || ContainsLegacyMapRuntime(root)) leftovers.Add(root.name);
        }

        if (leftovers.Count == 0) return;

        throw new InvalidOperationException(
            "Prototype의 기존 맵 런타임 오브젝트 제거에 실패했습니다: " +
            string.Join(", ", leftovers));
    }

    static bool IsExcludedRoot(string rootName)
    {
        string normalized = string.IsNullOrWhiteSpace(rootName) ? string.Empty : rootName.Trim();

        for (int i = 0; i < ExcludedRootNames.Length; i++)
        {
            string excluded = ExcludedRootNames[i];

            if (string.Equals(normalized, excluded, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(excluded + " (", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static void ClearChildren(Transform parent)
    {
        if (!parent) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    #endregion


    #region Waypoint 생성

    static readonly Vector2Int[] PathDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    static void RebuildWaypoints(TDMapRoot mapRoot)
    {
        TDMapCellMarker spawn = mapRoot.FindEndpoint(TDMapCellType.Spawn);
        TDMapCellMarker targetBase = mapRoot.FindEndpoint(TDMapCellType.Base);

        if (!spawn || !targetBase)
            throw new InvalidOperationException("MapRoot에 Spawn 또는 Base가 없습니다.");

        List<TDMapCellMarker> pathMarkers = new();
        mapRoot.CollectMarkers(TDMapCellType.Path, pathMarkers);

        if (pathMarkers.Count == 0)
            throw new InvalidOperationException("MapRoot에 Path 셀이 없습니다.");

        Dictionary<Vector2Int, TDMapCellMarker> pathByCell = new();

        for (int i = 0; i < pathMarkers.Count; i++)
        {
            TDMapCellMarker marker = pathMarkers[i];
            if (!marker) continue;

            if (pathByCell.ContainsKey(marker.GridPosition))
                throw new InvalidOperationException($"Path 셀이 중복되었습니다: {marker.GridPosition}");

            pathByCell.Add(marker.GridPosition, marker);
        }

        Vector2Int startCell = ResolveEndpointPathCell("Spawn", spawn.GridPosition, pathByCell);
        Vector2Int endCell = ResolveEndpointPathCell("Base", targetBase.GridPosition, pathByCell);

        if (startCell == endCell)
            throw new InvalidOperationException("Spawn과 Base가 같은 Path 셀에 연결되어 있습니다.");

        ValidatePathDegrees(pathByCell);
        List<Vector2Int> orderedPath = BuildOrderedPath(startCell, endCell, pathByCell);

        Transform waypointRoot = mapRoot.WaypointRoot;

        if (!waypointRoot)
            throw new InvalidOperationException("MapRoot의 PathWaypoints 부모가 없습니다.");

        ClearChildren(waypointRoot);

        float yOffset = mapRoot.Palette ? mapRoot.Palette.WaypointYOffset : 0f;
        List<Vector3> positions = new();

        AddWaypointPosition(positions, mapRoot.GridToWorld(spawn.GridPosition) + mapRoot.transform.up * yOffset);

        for (int i = 0; i < orderedPath.Count; i++)
            AddWaypointPosition(positions, mapRoot.GridToWorld(orderedPath[i]) + mapRoot.transform.up * yOffset);

        AddWaypointPosition(positions, mapRoot.GridToWorld(targetBase.GridPosition) + mapRoot.transform.up * yOffset);

        if (positions.Count < 2)
            throw new InvalidOperationException("생성 가능한 Waypoint가 2개 미만입니다.");

        List<Transform> waypoints = new(positions.Count);

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject waypoint = new($"Waypoint_{i:000}");
            waypoint.transform.SetParent(waypointRoot, true);
            waypoint.transform.position = positions[i];
            waypoint.transform.rotation = Quaternion.identity;
            waypoint.transform.localScale = Vector3.one;
            waypoints.Add(waypoint.transform);
        }

        mapRoot.SetWaypoints(waypoints);
        EditorUtility.SetDirty(mapRoot);
    }

    static Vector2Int ResolveEndpointPathCell(
        string endpointName,
        Vector2Int endpointCell,
        IReadOnlyDictionary<Vector2Int, TDMapCellMarker> pathByCell)
    {
        if (pathByCell.ContainsKey(endpointCell)) return endpointCell;

        List<Vector2Int> adjacent = GetPathNeighbors(endpointCell, pathByCell);

        if (adjacent.Count != 1)
            throw new InvalidOperationException(
                $"{endpointName}은 Path 위에 있거나 정확히 하나의 Path 셀과 인접해야 합니다. " +
                $"현재 인접 Path 수: {adjacent.Count}");

        return adjacent[0];
    }

    static void ValidatePathDegrees(IReadOnlyDictionary<Vector2Int, TDMapCellMarker> pathByCell)
    {
        foreach (KeyValuePair<Vector2Int, TDMapCellMarker> pair in pathByCell)
        {
            int neighborCount = GetPathNeighbors(pair.Key, pathByCell).Count;

            if (neighborCount > 2)
                throw new InvalidOperationException(
                    $"Path 분기가 발견되었습니다: {pair.Key} / 인접 Path {neighborCount}개");

            if (pathByCell.Count > 1 && neighborCount == 0)
                throw new InvalidOperationException($"고립된 Path 셀이 있습니다: {pair.Key}");
        }
    }

    static List<Vector2Int> BuildOrderedPath(
        Vector2Int startCell,
        Vector2Int endCell,
        IReadOnlyDictionary<Vector2Int, TDMapCellMarker> pathByCell)
    {
        List<Vector2Int> ordered = new();
        HashSet<Vector2Int> visited = new();

        Vector2Int current = startCell;
        Vector2Int previous = default;
        bool hasPrevious = false;

        while (true)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException($"Path 순환이 발견되었습니다: {current}");

            ordered.Add(current);

            if (current == endCell) break;

            List<Vector2Int> candidates = GetPathNeighbors(current, pathByCell);

            if (hasPrevious) candidates.Remove(previous);

            if (candidates.Count != 1)
                throw new InvalidOperationException(
                    $"Path를 단방향으로 이어갈 수 없습니다: {current} / 다음 후보 {candidates.Count}개");

            previous = current;
            current = candidates[0];
            hasPrevious = true;
        }

        if (visited.Count != pathByCell.Count)
            throw new InvalidOperationException(
                $"Spawn에서 Base까지 연결되지 않은 Path가 있습니다. " +
                $"연결 {visited.Count} / 전체 {pathByCell.Count}");

        return ordered;
    }

    static List<Vector2Int> GetPathNeighbors(
        Vector2Int cell,
        IReadOnlyDictionary<Vector2Int, TDMapCellMarker> pathByCell)
    {
        List<Vector2Int> result = new(2);

        for (int i = 0; i < PathDirections.Length; i++)
        {
            Vector2Int neighbor = cell + PathDirections[i];
            if (pathByCell.ContainsKey(neighbor)) result.Add(neighbor);
        }

        return result;
    }

    static void AddWaypointPosition(List<Vector3> positions, Vector3 position)
    {
        if (positions.Count > 0 && (positions[positions.Count - 1] - position).sqrMagnitude < 0.0001f) return;
        positions.Add(position);
    }

    #endregion

    #region 컴포넌트 연결

    void ConfigureStageController(Scene scene)
    {
        StageController controller = FindComponent<StageController>(scene);

        if (!controller)
            throw new InvalidOperationException("Prototype 씬에서 StageController를 찾지 못했습니다.");

        SerializedObject serializedController = new(controller);
        serializedController.Update();

        SetObject(serializedController, "stageData", stageData);
        SetBool(serializedController, "useRunContextStageData", true);

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    void ConfigureRuntimeSystems(Scene scene, TDMapRoot mapRoot)
    {
        GridGenerator gridGenerator = mapRoot.GridGenerator
            ? mapRoot.GridGenerator
            : mapRoot.GetComponentInChildren<GridGenerator>(true);

        GridDrawer gridDrawer = mapRoot.GridDrawer
            ? mapRoot.GridDrawer
            : mapRoot.GetComponentInChildren<GridDrawer>(true);

        MapBoundsInfoProvider boundsProvider = mapRoot.MapBoundsInfoProvider
            ? mapRoot.MapBoundsInfoProvider
            : mapRoot.GetComponentInChildren<MapBoundsInfoProvider>(true);

        MapController mapController = mapRoot.GetComponentInChildren<MapController>(true);
        BoxCollider gridBounds = mapRoot.GridBounds;
        BoxCollider cameraBounds = mapRoot.CameraBounds as BoxCollider;

        if (!gridGenerator || !gridDrawer || !boundsProvider || !mapController || !gridBounds || !cameraBounds)
            throw new InvalidOperationException(
                "Map Prefab 내부 MapRuntime 구성이 완성되지 않았습니다.\n" +
                "맵 프리팹을 TDMapPrefabSaver로 다시 저장해 주세요.");

        MonsterManager monsterManager = RequireComponent<MonsterManager>(scene);
        MonsterManagerStageProvider stageProvider = RequireComponent<MonsterManagerStageProvider>(scene);
        StageMonsterTracker monsterTracker = RequireComponent<StageMonsterTracker>(scene);
        StageController stageController = RequireComponent<StageController>(scene);
        BuildSystem buildSystem = RequireComponent<BuildSystem>(scene);

        ConfigureMonsterPath(monsterManager, mapRoot);
        ConfigureMonsterStageProvider(stageProvider, monsterManager);

        mapRoot.SetBounds(gridBounds, gridBounds, cameraBounds);
        mapRoot.SetRuntimeComponents(gridGenerator, gridDrawer, boundsProvider);
        EditorUtility.SetDirty(mapRoot);

        ConfigureRuntimeBinder(
            scene,
            stageController,
            mapRoot,
            monsterManager,
            stageProvider,
            monsterTracker,
            buildSystem);
    }

    void ConfigureMonsterPath(MonsterManager monsterManager, TDMapRoot mapRoot)
    {
        SerializedObject serializedManager = new(monsterManager);
        serializedManager.Update();

        SetBool(serializedManager, "useAutoSpawn", false);

        SerializedProperty paths = serializedManager.FindProperty("paths");

        if (paths == null)
            throw new InvalidOperationException("MonsterManager.paths를 찾지 못했습니다.");

        paths.arraySize = 1;

        SerializedProperty path = paths.GetArrayElementAtIndex(0);
        SerializedProperty pathName = path.FindPropertyRelative("pathName");
        SerializedProperty waypoints = path.FindPropertyRelative("waypoints");

        if (pathName != null) pathName.stringValue = stageData.StageId;

        if (waypoints == null)
            throw new InvalidOperationException("MonsterManager PathData.waypoints를 찾지 못했습니다.");

        waypoints.arraySize = mapRoot.Waypoints.Count;

        for (int i = 0; i < mapRoot.Waypoints.Count; i++)
            waypoints.GetArrayElementAtIndex(i).objectReferenceValue = mapRoot.Waypoints[i];

        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(monsterManager);
    }

    static void ConfigureMonsterStageProvider(
        MonsterManagerStageProvider provider,
        MonsterManager monsterManager)
    {
        SerializedObject serializedProvider = new(provider);
        serializedProvider.Update();

        SetObject(serializedProvider, "monsterManager", monsterManager);

        SerializedProperty pathIndex = serializedProvider.FindProperty("pathIndex");
        if (pathIndex != null) pathIndex.intValue = 0;

        serializedProvider.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(provider);
    }


    void ConfigureRuntimeBinder(
        Scene scene,
        StageController stageController,
        TDMapRoot mapRoot,
        MonsterManager monsterManager,
        MonsterManagerStageProvider stageProvider,
        StageMonsterTracker monsterTracker,
        BuildSystem buildSystem)
    {
        TDStageRuntimeBinder binder = FindComponent<TDStageRuntimeBinder>(scene);

        if (!binder)
        {
            GameObject binderObject = new("TDStageRuntime");
            SceneManager.MoveGameObjectToScene(binderObject, scene);
            binder = binderObject.AddComponent<TDStageRuntimeBinder>();
        }

        binder.Configure(
            stageData,
            stageController,
            mapRoot,
            monsterManager,
            stageProvider,
            monsterTracker,
            buildSystem);

        EditorUtility.SetDirty(binder);
    }

    static void ConfigureCameraStart(Scene scene, TDMapRoot mapRoot)
    {
        TopViewCameraController topView = FindComponent<TopViewCameraController>(scene);
        if (!topView) return;
        if (!mapRoot.TryCalculateTerrainBounds(out Bounds bounds)) return;

        SerializedObject serializedTopView = new(topView);
        SerializedProperty targetProperty = serializedTopView.FindProperty("topViewTarget");
        Transform target = targetProperty?.objectReferenceValue as Transform;

        if (!target) return;

        Vector3 position = target.position;
        position.x = bounds.center.x;
        position.z = bounds.center.z;
        target.position = position;
        EditorUtility.SetDirty(target);
    }

    #endregion

    #region StageData 및 씬 동기화

    void SyncStageData(string sceneName)
    {
        Undo.RecordObject(stageData, "StageData 씬 이름 동기화");

        stageData.StageId = sceneName;
        if (string.IsNullOrWhiteSpace(stageData.DisplayName)) stageData.DisplayName = sceneName;

        EditorUtility.SetDirty(stageData);
        AssetDatabase.SaveAssetIfDirty(stageData);
    }

    void RegisterStageDataAddressable()
    {
        string assetPath = AssetDatabase.GetAssetPath(stageData);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning("[TD Stage Builder] StageDataSO의 GUID를 찾지 못해 Addressables 등록을 건너뜁니다.", stageData);
            return;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (!settings)
        {
            Debug.LogWarning("[TD Stage Builder] AddressableAssetSettings가 없어 StageData 라벨 등록을 건너뜁니다.", stageData);
            return;
        }

        settings.AddLabel(StageDataLabel);

        AddressableAssetEntry entry = settings.FindAssetEntry(guid) ??
                                      settings.CreateOrMoveEntry(guid, settings.DefaultGroup);

        entry.SetLabel(StageDataLabel, true, true);
        EditorUtility.SetDirty(settings);
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (!string.Equals(scenes[i].path, scenePath, StringComparison.OrdinalIgnoreCase)) continue;

            scenes[i] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    #endregion

    #region 검증 및 UI

    void DrawPreview()
    {
        string sceneName = GetSceneName();
        string folderPath = GetOutputFolderPath();
        string targetPath = string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(folderPath)
            ? "-"
            : $"{folderPath}/{sceneName}.unity";

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("동기화 결과", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scene Name", string.IsNullOrEmpty(sceneName) ? "-" : sceneName);
            EditorGUILayout.LabelField("StageId", stageData ? stageData.StageId : "-");
            EditorGUILayout.LabelField("Scene Path", targetPath);
        }
    }

    bool CanBuild()
        => prototypeScene && mapPrefab && stageData && !string.IsNullOrEmpty(GetOutputFolderPath());

    bool ValidateInput(
        out TDMapRoot prefabMapRoot,
        out string sceneName,
        out string targetPath)
    {
        prefabMapRoot = null;
        sceneName = string.Empty;
        targetPath = string.Empty;

        if (!prototypeScene || !mapPrefab || !stageData)
        {
            EditorUtility.DisplayDialog(
                "TD Stage Builder",
                "Prototype Scene, Map Prefab, Stage Data를 모두 지정해 주세요.",
                "확인");
            return false;
        }

        string prototypePath = AssetDatabase.GetAssetPath(prototypeScene);

        if (string.IsNullOrEmpty(prototypePath) || !prototypePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "올바른 Prototype Scene을 지정해 주세요.", "확인");
            return false;
        }

        if (!PrefabUtility.IsPartOfPrefabAsset(mapPrefab))
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "Map Prefab에는 Project의 Prefab Asset을 지정해 주세요.", "확인");
            return false;
        }

        prefabMapRoot = mapPrefab.GetComponent<TDMapRoot>();

        if (!prefabMapRoot)
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "Map Prefab 루트에 TDMapRoot가 없습니다.", "확인");
            return false;
        }

        if (!prefabMapRoot.HasHierarchy)
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "Map Prefab의 기본 계층 참조가 완성되지 않았습니다.", "확인");
            return false;
        }

        if (!prefabMapRoot.HasEndpoints)
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "Map Prefab에 Spawn과 Base가 모두 필요합니다.", "확인");
            return false;
        }

        if (!ValidateMapRuntime(prefabMapRoot)) return false;

        sceneName = GetSceneName();

        if (string.IsNullOrEmpty(sceneName))
        {
            EditorUtility.DisplayDialog(
                "TD Stage Builder",
                "StageDataSO.StageId 또는 StageDataSO 에셋 이름으로 씬 이름을 만들 수 없습니다.",
                "확인");
            return false;
        }

        string folderPath = GetOutputFolderPath();

        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog("TD Stage Builder", "생성 폴더를 지정해 주세요.", "확인");
            return false;
        }

        EnsureFolder(folderPath);
        targetPath = $"{folderPath}/{sceneName}.unity";

        if (string.Equals(targetPath, prototypePath, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "TD Stage Builder",
                "생성 대상 씬 경로가 Prototype 씬과 같습니다. StageId 또는 생성 폴더를 변경해 주세요.",
                "확인");
            return false;
        }

        return true;
    }

    static bool ValidateMapRuntime(TDMapRoot mapRoot)
    {
        GridGenerator gridGenerator = mapRoot.GridGenerator
            ? mapRoot.GridGenerator
            : mapRoot.GetComponentInChildren<GridGenerator>(true);

        GridDrawer gridDrawer = mapRoot.GridDrawer
            ? mapRoot.GridDrawer
            : mapRoot.GetComponentInChildren<GridDrawer>(true);

        MapBoundsInfoProvider boundsProvider = mapRoot.MapBoundsInfoProvider
            ? mapRoot.MapBoundsInfoProvider
            : mapRoot.GetComponentInChildren<MapBoundsInfoProvider>(true);

        MapController mapController = mapRoot.GetComponentInChildren<MapController>(true);
        BoxCollider cameraBounds = mapRoot.CameraBounds as BoxCollider;

        if (gridGenerator && gridDrawer && boundsProvider && mapController && mapRoot.GridBounds && cameraBounds)
            return true;

        EditorUtility.DisplayDialog(
            "TD Stage Builder",
            "Map Prefab 내부 MapRuntime 구성이 없습니다.\n\n" +
            "필요 항목:\n" +
            "- GridGenerator\n" +
            "- GridDrawer\n" +
            "- MapBoundsInfoProvider\n" +
            "- MapController\n" +
            "- GridBound / CameraBound\n\n" +
            "TDMapPrefabSaver로 맵 프리팹을 다시 저장해 주세요.",
            "확인");

        return false;
    }

    string GetSceneName()
    {
        if (!stageData) return string.Empty;

        string source = string.IsNullOrWhiteSpace(stageData.StageId)
            ? stageData.name
            : stageData.StageId;

        return SanitizeFileName(source.Trim());
    }

    string GetOutputFolderPath()
    {
        string path = outputFolder ? AssetDatabase.GetAssetPath(outputFolder) : DefaultOutputFolder;
        return AssetDatabase.IsValidFolder(path) ? path : DefaultOutputFolder;
    }

    static SceneAsset FindPrototypeScene()
    {
        string[] guids = AssetDatabase.FindAssets("Prototype t:Scene");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (string.Equals(Path.GetFileNameWithoutExtension(path), "Prototype", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }

    #endregion

    #region 공통 유틸

    static T RequireComponent<T>(Scene scene) where T : Component
    {
        T component = FindComponent<T>(scene);

        if (!component)
            throw new InvalidOperationException($"Prototype 씬에서 {typeof(T).Name}을 찾지 못했습니다.");

        return component;
    }

    static T FindComponent<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component) return component;
        }

        return null;
    }


    static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
    }

    static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        value = value.Replace('/', '_').Replace('\\', '_').Trim();
        return value;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName)) return;
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    #endregion
}

#endif
