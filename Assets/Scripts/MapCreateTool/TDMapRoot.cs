using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TDMapRoot : MonoBehaviour
{
    [Header("팔레트")]
    [SerializeField] TDMapTilePaletteSO palette;

    [Header("그리드")]
    [SerializeField, Min(0.1f)] float cellSize = 1f;
    [SerializeField] float tileY;

    [SerializeField] float surfaceYOffset = 0.5f;

    [Header("맵 계층")]
    [SerializeField] Transform groundRoot;
    [SerializeField] Transform pathRoot;
    [SerializeField] Transform spawnBaseRoot;
    [SerializeField] Transform decorationRoot;
    [SerializeField] Transform waypointRoot;

    [Header("경로")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform basePoint;
    [SerializeField] List<Transform> waypoints = new();

    [Header("맵 영역")]
    [SerializeField] BoxCollider gridBounds;
    [SerializeField] Collider mapBounds;
    [SerializeField] Collider cameraBounds;

    [Header("런타임 컴포넌트")]
    [SerializeField] GridGenerator gridGenerator;
    [SerializeField] GridDrawer gridDrawer;
    [SerializeField] MapBoundsInfoProvider mapBoundsInfoProvider;

    readonly Dictionary<Vector2Int, TDMapCellMarker> terrainCells = new();
    readonly List<TDMapCellMarker> cellMarkers = new();
    readonly List<TDMapCellMarker> decorationMarkers = new();

    TDMapCellMarker spawnMarker;
    TDMapCellMarker baseMarker;
    bool cacheDirty = true;

    public TDMapTilePaletteSO Palette => palette;
    public float CellSize => cellSize;
    public float TileY => tileY;
    public float SurfaceYOffset => surfaceYOffset;

    public Transform GroundRoot => groundRoot;
    public Transform PathRoot => pathRoot;
    public Transform SpawnBaseRoot => spawnBaseRoot;
    public Transform DecorationRoot => decorationRoot;
    public Transform WaypointRoot => waypointRoot;

    public Transform SpawnPoint => spawnPoint;
    public Transform BasePoint => basePoint;
    public IReadOnlyList<Transform> Waypoints => waypoints;

    public BoxCollider GridBounds => gridBounds;
    public Collider MapBounds => mapBounds;
    public Collider CameraBounds => cameraBounds;
    public GridGenerator GridGenerator => gridGenerator;
    public GridDrawer GridDrawer => gridDrawer;
    public MapBoundsInfoProvider MapBoundsInfoProvider => mapBoundsInfoProvider;

    public bool HasPalette => palette;
    public bool HasEndpoints => spawnPoint && basePoint;
    public bool HasWaypoints => waypoints.Count > 1;
    public bool HasHierarchy => groundRoot && pathRoot && spawnBaseRoot && decorationRoot && waypointRoot;

    public IReadOnlyList<TDMapCellMarker> CellMarkers
    {
        get { EnsureCellCache(); return cellMarkers; }
    }

    public IReadOnlyList<TDMapCellMarker> DecorationMarkers
    {
        get { EnsureCellCache(); return decorationMarkers; }
    }

    #region 그리드 좌표

    public Vector3 GridToLocal(Vector2Int gridPosition)
        => new(gridPosition.x * cellSize, tileY, gridPosition.y * cellSize);

    public Vector3 GridToWorld(Vector2Int gridPosition)
        => transform.TransformPoint(GridToLocal(gridPosition));

    public Vector3 GetCellSurfaceWorld(Vector2Int gridPosition)
        => GridToWorld(gridPosition) + transform.up * surfaceYOffset;

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        return new Vector2Int(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.z / cellSize));
    }

    public Vector3 SnapToGrid(Vector3 worldPosition) => GridToWorld(WorldToGrid(worldPosition));

    public Bounds GetCellWorldBounds(Vector2Int gridPosition, float height = 1f)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 size = new(cellSize * Mathf.Abs(scale.x), Mathf.Max(0.01f, height), cellSize * Mathf.Abs(scale.z));
        return new Bounds(GridToWorld(gridPosition), size);
    }

    #endregion

    #region 셀 검색

    public void MarkCellCacheDirty() => cacheDirty = true;

    [ContextMenu("셀 캐시 다시 만들기")]
    public void RebuildCellCache()
    {
        terrainCells.Clear();
        cellMarkers.Clear();
        decorationMarkers.Clear();
        spawnMarker = null;
        baseMarker = null;

        TDMapCellMarker[] markers = GetComponentsInChildren<TDMapCellMarker>(true);

        for (int i = 0; i < markers.Length; i++)
        {
            TDMapCellMarker marker = markers[i];
            if (!marker) continue;

            cellMarkers.Add(marker);

            if (marker.IsDecoration)
            {
                decorationMarkers.Add(marker);
                continue;
            }

            if (marker.IsSpawn)
            {
                if (!spawnMarker) spawnMarker = marker;
                else Debug.LogWarning("[TDMapRoot] Spawn이 두 개 이상입니다.", marker);
                continue;
            }

            if (marker.IsBase)
            {
                if (!baseMarker) baseMarker = marker;
                else Debug.LogWarning("[TDMapRoot] Base가 두 개 이상입니다.", marker);
                continue;
            }

            if (!marker.IsGround && !marker.IsPath) continue;

            if (terrainCells.TryGetValue(marker.GridPosition, out TDMapCellMarker existing))
            {
                Debug.LogWarning($"[TDMapRoot] 동일 셀에 지형이 중복되었습니다. {marker.GridPosition} / {existing.name} / {marker.name}", marker);
                continue;
            }

            terrainCells.Add(marker.GridPosition, marker);
        }

        spawnPoint = spawnMarker ? spawnMarker.transform : null;
        basePoint = baseMarker ? baseMarker.transform : null;
        cacheDirty = false;
    }

    public TDMapCellMarker FindTerrainCell(Vector2Int gridPosition)
    {
        EnsureCellCache();
        return terrainCells.TryGetValue(gridPosition, out TDMapCellMarker marker) ? marker : null;
    }

    public TDMapCellMarker FindCell(TDMapCellType cellType, Vector2Int gridPosition)
    {
        EnsureCellCache();

        if (cellType == TDMapCellType.Spawn) return spawnMarker && spawnMarker.GridPosition == gridPosition ? spawnMarker : null;
        if (cellType == TDMapCellType.Base) return baseMarker && baseMarker.GridPosition == gridPosition ? baseMarker : null;

        if (cellType == TDMapCellType.Decoration)
        {
            for (int i = 0; i < decorationMarkers.Count; i++)
                if (decorationMarkers[i].GridPosition == gridPosition) return decorationMarkers[i];
            return null;
        }

        TDMapCellMarker terrain = FindTerrainCell(gridPosition);
        return terrain && terrain.CellType == cellType ? terrain : null;
    }

    public TDMapCellMarker FindEndpoint(TDMapCellType cellType)
    {
        EnsureCellCache();
        return cellType == TDMapCellType.Spawn ? spawnMarker : cellType == TDMapCellType.Base ? baseMarker : null;
    }

    public bool HasTerrainAt(Vector2Int gridPosition) => FindTerrainCell(gridPosition);
    public bool HasGroundAt(Vector2Int gridPosition) => FindTerrainCell(gridPosition)?.IsGround == true;
    public bool HasPathAt(Vector2Int gridPosition) => FindTerrainCell(gridPosition)?.IsPath == true;

    public TDNeighborMask GetGroundNeighborMask(Vector2Int gridPosition) => GetNeighborMask(gridPosition, HasGroundAt);
    public TDNeighborMask GetPathNeighborMask(Vector2Int gridPosition) => GetNeighborMask(gridPosition, HasPathAt);

    TDNeighborMask GetNeighborMask(Vector2Int gridPosition, System.Func<Vector2Int, bool> condition)
    {
        EnsureCellCache();
        TDNeighborMask result = TDNeighborMask.None;

        for (int i = 0; i < TDMapDirectionUtility.GridDirections.Length; i++)
        {
            Vector2Int position = gridPosition + TDMapDirectionUtility.GridDirections[i];
            if (condition(position)) result |= TDMapDirectionUtility.DirectionMasks[i];
        }

        return result;
    }

    public void CollectMarkers(TDMapCellType cellType, List<TDMapCellMarker> results)
    {
        if (results == null) return;
        EnsureCellCache();
        results.Clear();

        for (int i = 0; i < cellMarkers.Count; i++)
            if (cellMarkers[i] && cellMarkers[i].CellType == cellType) results.Add(cellMarkers[i]);
    }

    void EnsureCellCache()
    {
        if (cacheDirty) RebuildCellCache();
    }

    #endregion

    #region 맵 영역 계산

    public bool TryCalculateTerrainBounds(out Bounds bounds)
    {
        EnsureCellCache();
        bool found = false;
        bounds = default;

        foreach (KeyValuePair<Vector2Int, TDMapCellMarker> pair in terrainCells)
        {
            Bounds cellBounds = GetCellWorldBounds(pair.Key);
            if (!found) { bounds = cellBounds; found = true; }
            else bounds.Encapsulate(cellBounds);
        }

        return found;
    }

    public bool TryCalculateVisualBounds(out Bounds bounds)
    {
        EnsureCellCache();
        bool found = false;
        bounds = default;

        for (int i = 0; i < cellMarkers.Count; i++)
        {
            TDMapCellMarker marker = cellMarkers[i];
            if (!marker || !marker.TryGetWorldBounds(out Bounds markerBounds)) continue;
            if (!found) { bounds = markerBounds; found = true; }
            else bounds.Encapsulate(markerBounds);
        }

        return found;
    }

    #endregion

    #region 데이터 설정

    public void SetPalette(TDMapTilePaletteSO value) => palette = value;

    public void ConfigureGrid(float newCellSize, float newTileY)
    {
        cellSize = Mathf.Max(0.1f, newCellSize);
        tileY = newTileY;
    }

    public void SetHierarchy(Transform newGroundRoot, Transform newPathRoot, Transform newSpawnBaseRoot,
        Transform newDecorationRoot, Transform newWaypointRoot)
    {
        groundRoot = newGroundRoot;
        pathRoot = newPathRoot;
        spawnBaseRoot = newSpawnBaseRoot;
        decorationRoot = newDecorationRoot;
        waypointRoot = newWaypointRoot;
        MarkCellCacheDirty();
    }

    public void SetEndpoints(Transform newSpawnPoint, Transform newBasePoint)
    {
        spawnPoint = newSpawnPoint;
        basePoint = newBasePoint;
    }

    public void RefreshEndpointReferences()
    {
        MarkCellCacheDirty();
        RebuildCellCache();
    }

    public void SetWaypoints(IReadOnlyList<Transform> newWaypoints)
    {
        waypoints.Clear();
        if (newWaypoints == null) return;

        for (int i = 0; i < newWaypoints.Count; i++)
            if (newWaypoints[i]) waypoints.Add(newWaypoints[i]);
    }

    public void ClearWaypoints() => waypoints.Clear();

    public void AlignWaypointsToSurface()
    {
        float additionalOffset = palette ? palette.WaypointYOffset : 0f;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform waypoint = waypoints[i];
            if (!waypoint) continue;

            Vector2Int cell = WorldToGrid(waypoint.position);
            waypoint.position =
                GetCellSurfaceWorld(cell) +
                transform.up * additionalOffset;
        }
    }

    public void SetBounds(BoxCollider newGridBounds, Collider newMapBounds, Collider newCameraBounds)
    {
        gridBounds = newGridBounds;
        mapBounds = newMapBounds;
        cameraBounds = newCameraBounds;
    }

    public void SetRuntimeComponents(GridGenerator newGridGenerator, GridDrawer newGridDrawer,
        MapBoundsInfoProvider newMapBoundsInfoProvider)
    {
        gridGenerator = newGridGenerator;
        gridDrawer = newGridDrawer;
        mapBoundsInfoProvider = newMapBoundsInfoProvider;
    }

    #endregion

    #region 계층 조회

    public Transform GetParent(TDMapCellType cellType) => cellType switch
    {
        TDMapCellType.Ground => groundRoot,
        TDMapCellType.Path => pathRoot,
        TDMapCellType.Spawn => spawnBaseRoot,
        TDMapCellType.Base => spawnBaseRoot,
        TDMapCellType.Decoration => decorationRoot,
        _ => transform
    };

    [ContextMenu("기본 계층 참조 찾기")]
    public void FindHierarchyReferences()
    {
        groundRoot = transform.Find("Ground");
        pathRoot = transform.Find("Path");
        spawnBaseRoot = transform.Find("SpawnBase");
        decorationRoot = transform.Find("Decoration");
        waypointRoot = transform.Find("PathWaypoints");
        MarkCellCacheDirty();
    }

    #endregion

    void OnTransformChildrenChanged() => MarkCellCacheDirty();

    void OnValidate()
    {
        cellSize = Mathf.Max(0.1f, cellSize);
        surfaceYOffset = Mathf.Max(0f, surfaceYOffset);

        waypoints ??= new List<Transform>();
        for (int i = waypoints.Count - 1; i >= 0; i--)
            if (!waypoints[i]) waypoints.RemoveAt(i);
        MarkCellCacheDirty();
    }
}
