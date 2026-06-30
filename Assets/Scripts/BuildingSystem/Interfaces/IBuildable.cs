using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum BuildingVisualState
{
    Normal = 0,
    Valid,
    InValid,
}

public interface IBuildable
{
    // 해체 가능 여부
    bool IsDestructible { get; }           

    // 건물이 차지하는 칸의 셀 좌표 리스트를 반환하는 함수
    List<Vector2Int> GetOccupiedOffsets();
    // 건물의 중앙 좌표를 반환하는 함수
    Vector2 GetCenter(float cellSize);
    // 건물이 설치된 그리드
    IGridProvider ConstructedGrid { get; set; }
    // 건물이 설치된 인덱스
    Vector2Int ConstructedIndex { get; set; }
    // 건물이 지어졌을 때, 실행하는 함수
    void OnPlaced();
}
