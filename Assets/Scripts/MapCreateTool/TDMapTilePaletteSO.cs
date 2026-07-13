using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum TDNeighborMask
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
    All = North | East | South | West
}

[Serializable]
public sealed class TDGroundTileSet
{
    [Header("기본 프리팹")]
    [SerializeField] GameObject center;
    [SerializeField] GameObject edge;
    [SerializeField] GameObject corner;
    [SerializeField] GameObject solo;

    [Header("프리팹 기본 Y 회전")]
    [Tooltip("Center는 방향성이 없으면 0으로 둡니다.")]
    [SerializeField] float centerBaseYRotation;

    [Tooltip("Edge는 기본적으로 북쪽 면이 열린 상태로 간주합니다.")]
    [SerializeField] float edgeBaseYRotation;

    [Tooltip("Corner는 기본적으로 북쪽과 동쪽 면이 열린 상태로 간주합니다.")]
    [SerializeField] float cornerBaseYRotation;

    [SerializeField] float soloBaseYRotation;

    public GameObject Center => center;
    public GameObject Edge => edge;
    public GameObject Corner => corner;
    public GameObject Solo => solo;

    public float CenterBaseYRotation => centerBaseYRotation;
    public float EdgeBaseYRotation => edgeBaseYRotation;
    public float CornerBaseYRotation => cornerBaseYRotation;
    public float SoloBaseYRotation => soloBaseYRotation;

    public bool IsValid => center && edge && corner && solo;
}

[Serializable]
public sealed class TDPathTileSet
{
    [Header("기본 프리팹")]
    [SerializeField] GameObject straight;
    [SerializeField] GameObject corner;
    [SerializeField] GameObject end;
    [SerializeField] GameObject solo;

    [Header("프리팹 기본 Y 회전")]
    [Tooltip("Straight는 기본적으로 북쪽-남쪽 방향으로 연결된 상태로 간주합니다.")]
    [SerializeField] float straightBaseYRotation;

    [Tooltip("Corner는 기본적으로 북쪽과 동쪽 방향으로 연결된 상태로 간주합니다.")]
    [SerializeField] float cornerBaseYRotation;

    [Tooltip("End는 기본적으로 북쪽 방향으로 길이 연결된 상태로 간주합니다.")]
    [SerializeField] float endBaseYRotation;

    [SerializeField] float soloBaseYRotation;

    public GameObject Straight => straight;
    public GameObject Corner => corner;
    public GameObject End => end;
    public GameObject Solo => solo;

    public float StraightBaseYRotation => straightBaseYRotation;
    public float CornerBaseYRotation => cornerBaseYRotation;
    public float EndBaseYRotation => endBaseYRotation;
    public float SoloBaseYRotation => soloBaseYRotation;

    public bool IsValid => straight && corner && end && solo;
}

[Serializable]
public sealed class TDDecorationPaletteEntry
{
    [SerializeField] string displayName;
    [SerializeField] GameObject prefab;

    [Header("타워 설치 판정")]
    [SerializeField] bool blocksTower = true;

    [Tooltip("비어 있으면 팔레트의 기본 Decoration 또는 Obstacle 레이어를 사용합니다.")]
    [SerializeField] LayerMask customLayer;

    [SerializeField] bool applyLayerRecursively = true;

    [Header("자유 배치")]
    [SerializeField] float surfaceOffset;
    [SerializeField] bool alignToSurfaceNormal;
    [SerializeField] bool randomYRotation = true;

    [Min(0.01f)]
    [SerializeField] float minScale = 1f;

    [Min(0.01f)]
    [SerializeField] float maxScale = 1f;

    public string DisplayName
        => string.IsNullOrWhiteSpace(displayName)
            ? prefab ? prefab.name : "Decoration"
            : displayName;

    public GameObject Prefab => prefab;
    public bool BlocksTower => blocksTower;
    public LayerMask CustomLayer => customLayer;
    public bool ApplyLayerRecursively => applyLayerRecursively;

