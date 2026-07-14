#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TDMapPrefabSaver
{
    const string DefaultFolder = "Assets/MapSystem/Maps";
    const string RuntimeRootName = "MapRuntime";
    const string GridBoundName = "GridBound";
    const string CameraBoundName = "CameraBound";
    const string GridMeshName = "GridMesh";

    // Prototype 씬의 GridDrawer.gridMaterial에 연결된 머티리얼 GUID.
    // 프로젝트에서 머티리얼이 이동해도 GUID가 유지되는 한 정상적으로 찾는다.
    const string DefaultGridMaterialGuid = "75f4aaa27ecccc6428809e1e3cc1232e";

    [MenuItem("Tools/Grid Map Painter/선택 MapRoot 프리팹 저장")]
    public static void SaveSelectedMapRoot()
    {
        TDMapRoot mapRoot = ResolveMapRoot();

        if (!mapRoot)
        {
            EditorUtility.DisplayDialog(
                "TD Map Prefab",
                "저장할 TDMapRoot를 선택해 주세요.\n\n씬에 TDMapRoot가 하나만 있다면 자동으로 찾습니다.",
                "확인");
            return;
        }

        SaveMapRoot(mapRoot);
    }

    [MenuItem("Tools/Grid Map Painter/선택 MapRoot 런타임 구성 갱신")]
    public static void ConfigureSelectedMapRootRuntime()
    {
        TDMapRoot mapRoot = ResolveMapRoot();

        if (!mapRoot)
        {
            EditorUtility.DisplayDialog("TD Map Runtime", "구성할 TDMapRoot를 선택해 주세요.", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(mapRoot.gameObject, "TD Map 런타임 구성");
        ConfigureRuntime(mapRoot);
        EditorUtility.SetDirty(mapRoot);
        Selection.activeGameObject = mapRoot.gameObject;

        Debug.Log("[TD Map] MapRoot 내부 런타임 구성을 갱신했습니다.", mapRoot);
    }

    public static GameObject SaveMapRoot(TDMapRoot mapRoot)
    {
        if (!mapRoot || !ValidateMapRoot(mapRoot)) return null;

        EnsureFolder(DefaultFolder);

        string assetPath = EditorUtility.SaveFilePanelInProject(
            "TD Map Prefab 저장",
            GetDefaultPrefabName(mapRoot.name),
            "prefab",
            "맵 프리팹을 저장할 위치를 선택해 주세요.",
            DefaultFolder);

        if (string.IsNullOrEmpty(assetPath)) return null;

        GameObject clone = null;

        try
        {
            clone = Object.Instantiate(mapRoot.gameObject);
            clone.name = mapRoot.name;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;

            TDMapRoot cloneRoot = clone.GetComponent<TDMapRoot>();
            PreparePrefabClone(cloneRoot);
            ConfigureRuntime(cloneRoot);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, assetPath, out bool success);

            if (!success || !prefab)
            {
                EditorUtility.DisplayDialog(
                    "TD Map Prefab",
                    "맵 프리팹 저장에 실패했습니다.\nConsole을 확인해 주세요.",
                    "확인");
                return null;
            }

            AssetDatabase.SaveAssets();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log(
                $"[TD Map] 맵 프리팹 저장 완료\n" +
                $"MapRoot: {mapRoot.name}\n" +
                $"저장 경로: {assetPath}\n" +
                "MapRuntime: GridGenerator / GridDrawer / Bounds / MapController 포함",
                prefab);

            EditorUtility.DisplayDialog(
                "TD Map Prefab",
                $"맵 프리팹 저장이 완료되었습니다.\n\n{assetPath}",
                "확인");

            return prefab;
        }
        finally
        {
            if (clone) Object.DestroyImmediate(clone);
        }
    }

    #region 런타임 구성

    public static void ConfigureRuntime(TDMapRoot mapRoot)
    {
        if (!mapRoot) return;

        mapRoot.FindHierarchyReferences();
        mapRoot.RefreshEndpointReferences();
        mapRoot.RebuildCellCache();

        if (!TryCalculateLocalTerrainBounds(mapRoot, out Bounds terrainBounds))
            throw new System.InvalidOperationException("Ground/Path Bounds를 계산할 수 없습니다.");

        Transform runtimeRoot = GetOrCreateChild(mapRoot.transform, RuntimeRootName);
        ResetLocalTransform(runtimeRoot);

        GridGenerator gridGenerator = GetOrAddComponent<GridGenerator>(runtimeRoot.gameObject);
        TDMapBuildGridProvider buildGridProvider = GetOrAddComponent<TDMapBuildGridProvider>(mapRoot.gameObject);
        MapBoundsInfoProvider boundsProvider = GetOrAddComponent<MapBoundsInfoProvider>(runtimeRoot.gameObject);
        MapController mapController = GetOrAddComponent<MapController>(runtimeRoot.gameObject);

        Transform gridBoundRoot = GetOrCreateChild(runtimeRoot, GridBoundName);
        Transform cameraBoundRoot = GetOrCreateChild(runtimeRoot, CameraBoundName);
        Transform gridMeshRoot = GetOrCreateChild(runtimeRoot, GridMeshName);

        BoxCollider gridBound = GetOrAddComponent<BoxCollider>(gridBoundRoot.gameObject);
        BoxCollider cameraBound = GetOrAddComponent<BoxCollider>(cameraBoundRoot.gameObject);

        MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(gridMeshRoot.gameObject);
        MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(gridMeshRoot.gameObject);
        GridDrawer gridDrawer = GetOrAddComponent<GridDrawer>(gridMeshRoot.gameObject);

        ConfigureBounds(mapRoot, terrainBounds, gridBoundRoot, gridBound, cameraBoundRoot, cameraBound);
        ConfigureGridMesh(mapRoot, gridMeshRoot, meshFilter, meshRenderer, gridDrawer);
        ConfigureGridGenerator(mapRoot, gridGenerator, gridDrawer, gridBound);
        ConfigureBoundsProvider(boundsProvider, gridBound, cameraBound);
        ConfigureMapController(mapRoot, mapController, boundsProvider);
        ConfigureGridGenerator(mapRoot, gridGenerator, gridDrawer, gridBound);

        buildGridProvider.Configure(mapRoot);

        mapRoot.SetBounds(gridBound, gridBound, cameraBound);
        mapRoot.SetRuntimeComponents(gridGenerator, gridDrawer, boundsProvider);

        EditorUtility.SetDirty(gridGenerator);
        EditorUtility.SetDirty(buildGridProvider);
        EditorUtility.SetDirty(gridDrawer);
        EditorUtility.SetDirty(boundsProvider);
        EditorUtility.SetDirty(mapController);
        EditorUtility.SetDirty(mapRoot);
    }

    static void ConfigureBounds(
        TDMapRoot mapRoot,
        Bounds terrainBounds,
        Transform gridBoundRoot,
        BoxCollider gridBound,
        Transform cameraBoundRoot,
        BoxCollider cameraBound)
    {
        float padding = mapRoot.Palette ? mapRoot.Palette.CameraBoundsPadding : 2f;

        ResetLocalTransform(gridBoundRoot);
        gridBoundRoot.localPosition = terrainBounds.center;
        gridBound.center = Vector3.zero;
        gridBound.size = new Vector3(
            Mathf.Max(mapRoot.CellSize, terrainBounds.size.x),
            4f,
            Mathf.Max(mapRoot.CellSize, terrainBounds.size.z));
        gridBound.isTrigger = false;
        gridBound.enabled = true;

        ResetLocalTransform(cameraBoundRoot);
        cameraBoundRoot.localPosition = terrainBounds.center;
        cameraBound.center = Vector3.zero;
        cameraBound.size = new Vector3(
            Mathf.Max(mapRoot.CellSize, terrainBounds.size.x + padding * 2f),
            0.1f,
            Mathf.Max(mapRoot.CellSize, terrainBounds.size.z + padding * 2f));
        cameraBound.isTrigger = true;
        cameraBound.enabled = true;
    }

    static void ConfigureGridMesh(
        TDMapRoot mapRoot,
        Transform gridMeshRoot,
        MeshFilter meshFilter,
        MeshRenderer meshRenderer,
        GridDrawer gridDrawer)
    {
        ResetLocalTransform(gridMeshRoot);
        gridMeshRoot.localPosition = new Vector3(0f, mapRoot.SurfaceYOffset + 0.02f, 0f);

        Material material = ResolveGridMaterial(gridDrawer, meshRenderer);

        SerializedObject serializedDrawer = new(gridDrawer);
        serializedDrawer.Update();
        SetObject(serializedDrawer, "meshFilter", meshFilter);
        SetObject(serializedDrawer, "meshRenderer", meshRenderer);
        if (material) SetObject(serializedDrawer, "gridMaterial", material);
        serializedDrawer.ApplyModifiedPropertiesWithoutUndo();

        if (material) meshRenderer.sharedMaterial = material;
    }

    static void ConfigureGridGenerator(
        TDMapRoot mapRoot,
        GridGenerator gridGenerator,
        GridDrawer gridDrawer,
        BoxCollider gridBound)
    {
        SerializedObject serializedGrid = new(gridGenerator);
        serializedGrid.Update();

        SetObject(serializedGrid, "gridDrawer", gridDrawer);
        SetObject(serializedGrid, "gridBound", gridBound);

        SerializedProperty cellSize = serializedGrid.FindProperty("cellSize");
        if (cellSize != null) cellSize.intValue = Mathf.Max(1, Mathf.RoundToInt(mapRoot.CellSize));

        if (mapRoot.Palette)
        {
            SerializedProperty floorLayer = serializedGrid.FindProperty("floorLayer");
            SerializedProperty obstacleLayer = serializedGrid.FindProperty("obstacleLayer");

            if (floorLayer != null) floorLayer.intValue = mapRoot.Palette.FloorLayer.value;
            if (obstacleLayer != null) obstacleLayer.intValue = mapRoot.Palette.ObstacleLayer.value;
        }

        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureBoundsProvider(
        MapBoundsInfoProvider boundsProvider,
        BoxCollider gridBound,
        BoxCollider cameraBound)
    {
        SerializedObject serializedProvider = new(boundsProvider);
        serializedProvider.Update();

        SetObject(serializedProvider, "mapBoundsCollider", gridBound);
        SetObject(serializedProvider, "cameraBoundsCollider", cameraBound);

        serializedProvider.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureMapController(
        TDMapRoot mapRoot,
        MapController mapController,
        MapBoundsInfoProvider boundsProvider)
    {
        SerializedObject serializedMap = new(mapController);
        serializedMap.Update();

        SerializedProperty initialProviders = serializedMap.FindProperty("initialProviders");

        if (initialProviders != null)
        {
            initialProviders.arraySize = 1;
            initialProviders.GetArrayElementAtIndex(0).objectReferenceValue = boundsProvider;
        }

        SerializedProperty enemyGridCellSize = serializedMap.FindProperty("enemyGridCellSize");
        if (enemyGridCellSize != null) enemyGridCellSize.floatValue = Mathf.Max(1f, mapRoot.CellSize);

        serializedMap.ApplyModifiedPropertiesWithoutUndo();
    }

    static Material ResolveGridMaterial(GridDrawer gridDrawer, MeshRenderer meshRenderer)
    {
        SerializedObject serializedDrawer = new(gridDrawer);
        SerializedProperty materialProperty = serializedDrawer.FindProperty("gridMaterial");
        Material material = materialProperty?.objectReferenceValue as Material;

        if (material) return material;
        if (meshRenderer && meshRenderer.sharedMaterial) return meshRenderer.sharedMaterial;

        string materialPath = AssetDatabase.GUIDToAssetPath(DefaultGridMaterialGuid);
        material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (!material)
            Debug.LogWarning(
                "[TD Map] Prototype Grid Material을 찾지 못했습니다. " +
                "MapRuntime/GridMesh의 GridDrawer와 MeshRenderer에 머티리얼을 직접 연결해 주세요.");

        return material;
    }

    static bool TryCalculateLocalTerrainBounds(TDMapRoot mapRoot, out Bounds bounds)
    {
        IReadOnlyList<TDMapCellMarker> markers = mapRoot.CellMarkers;
        bool found = false;
        int minX = 0;
        int maxX = 0;
        int minY = 0;
        int maxY = 0;

        for (int i = 0; i < markers.Count; i++)
        {
            TDMapCellMarker marker = markers[i];
            if (!marker || !marker.IsGround && !marker.IsPath) continue;

            Vector2Int cell = marker.GridPosition;

            if (!found)
            {
                minX = maxX = cell.x;
                minY = maxY = cell.y;
                found = true;
                continue;
            }

            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        if (!found)
        {
            bounds = default;
            return false;
        }

        float cellSize = mapRoot.CellSize;
        Vector3 center = new(
            (minX + maxX) * 0.5f * cellSize,
            mapRoot.TileY,
            (minY + maxY) * 0.5f * cellSize);

        Vector3 size = new(
            (maxX - minX + 1) * cellSize,
            1f,
            (maxY - minY + 1) * cellSize);

        bounds = new Bounds(center, size);
        return true;
    }

    #endregion

    #region 저장 전 처리

    static bool ValidateMapRoot(TDMapRoot mapRoot)
    {
        if (!mapRoot.HasHierarchy) mapRoot.FindHierarchyReferences();
        mapRoot.RebuildCellCache();

        if (!mapRoot.HasHierarchy)
        {
            EditorUtility.DisplayDialog(
                "TD Map 검증 실패",
                "MapRoot의 기본 계층을 찾지 못했습니다.\n\n" +
                "Ground / Path / SpawnBase / Decoration / PathWaypoints를 확인해 주세요.",
                "확인");
            return false;
        }

        int groundCount = 0;
        int pathCount = 0;
        int spawnCount = 0;
        int baseCount = 0;
        IReadOnlyList<TDMapCellMarker> markers = mapRoot.CellMarkers;

        for (int i = 0; i < markers.Count; i++)
        {
            TDMapCellMarker marker = markers[i];
            if (!marker) continue;

            switch (marker.CellType)
            {
                case TDMapCellType.Ground: groundCount++; break;
                case TDMapCellType.Path: pathCount++; break;
                case TDMapCellType.Spawn: spawnCount++; break;
                case TDMapCellType.Base: baseCount++; break;
            }
        }

        List<string> errors = new();

        if (groundCount + pathCount == 0) errors.Add("- Ground 또는 Path가 하나도 없습니다.");
        if (pathCount == 0) errors.Add("- Path가 하나도 없습니다.");
        if (spawnCount != 1) errors.Add($"- Spawn은 정확히 1개여야 합니다. 현재: {spawnCount}");
        if (baseCount != 1) errors.Add($"- Base는 정확히 1개여야 합니다. 현재: {baseCount}");

        int missingScriptCount = CountMissingScripts(mapRoot.transform);
        if (missingScriptCount > 0) errors.Add($"- Missing Script가 {missingScriptCount}개 있습니다.");

        if (errors.Count == 0) return true;

        EditorUtility.DisplayDialog("TD Map 검증 실패", string.Join("\n", errors), "확인");
        return false;
    }

    static void PreparePrefabClone(TDMapRoot mapRoot)
    {
        if (!mapRoot) return;

        mapRoot.FindHierarchyReferences();
        mapRoot.RefreshEndpointReferences();

        List<Transform> internalWaypoints = new();

        for (int i = 0; i < mapRoot.Waypoints.Count; i++)
        {
            Transform waypoint = mapRoot.Waypoints[i];
            if (waypoint && waypoint.IsChildOf(mapRoot.transform)) internalWaypoints.Add(waypoint);
        }

        mapRoot.SetWaypoints(internalWaypoints);
        mapRoot.RebuildCellCache();
        EditorUtility.SetDirty(mapRoot);
    }

    static int CountMissingScripts(Transform root)
    {
        if (!root) return 0;

        int count = 0;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
            count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);

        return count;
    }

    #endregion

    #region 공통

    static TDMapRoot ResolveMapRoot()
    {
        if (Selection.activeGameObject)
        {
            TDMapRoot selectedRoot = Selection.activeGameObject.GetComponentInParent<TDMapRoot>();
            if (selectedRoot) return selectedRoot;
        }

        TDMapRoot[] roots = Object.FindObjectsByType<TDMapRoot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return roots.Length == 1 ? roots[0] : null;
    }

    static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child) return child;

        GameObject childObject = new(childName);
        childObject.transform.SetParent(parent, false);
        ResetLocalTransform(childObject.transform);
        return childObject.transform;
    }

    static T GetOrAddComponent<T>(GameObject target) where T : Component
        => target.TryGetComponent(out T component) ? component : target.AddComponent<T>();

    static void ResetLocalTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    static string GetDefaultPrefabName(string mapRootName)
    {
        string prefabName = string.IsNullOrWhiteSpace(mapRootName) ? "TD_Map" : mapRootName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            prefabName = prefabName.Replace(invalidChar, '_');

        return prefabName.EndsWith("_Prefab") ? prefabName : $"{prefabName}_Prefab";
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parentPath = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName)) return;
        if (!AssetDatabase.IsValidFolder(parentPath)) EnsureFolder(parentPath);

        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    #endregion
}

#endif