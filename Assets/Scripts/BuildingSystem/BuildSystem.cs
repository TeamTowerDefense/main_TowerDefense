using System;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    public int TowerLimit { get; private set; } = 0;
    public int CurrentTowerCount { get; private set; } = 0;
    public event Action<int, int> OnTowerCountChanged;

    public void SetTowerLimit(int limit)
    {
        TowerLimit = limit;
        OnTowerCountChanged?.Invoke(CurrentTowerCount, TowerLimit);
    }

    public bool CanBuildTower()
    {
        return CurrentTowerCount < TowerLimit;
    }

    // 건물 설치
    public GameObject PlaceBuilding(GameObject prefabToPlace, IGridProvider grid, Vector2Int index, Vector3 pos, Quaternion rotation)
    {
        CurrentTowerCount++;
        OnTowerCountChanged?.Invoke(CurrentTowerCount, 0);

        GameObject newObj = Instantiate(prefabToPlace, pos, rotation);

        IBuildable buildable = newObj.GetComponent<IBuildable>();
        buildable.ConstructedGrid = grid;
        buildable.ConstructedIndex = index;

        return newObj;
    }

    // 건물 파괴
    public void DestroyBuilding(GameObject buildingObj)
    {

        if (CurrentTowerCount > 0)
        {
            CurrentTowerCount--;
            OnTowerCountChanged?.Invoke(CurrentTowerCount, TowerLimit);
        }
        Destroy(buildingObj);
    }

}
