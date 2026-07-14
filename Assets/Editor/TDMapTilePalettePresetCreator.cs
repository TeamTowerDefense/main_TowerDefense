#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public static class TDMapTilePalettePresetCreator
{
    const string PaletteFolder = "Assets/MapSystem/Palettes";
    const string PalettePath = PaletteFolder + "/TDMapTilePalette_Default.asset";

    const string GroundLayerName = "Ground";
    const string PathLayerName = "EnemyTile";
    const string ObstacleLayerName = "MapObstacle";
    const string DecorationLayerName = "Decoration";

    [MenuItem("Tools/TD Map/기본 타일 팔레트 생성 또는 갱신")]
    public static void CreateOrUpdateDefaultPalette()
    {
        EnsureFolder("Assets/MapSystem");
        EnsureFolder(PaletteFolder);

        LayerIndices layers = EnsureLayers();

        if (layers.ground < 0 || layers.path < 0)
        {
            EditorUtility.DisplayDialog(
                "TD Map Palette",
                "Ground 또는 EnemyTile 레이어를 찾지 못했습니다.\n" +
                "현재 프로젝트의 Layers 설정을 확인해 주세요.",
                "확인"
            );

            return;
        }

        TDMapTilePaletteSO palette =
            AssetDatabase.LoadAssetAtPath<TDMapTilePaletteSO>(PalettePath);

        bool created = !palette;

        if (created)
        {
            palette = ScriptableObject.CreateInstance<TDMapTilePaletteSO>();
            AssetDatabase.CreateAsset(palette, PalettePath);
        }

        SerializedObject serializedPalette = new SerializedObject(palette);
        serializedPalette.Update();

        ConfigureGroundTiles(serializedPalette);
        ConfigurePathTiles(serializedPalette);
        ConfigureSpecialRules(serializedPalette);
        ConfigureLayers(serializedPalette, layers);
        ConfigureDefaults(serializedPalette);

        serializedPalette.ApplyModifiedProperties();

        EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = palette;
        EditorGUIUtility.PingObject(palette);

        string message = created
            ? "기본 TD 맵 타일 팔레트를 생성했습니다."
            : "기존 TD 맵 타일 팔레트를 갱신했습니다.";

        Debug.Log(
            $"[TD Map] {message}\n" +
            $"경로: {PalettePath}\n" +
            $"Ground Layer: {layers.ground}\n" +
            $"EnemyTile Layer: {layers.path}\n" +
            $"MapObstacle Layer: {layers.obstacle}\n" +
            $"Decoration Layer: {layers.decoration}",
            palette
        );

        EditorUtility.DisplayDialog(
            "TD Map Palette",
            message +
            "\n\nSpawn Prefab과 Base Prefab은 직접 연결해 주세요." +
            "\nTile_Road_Inner_Corner_3_Edge는 분기 미리보기용으로 등록됩니다.",
            "확인"
        );
    }

    #region 기본 타일 설정

    static void ConfigureGroundTiles(SerializedObject serializedPalette)
    {
        SerializedProperty groundTiles =
            serializedPalette.FindProperty("groundTiles");

        SetObject(
            groundTiles,
            "center",
            FindPrefab("Tile_Center")
        );

        SetObject(
            groundTiles,
            "edge",
            FindPrefab("Tile_Edge")
        );

        SetObject(
            groundTiles,
            "corner",
            FindPrefab("Tile_Corner")
        );

        SetObject(
            groundTiles,
            "solo",
            FindPrefab("Tile_Solo_Island")
        );

        SetFloat(groundTiles, "centerBaseYRotation", 0f);
        SetFloat(groundTiles, "edgeBaseYRotation", 0f);
        SetFloat(groundTiles, "cornerBaseYRotation", 0f);
        SetFloat(groundTiles, "soloBaseYRotation", 0f);
    }

    static void ConfigurePathTiles(SerializedObject serializedPalette)
    {
        SerializedProperty pathTiles =
            serializedPalette.FindProperty("pathTiles");

        SetObject(
            pathTiles,
            "straight",
            FindPrefab("Tile_Road_Solo")
        );

        SetObject(
            pathTiles,
            "corner",
            FindPrefab("Tile_Road_Solo_Corner")
        );

        SetObject(
            pathTiles,
            "end",
            FindPrefab("Tile_Road_End")
        );

        // 완전히 고립된 Path 전용 프리팹이 없으므로
        // 편집 중 임시 표시에는 End를 재사용한다.
        // 완성 검증에서는 고립된 Path를 저장하지 못하게 막는다.
        SetObject(
            pathTiles,
            "solo",
            FindPrefab("Tile_Road_End")
        );

        SetFloat(pathTiles, "straightBaseYRotation", 0f);
        SetFloat(pathTiles, "cornerBaseYRotation", 0f);
        SetFloat(pathTiles, "endBaseYRotation", 0f);
        SetFloat(pathTiles, "soloBaseYRotation", 0f);
    }

    #endregion

    #region 특수 규칙

    static void ConfigureSpecialRules(SerializedObject serializedPalette)
    {
        SerializedProperty rules =
            serializedPalette.FindProperty("specialRules");

        rules.arraySize = 5;

        // 사진 1:
        // Ground = 북, 동, 남
        // Path   = 서
        ConfigureRule(
            rules.GetArrayElementAtIndex(0),
            "Road End Edge",
            "Tile_Road_End_Edge",
            500,
            TDNeighborMask.North |
            TDNeighborMask.East |
            TDNeighborMask.South,
            TDNeighborMask.West
        );

        // 사진 2:
        // Ground = 동
        // Path   = 북, 남, 서
        //
        // 현재 단일 경로에서는 최종 저장 불가인 분기 형태지만
        // 제작 도중의 시각 갱신을 위해 등록한다.
        ConfigureRule(
            rules.GetArrayElementAtIndex(1),
            "Branch Preview - Inner Corner 3 Edge",
            "Tile_Road_Inner_Corner_3_Edge",
            600,
            TDNeighborMask.East,
            TDNeighborMask.North |
            TDNeighborMask.South |
            TDNeighborMask.West
        );

        // 사진 3:
        // Ground 없음
        // Path = 북, 서
        ConfigureRule(
            rules.GetArrayElementAtIndex(2),
            "Corner Foundation End",
            "Tile_Corner_Foundation_End",
            450,
            TDNeighborMask.None,
            TDNeighborMask.North |
            TDNeighborMask.West
        );

        // 사진 4:
        // Ground 없음
        // Path = 북
        ConfigureRule(
            rules.GetArrayElementAtIndex(3),
            "Edge Foundation End",
            "Tile_Edge_Foundation_End",
            450,
            TDNeighborMask.None,
            TDNeighborMask.North
        );

        // 사진 5:
        // Ground = 북, 남
        // Path   = 동, 서
        ConfigureRule(
            rules.GetArrayElementAtIndex(4),
            "Road Edge End Solo",
            "Tile_Road_Edge_End_Solo",
            400,
            TDNeighborMask.North |
            TDNeighborMask.South,
            TDNeighborMask.East |
            TDNeighborMask.West
        );
    }

    static void ConfigureRule(
        SerializedProperty rule,
        string ruleName,
        string prefabName,
        int priority,
        TDNeighborMask groundMask,
        TDNeighborMask pathMask)
    {
        TDNeighborMask forbiddenGround =
            TDNeighborMask.All & ~groundMask;

        TDNeighborMask forbiddenPath =
            TDNeighborMask.All & ~pathMask;

        rule.FindPropertyRelative("ruleName").stringValue = ruleName;

        rule.FindPropertyRelative("targetType").enumValueIndex =
            (int)TDMapCellType.Path;

        rule.FindPropertyRelative("prefab").objectReferenceValue =
            FindPrefab(prefabName);

        rule.FindPropertyRelative("priority").intValue = priority;
        rule.FindPropertyRelative("allowRotation").boolValue = true;
        rule.FindPropertyRelative("baseYRotation").floatValue = 0f;

        rule.FindPropertyRelative("requiredTerrain").intValue =
            (int)groundMask;

        rule.FindPropertyRelative("forbiddenTerrain").intValue =
            (int)forbiddenGround;

        rule.FindPropertyRelative("requiredPath").intValue =
            (int)pathMask;

        rule.FindPropertyRelative("forbiddenPath").intValue =
            (int)forbiddenPath;
    }

    #endregion

    #region 레이어 설정

    static void ConfigureLayers(
        SerializedObject serializedPalette,
        LayerIndices layers)
    {
        int floorMask = 1 << layers.ground;

        int obstacleMask =
            (1 << layers.path) |
            (1 << layers.obstacle);

        int decorationMask = 1 << layers.decoration;

        serializedPalette.FindProperty("floorLayer").intValue =
            floorMask;

        serializedPalette.FindProperty("obstacleLayer").intValue =
            obstacleMask;

        serializedPalette.FindProperty("decorationLayer").intValue =
            decorationMask;
    }

    static LayerIndices EnsureLayers()
    {
        Object[] tagManagerAssets =
            AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset"
            );

        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
        {
            Debug.LogError("[TD Map] TagManager.asset을 찾지 못했습니다.");

            return new LayerIndices
            {
                ground = -1,
                path = -1,
                obstacle = -1,
                decoration = -1
            };
        }

        SerializedObject tagManager =
            new SerializedObject(tagManagerAssets[0]);

        SerializedProperty layers =
            tagManager.FindProperty("layers");

        int groundLayer = FindLayer(layers, GroundLayerName);
        int pathLayer = FindLayer(layers, PathLayerName);

        int obstacleLayer =
            FindOrCreateLayer(layers, ObstacleLayerName);

        int decorationLayer =
            FindOrCreateLayer(layers, DecorationLayerName);

        tagManager.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new LayerIndices
        {
            ground = groundLayer,
            path = pathLayer,
            obstacle = obstacleLayer,
            decoration = decorationLayer
        };
    }

    static int FindLayer(
        SerializedProperty layers,
        string layerName)
    {
        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty layer =
                layers.GetArrayElementAtIndex(i);

            if (layer.stringValue == layerName)
                return i;
        }

        return -1;
    }

    static int FindOrCreateLayer(
        SerializedProperty layers,
        string layerName)
    {
        int existingLayer = FindLayer(layers, layerName);
        if (existingLayer >= 0) return existingLayer;

        // Unity 기본 및 현재 프로젝트 레이어를 건드리지 않기 위해
        // User Layer 8번 이후의 첫 빈 공간을 사용한다.
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer =
                layers.GetArrayElementAtIndex(i);

            if (!string.IsNullOrEmpty(layer.stringValue))
                continue;

            layer.stringValue = layerName;

            Debug.Log(
                $"[TD Map] Layer {i}에 '{layerName}'을 생성했습니다."
            );

            return i;
        }

        Debug.LogError(
            $"[TD Map] '{layerName}' 레이어를 생성할 빈 슬롯이 없습니다."
        );

        return -1;
    }

    struct LayerIndices
    {
        public int ground;
        public int path;
        public int obstacle;
        public int decoration;
    }

    #endregion

    #region 공통 설정

    static void ConfigureDefaults(SerializedObject serializedPalette)
    {
        // 현재 프리팹의 BoxCollider가 2x2이므로
        // 1x1 셀에 맞춰 Visual 자식에 0.5배를 적용한다.
        serializedPalette.FindProperty("visualScale").floatValue = 0.5f;

        serializedPalette.FindProperty("waypointYOffset").floatValue = 0f;
        serializedPalette.FindProperty("cameraBoundsPadding").floatValue = 2f;
    }

    static GameObject FindPrefab(string exactName)
    {
        string[] guids =
            AssetDatabase.FindAssets($"{exactName} t:Prefab");

        GameObject firstPartialMatch = null;

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guids[i]);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (!prefab) continue;

            if (!firstPartialMatch)
                firstPartialMatch = prefab;

            if (Path.GetFileNameWithoutExtension(path) == exactName)
                return prefab;
        }

        if (firstPartialMatch)
        {
            Debug.LogWarning(
                $"[TD Map] 정확한 이름의 '{exactName}'을 찾지 못해 " +
                $"'{firstPartialMatch.name}'을 대신 사용합니다.",
                firstPartialMatch
            );

            return firstPartialMatch;
        }

        Debug.LogError(
            $"[TD Map] Prefab을 찾지 못했습니다: {exactName}"
        );

        return null;
    }

    static void SetObject(
        SerializedProperty parent,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            parent.FindPropertyRelative(propertyName);

        if (property != null)
            property.objectReferenceValue = value;
    }

    static void SetFloat(
        SerializedProperty parent,
        string propertyName,
        float value)
    {
        SerializedProperty property =
            parent.FindPropertyRelative(propertyName);

        if (property != null)
            property.floatValue = value;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent =
            Path.GetDirectoryName(folderPath)?.Replace("\\", "/");

        string folderName =
            Path.GetFileName(folderPath);

        if (string.IsNullOrEmpty(parent) ||
            string.IsNullOrEmpty(folderName))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    #endregion
}

#endif