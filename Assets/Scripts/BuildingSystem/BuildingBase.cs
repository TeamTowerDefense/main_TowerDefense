using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BuildingBase : MonoBehaviour, IBuildable, IHasPreview
{
    // 직렬화된 변수들은 나중에 SO로 이동
    [SerializeField]
    private List<Vector2Int> baseFootprint = new List<Vector2Int>() { new Vector2Int(0, 0) };
    [SerializeField]
    private GameObject previewPF;
    [SerializeField]
    private Material validStateMaterial;
    [SerializeField]
    private Material inValidStateMaterial;

    [SerializeField]
    private bool isDestructible;

    public bool IsDestructible => isDestructible;

    public IGridProvider ConstructedGrid {  get; set; }
    public Vector2Int ConstructedIndex { get; set; }

    // 건물 오브젝트 내부의 모든 렌더러와 해당 렌더러의 원래 머티리얼을 저장해 놓는 딕셔너리
    private Dictionary<Renderer, Material[]> originalMaterials = 
        new Dictionary<Renderer, Material[]>();

    public List<Vector2Int> GetOccupiedOffsets()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        foreach (Vector2Int localOffset in baseFootprint)
        {
            cells.Add(localOffset);
        }

        return cells;
    }

    public Vector2 GetCenter(float cellSize)
    {
        List<Vector2Int> offsets = GetOccupiedOffsets();
        if (offsets.Count == 0)
        {
            return Vector3.zero;
        }

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Vector2Int offset in offsets)
        {
            if(offset.x < minX) minX = offset.x;
            if(offset.x > maxX) maxX = offset.x;

            if(offset.y < minY) minY = offset.y;
            if(offset.y > maxY) maxY = offset.y; 
        }

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        //Debug.Log(centerX * cellSize + ", " + centerY * cellSize);

        return new Vector2(centerX * cellSize, centerY * cellSize);
    }

    public GameObject GetPreview()
    {
        return previewPF;
    }

    public virtual void OnPlaced() { }

    public void SetVisualState(BuildingVisualState newState)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        Material targetMaterial = null;
        // newState에 따라 targetMaterial 설정
        switch (newState)
        {
            case BuildingVisualState.Normal:
                foreach (Renderer r in renderers)
                {
                    if (originalMaterials.ContainsKey(r))
                    {
                        r.materials = originalMaterials[r];
                    }
                }
                return;
            case BuildingVisualState.Valid:
                targetMaterial = validStateMaterial;
                break;
            case BuildingVisualState.InValid:
                targetMaterial = inValidStateMaterial;
                break;
        }

        foreach (Renderer r in renderers)
        {
            // 딕셔너리에 렌더러, 머티리얼 추가
            if (!originalMaterials.ContainsKey(r))
            {
                originalMaterials.Add(r, r.materials);
            }

            // 렌더러의 머티리얼을 targetMaterial로 설정
            Material[] targetMaterialArr = new Material[r.materials.Length];
            for (int i = 0; i < targetMaterialArr.Length; i++)
            {
                targetMaterialArr[i] = targetMaterial;
            }
            r.materials = targetMaterialArr;
        }
    }

}
