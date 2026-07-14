using UnityEngine;

public enum TDMapCellType
{
    Ground,
    Path,
    Spawn,
    Base,
    Decoration
}

[DisallowMultipleComponent]
public sealed class TDMapCellMarker : MonoBehaviour
{
    [Header("셀 정보")]
    [SerializeField] TDMapCellType cellType;
    [SerializeField] Vector2Int gridPosition;

    [Header("외형")]
    [SerializeField] GameObject sourcePrefab;
    [SerializeField] Transform visualRoot;

    [Header("배치 판정")]
    [Tooltip("장식물이 타워 설치를 막는지 나타냅니다.")]
    [SerializeField] bool blocksTower;

    public TDMapCellType CellType => cellType;
    public Vector2Int GridPosition => gridPosition;

    public GameObject SourcePrefab => sourcePrefab;
    public Transform VisualRoot => visualRoot;

    public bool BlocksTower => blocksTower;

    public bool IsGround => cellType == TDMapCellType.Ground;
    public bool IsPath => cellType == TDMapCellType.Path;
    public bool IsSpawn => cellType == TDMapCellType.Spawn;
    public bool IsBase => cellType == TDMapCellType.Base;
    public bool IsDecoration => cellType == TDMapCellType.Decoration;

    public bool IsTerrain
        => cellType is TDMapCellType.Ground
            or TDMapCellType.Path
            or TDMapCellType.Spawn
            or TDMapCellType.Base;

    /// <summary>
    /// Ground, Path, Spawn, Base는 실제 셀 좌표입니다.
    /// Decoration은 자유 배치 위치에서 계산된 기준 셀 좌표입니다.
    /// </summary>
    public void Setup(
        TDMapCellType type,
        Vector2Int position,
        GameObject prefab,
        Transform visual,
        bool towerBlocking = false)
    {
        cellType = type;
        gridPosition = position;
        sourcePrefab = prefab;
        visualRoot = visual;
        blocksTower = towerBlocking;
    }

    public void SetCellType(TDMapCellType type) => cellType = type;
    public void SetGridPosition(Vector2Int position) => gridPosition = position;
    public void SetBlocksTower(bool value) => blocksTower = value;

    public void SetVisual(GameObject prefab, Transform visual)
    {
        sourcePrefab = prefab;
        visualRoot = visual;
    }

    public void ClearVisual()
    {
        sourcePrefab = null;
        visualRoot = null;
    }

    #region Bounds

    /// <summary>
    /// Collider를 우선 사용하고, Collider가 없으면 Renderer Bounds를 사용합니다.
    /// 장식과 삭제 셀의 겹침 판정에 사용합니다.
    /// </summary>
    public bool TryGetWorldBounds(out Bounds bounds)
    {
        Transform target = visualRoot ? visualRoot : transform;

        if (TryGetColliderBounds(target, out bounds))
            return true;

        return TryGetRendererBounds(target, out bounds);
    }

    static bool TryGetColliderBounds(Transform target, out Bounds bounds)
    {
        Collider[] colliders =
            target.GetComponentsInChildren<Collider>(true);

        bool found = false;
        bounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (!collider || !collider.enabled) continue;

            if (!found)
            {
                bounds = collider.bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(collider.bounds);
        }

        return found;
    }

    static bool TryGetRendererBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(true);

        bool found = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (!renderer || !renderer.enabled) continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return found;
    }

    #endregion

    void OnValidate()
    {
        if (visualRoot && !visualRoot.IsChildOf(transform))
            visualRoot = null;
    }
}