    public float SurfaceOffset => surfaceOffset;
    public bool AlignToSurfaceNormal => alignToSurfaceNormal;
    public bool RandomYRotation => randomYRotation;

    public float MinScale => Mathf.Min(minScale, maxScale);
    public float MaxScale => Mathf.Max(minScale, maxScale);

    public bool IsValid => prefab;

    public float GetRandomScale()
        => UnityEngine.Random.Range(MinScale, MaxScale);

    public float GetRandomYRotation()
        => randomYRotation ? UnityEngine.Random.Range(0f, 360f) : 0f;
}

[Serializable]
public sealed class TDMapTileRule
{
    [SerializeField] string ruleName = "New Rule";

    [Tooltip("이 규칙을 적용할 셀 종류입니다.")]
    [SerializeField] TDMapCellType targetType = TDMapCellType.Path;

    [SerializeField] GameObject prefab;

    [Header("규칙 우선순위")]
    [Tooltip("높은 값의 규칙을 먼저 검사합니다.")]
    [SerializeField] int priority = 100;

    [Header("방향")]
    [SerializeField] bool allowRotation = true;

    [Tooltip("마스크가 작성된 기본 방향에서 프리팹에 추가로 적용할 Y 회전입니다.")]
    [SerializeField] float baseYRotation;

    [Header("주변 지형 조건")]
    [Tooltip("반드시 존재해야 하는 Ground/Path/Spawn/Base 방향입니다.")]
    [SerializeField] TDNeighborMask requiredTerrain;

    [Tooltip("반드시 비어 있어야 하는 Ground/Path/Spawn/Base 방향입니다.")]
    [SerializeField] TDNeighborMask forbiddenTerrain;

    [Header("주변 길 조건")]
    [Tooltip("반드시 Path/Spawn/Base가 존재해야 하는 방향입니다.")]
    [SerializeField] TDNeighborMask requiredPath;

    [Tooltip("반드시 Path/Spawn/Base가 없어야 하는 방향입니다.")]
    [SerializeField] TDNeighborMask forbiddenPath;

    public string RuleName => ruleName;
    public TDMapCellType TargetType => targetType;
    public GameObject Prefab => prefab;
    public int Priority => priority;
    public float BaseYRotation => baseYRotation;

    public TDNeighborMask RequiredTerrain => requiredTerrain;
    public TDNeighborMask ForbiddenTerrain => forbiddenTerrain;
    public TDNeighborMask RequiredPath => requiredPath;
    public TDNeighborMask ForbiddenPath => forbiddenPath;

    public bool IsValid => prefab;

    public bool TryMatch(
        TDNeighborMask terrainMask,
        TDNeighborMask pathMask,
        out float yRotation)
    {
        int rotationCount = allowRotation ? 4 : 1;

        for (int quarterTurns = 0; quarterTurns < rotationCount; quarterTurns++)
        {
            TDNeighborMask rotatedRequiredTerrain =
                TDMapDirectionUtility.RotateClockwise(requiredTerrain, quarterTurns);

            TDNeighborMask rotatedForbiddenTerrain =
                TDMapDirectionUtility.RotateClockwise(forbiddenTerrain, quarterTurns);

            TDNeighborMask rotatedRequiredPath =
                TDMapDirectionUtility.RotateClockwise(requiredPath, quarterTurns);

            TDNeighborMask rotatedForbiddenPath =
                TDMapDirectionUtility.RotateClockwise(forbiddenPath, quarterTurns);

            if (!ContainsAll(terrainMask, rotatedRequiredTerrain)) continue;
            if (ContainsAny(terrainMask, rotatedForbiddenTerrain)) continue;

            if (!ContainsAll(pathMask, rotatedRequiredPath)) continue;
            if (ContainsAny(pathMask, rotatedForbiddenPath)) continue;

            yRotation = baseYRotation + quarterTurns * 90f;
            return true;
        }

        yRotation = 0f;
        return false;
    }

