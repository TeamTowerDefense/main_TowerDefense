using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("기본 정보")]
    public int buildingID = 0;
    public string buildingName = "";
    public GameObject buildingPrefab; 

    [Header("경제 데이터")]
    public int cost = 10;
    public bool isDestructible = true;

    [Header("그리드 및 건설 연출")]
    public List<Vector2Int> baseFootprint = new List<Vector2Int>() { Vector2Int.zero };
    public GameObject previewPF;
    public Material validStateMaterial;
    public Material inValidStateMaterial;
    public PoolEffect placeVFX;
}
