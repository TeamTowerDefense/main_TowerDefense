using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TDMapBuildGridProvider : MonoBehaviour, IGridProvider
{

    [SerializeField] TDMapRoot mapRoot;
    [SerializeField, Min(0.01f)] float obstacleCheckHeight = 0.98f;

    readonly HashSet<Vector2Int> occupiedCells = new();

    public int CellSize => mapRoot ? Mathf.Max(1, Mathf.RoundToInt(mapRoot.CellSize)) : 1;

    void Awake() => ResolveMapRoot();
    void OnValidate() => ResolveMapRoot();

    public void Configure(TDMapRoot target)
    {
        if (mapRoot == target) return;

        mapRoot = target;
        occupiedCells.Clear();
    }

    public bool CheckGridInPoint(Vector3 point)
        => mapRoot && IsBuildableCell(GetCellIndex(point));

    public Vector2Int GetCellIndex(Vector3 point)
        => mapRoot ? mapRoot.WorldToGrid(point) : default;

    public Vector3 GetCellCenterFromIndex(Vector2Int index)
        => mapRoot ? mapRoot.GetCellSurfaceWorld(index) : Vector3.zero;

    public Vector3 GetCellCenterFromPoint(Vector3 point)
        => GetCellCenterFromIndex(GetCellIndex(point));

    public bool CheckCellValid(Vector2Int index, LayerMask obstacleLayer)
    {
        if (!mapRoot || !IsBuildableCell(index) || occupiedCells.Contains(index))
            return false;

        Vector3 halfExtents = new(
            mapRoot.CellSize * 0.49f,
            obstacleCheckHeight * 0.5f,
            mapRoot.CellSize * 0.49f);

        int obstacleMask = obstacleLayer.value;

        if (mapRoot.Palette)
            obstacleMask |= mapRoot.Palette.ObstacleLayer.value;

        return !Physics.CheckBox(
            GetCellCenterFromIndex(index),
            halfExtents,
            mapRoot.transform.rotation,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    public void RegisterOccupancy(
        Vector2Int index,
        List<Vector2Int> offsetList,
        bool isOccupied)
    {
        if (offsetList == null) return;

        for (int i = 0; i < offsetList.Count; i++)
        {
            Vector2Int target = index + offsetList[i];

            if (isOccupied) occupiedCells.Add(target);
            else occupiedCells.Remove(target);
        }
    }

    bool IsBuildableCell(Vector2Int index)
    {
        if (!mapRoot.HasGroundAt(index)) return false;

        TDMapCellMarker spawn = mapRoot.FindEndpoint(TDMapCellType.Spawn);
        if (spawn && spawn.GridPosition == index) return false;

        TDMapCellMarker targetBase = mapRoot.FindEndpoint(TDMapCellType.Base);
        return !targetBase || targetBase.GridPosition != index;
    }

    void ResolveMapRoot()
    {
        if (!mapRoot) mapRoot = GetComponent<TDMapRoot>();
        if (!mapRoot) mapRoot = GetComponentInParent<TDMapRoot>();
    }
}

