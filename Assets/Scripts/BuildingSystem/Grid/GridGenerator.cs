using System;
using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour, IGridProvider
{
    [SerializeField]
    private GridDrawer gridDrawer;
    // 그리드 생성 영역
    [SerializeField]
    private BoxCollider gridBound;
    // 셀 한개 사이즈 
    [SerializeField]
    private int cellSize;
    public int CellSize { get { return cellSize; }}

    [SerializeField]
    private LayerMask floorLayer;
    [SerializeField]
    private LayerMask obstacleLayer;

    private GridData gridData;

    private Vector3 StartPos => gridBound.bounds.min;

    private float CellSizeF => (float)cellSize;

    private void Awake()
    {
        InitInternal();
    }

    private void Start()
    {
        InitExternal();
    }

    public void InitInternal()
    {
        gridData = new GridData();
    }

    public void InitExternal()
    {
        GenerateGrid();
    }


    // 해당 지점에 그리드가 존재 하는지 검사
    public bool CheckGridInPoint(Vector3 point)
    {
        return gridData.GetCell(GetCellIndex(point)) != null;
    }

    public Vector2Int GetCellIndex(Vector3 point)
    {
        int x = Mathf.FloorToInt((point.x - StartPos.x) / CellSizeF);
        int y = Mathf.FloorToInt((point.z - StartPos.z) / CellSizeF);

        return new Vector2Int(x, y);
    }

    public Vector3 GetCellCenterFromIndex(Vector2Int index)
    {
        float centerX = (index.x * cellSize) + (cellSize / 2f) + StartPos.x;
        float centerZ = (index.y * cellSize) + (cellSize / 2f) + StartPos.z;

        return new Vector3(centerX, 0f, centerZ);
    }
    public Vector3 GetCellCenterFromPoint(Vector3 point)
    {
        // 월드 좌표 -> 셀 인덱스
        Vector2Int index = GetCellIndex(point);

        Debug.Log(index);

        // 셀 인덱스 -> 셀 중앙 좌표
        return GetCellCenterFromIndex(index);
    }

    public bool CheckCellValid(Vector2Int index, LayerMask obstacleLayer)
    {
        // 그리드 데이터에 인덱스에 해당하는 셀이 있는지, 셀이 비어 있는지 검사
        if (!gridData.IsValidCell(index))
        {
            Debug.Log("Cell Not Valid");
            return false;
        }

        if(gridData.IsCellOccupied(index))
        {
            return false;
        }


        // 셀 내에 다른 오브젝트가 있는지 검사
        if(CheckObjInCell(GetCellCenterFromIndex(index), cellSize, obstacleLayer))
        {
            Debug.Log("Obj in cell");
            return false;
        }

        return true;
    }

    public void RegisterOccupancy(Vector2Int index, List<Vector2Int> offsetList, bool isOccupied)
    {
        foreach (Vector2Int offset in offsetList)
        {
            Vector2Int targetIndex = index + offset;

            gridData.SetCellState(targetIndex, isOccupied);
        }
    }
    public void GenerateGrid()
    {
        // 1. 콜라이더의 영역(Bounds) 가져오기
        Bounds bounds = gridBound.bounds;

        int gridWidth = Mathf.CeilToInt(bounds.size.x / cellSize);
        int gridHeight = Mathf.CeilToInt(bounds.size.z / cellSize);

        for (int z = 0; z < gridHeight; z ++)
        {
            for(int x = 0; x < gridWidth; x ++)
            {
                // 셀 인덱스 Vector2 생성
                Vector2Int cellIndex = new Vector2Int(x, z);

                // 셀 인덱스 -> 셀 중앙 좌표 변환 
                Vector3 cellCenterPos = GetCellCenterFromIndex(cellIndex);
                Debug.DrawLine(new Vector3(cellCenterPos.x, -10, cellCenterPos.z), new Vector3(cellCenterPos.x, 10, cellCenterPos.z), Color.yellow, 10f);
                Debug.Log("CellCenter: " + cellCenterPos);

                // 조건 1: 셀 좌표가 생성 범위 내에 있는가?
                if (!bounds.Contains(cellCenterPos))
                    continue;

                // 조건 2: 셀 위치에 벽이나 장애물이 겹치지 않는가?
                if (CheckObjInCell(cellCenterPos, cellSize, obstacleLayer))
                    continue;

                // 조건 3: 셀 위치에 바닥이 있는가?
                if (!CheckObjInCell(cellCenterPos, cellSize, floorLayer))
                    continue;

                // 조건 4: 딕셔너리가 셀을 들고 있지 않는가?
                gridData.AddCell(cellIndex);
                Debug.Log(cellIndex);
            }
        }

        //Debug.Log(gridData.GetGrid().Count);
        gridDrawer.DrawMesh(gridData.GetGrid(), cellSize, StartPos);
    }

    public bool CheckObjInCell(Vector3 pos, float cellSize, LayerMask objLayer)
    {
        // 검사 육면체의 절반 크기
        Vector3 halfExtents = new Vector3(cellSize * 0.49f, 0.49f, cellSize * 0.49f);

        if (Physics.CheckBox(pos, halfExtents, Quaternion.identity, objLayer))
        {
            return true;
        }

        return false;
    }

    //void OnDrawGizmos()
    //{
    //    if (gridData == null || gridData.GetGrid().Count == 0) return;

    //    Gizmos.color = Color.green;
    //    foreach (KeyValuePair<Vector2Int, CellData> kvp in gridData.GetGrid())
    //    {
    //        // 등록된 좌표 위치에 작은 초록색 구체를 그립니다.
    //        Vector3 pos = new Vector3(kvp.Key.x * cellSize + cellSize / 2, transform.position.y + 0.2f, kvp.Key.y * cellSize + cellSize / 2);
    //        Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0, cellSize));
    //    }
    //}
}