    static bool ContainsAll(TDNeighborMask source, TDNeighborMask required)
        => (source & required) == required;

    static bool ContainsAny(TDNeighborMask source, TDNeighborMask forbidden)
        => (source & forbidden) != 0;
}

[CreateAssetMenu(
    fileName = "TDMapTilePalette",
    menuName = "Tower Defense/Map Tile Palette")]
public sealed class TDMapTilePaletteSO : ScriptableObject
{
    [Header("기본 Ground 타일")]
    [SerializeField] TDGroundTileSet groundTiles = new();

    [Header("기본 Path 타일")]
    [SerializeField] TDPathTileSet pathTiles = new();

    [Header("시작 및 도착")]
    [SerializeField] GameObject spawnPrefab;
    [SerializeField] GameObject basePrefab;

    [Header("특수 조합 타일")]
    [Tooltip(
        "Road_End_Edge처럼 Ground 외곽 형태와 Path 형태가 합쳐진 타일을 등록합니다. " +
        "일반 Ground/Path보다 먼저 검사됩니다.")]
    [SerializeField] List<TDMapTileRule> specialRules = new();

    [Header("장식 팔레트")]
    [SerializeField] List<TDDecorationPaletteEntry> decorations = new();

    [Header("레이어")]
    [Tooltip("타워를 설치할 수 있는 Ground 오브젝트에 적용할 레이어입니다.")]
    [SerializeField] LayerMask floorLayer;

    [Tooltip("Path, Spawn, Base 및 설치를 막는 장식물에 적용할 레이어입니다.")]
    [SerializeField] LayerMask obstacleLayer;

    [Tooltip("타워 설치를 막지 않는 장식물의 기본 레이어입니다.")]
    [SerializeField] LayerMask decorationLayer;

    [Header("GridGenerator")]
    [SerializeField] Material gridMaterial;

    [Header("배치 보정")]
    [Tooltip(
        "원본 프리팹 크기가 셀 크기와 다를 때 사용하는 배율입니다. " +
        "정규화된 1x1 프리팹이라면 1을 사용합니다.")]
    [Min(0.01f)]
    [SerializeField] float visualScale = 1f;

    [Tooltip("생성되는 Waypoint의 타일 중심 기준 Y 오프셋입니다.")]
    [SerializeField] float waypointYOffset;

    [Tooltip("카메라 이동 Bounds가 맵 Bounds보다 추가로 넓어지는 거리입니다.")]
    [Min(0f)]
    [SerializeField] float cameraBoundsPadding = 2f;

    public TDGroundTileSet GroundTiles => groundTiles;
    public TDPathTileSet PathTiles => pathTiles;

    public GameObject SpawnPrefab => spawnPrefab;
    public GameObject BasePrefab => basePrefab;

    public IReadOnlyList<TDMapTileRule> SpecialRules => specialRules;
    public IReadOnlyList<TDDecorationPaletteEntry> Decorations => decorations;

    public LayerMask FloorLayer => floorLayer;
    public LayerMask ObstacleLayer => obstacleLayer;
    public LayerMask DecorationLayer => decorationLayer;

    public Material GridMaterial => gridMaterial;
    public float VisualScale => visualScale;
    public float WaypointYOffset => waypointYOffset;
    public float CameraBoundsPadding => cameraBoundsPadding;

    public int FloorObjectLayer => GetFirstLayer(floorLayer);
    public int ObstacleObjectLayer => GetFirstLayer(obstacleLayer);
    public int DecorationObjectLayer => GetFirstLayer(decorationLayer);

    public bool HasRequiredTerrainTiles
        => groundTiles != null && groundTiles.IsValid;

    public bool HasRequiredPathTiles
        => pathTiles != null && pathTiles.IsValid;

    public bool HasEndpoints
        => spawnPrefab && basePrefab;

    public int GetDecorationLayer(TDDecorationPaletteEntry entry)
    {
        if (entry == null) return DecorationObjectLayer;

        if (entry.CustomLayer.value != 0)
            return GetFirstLayer(entry.CustomLayer);

        return entry.BlocksTower
            ? ObstacleObjectLayer
            : DecorationObjectLayer;
    }

    public bool TryGetSpecialRule(
        TDMapCellType cellType,
        TDNeighborMask terrainMask,
        TDNeighborMask pathMask,
        out GameObject prefab,
        out float yRotation)
    {
        prefab = null;
        yRotation = 0f;

        TDMapTileRule bestRule = null;
        float bestRotation = 0f;

        for (int i = 0; i < specialRules.Count; i++)
        {
            TDMapTileRule rule = specialRules[i];

            if (rule == null || !rule.IsValid) continue;
            if (rule.TargetType != cellType) continue;

            if (!rule.TryMatch(terrainMask, pathMask, out float rotation))
                continue;

            if (bestRule != null && bestRule.Priority >= rule.Priority)
                continue;

            bestRule = rule;
            bestRotation = rotation;
        }

        if (bestRule == null) return false;

        prefab = bestRule.Prefab;
        yRotation = bestRotation;

        return true;
    }

    static int GetFirstLayer(LayerMask layerMask)
    {
        int mask = layerMask.value;

        if (mask == 0)
            return 0;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & 1 << layer) != 0)
                return layer;
        }

        return 0;
    }

    void OnValidate()
    {
        visualScale = Mathf.Max(0.01f, visualScale);
        cameraBoundsPadding = Mathf.Max(0f, cameraBoundsPadding);

        decorations ??= new List<TDDecorationPaletteEntry>();
        specialRules ??= new List<TDMapTileRule>();
    }
}

public static class TDMapDirectionUtility
{
    public static readonly Vector2Int[] GridDirections =
    {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0)
    };

    public static readonly TDNeighborMask[] DirectionMasks =
    {
        TDNeighborMask.North,
        TDNeighborMask.East,
        TDNeighborMask.South,
        TDNeighborMask.West
    };

    public static TDNeighborMask RotateClockwise(
        TDNeighborMask mask,
        int quarterTurns)
    {
        quarterTurns = ((quarterTurns % 4) + 4) % 4;

        for (int i = 0; i < quarterTurns; i++)
            mask = RotateClockwiseOnce(mask);

        return mask;
    }

    public static int Count(TDNeighborMask mask)
    {
        int value = (int)mask;
        int count = 0;

        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }

    public static bool IsOppositePair(TDNeighborMask mask)
        => mask == (TDNeighborMask.North | TDNeighborMask.South) ||
           mask == (TDNeighborMask.East | TDNeighborMask.West);

    public static bool IsAdjacentPair(TDNeighborMask mask)
    {
        if (Count(mask) != 2) return false;
        return !IsOppositePair(mask);
    }

    public static bool TryFindRotation(
        TDNeighborMask baseMask,
        TDNeighborMask targetMask,
        out int quarterTurns)
    {
        for (int i = 0; i < 4; i++)
        {
            if (RotateClockwise(baseMask, i) != targetMask) continue;

            quarterTurns = i;
            return true;
        }

        quarterTurns = 0;
        return false;
    }

    public static TDNeighborMask GetMask(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return TDNeighborMask.North;
        if (direction == Vector2Int.right) return TDNeighborMask.East;
        if (direction == Vector2Int.down) return TDNeighborMask.South;
        if (direction == Vector2Int.left) return TDNeighborMask.West;

        return TDNeighborMask.None;
    }

    static TDNeighborMask RotateClockwiseOnce(TDNeighborMask mask)
    {
        TDNeighborMask result = TDNeighborMask.None;

        if ((mask & TDNeighborMask.North) != 0)
            result |= TDNeighborMask.East;

        if ((mask & TDNeighborMask.East) != 0)
            result |= TDNeighborMask.South;

        if ((mask & TDNeighborMask.South) != 0)
            result |= TDNeighborMask.West;

        if ((mask & TDNeighborMask.West) != 0)
            result |= TDNeighborMask.North;

        return result;
    }
}