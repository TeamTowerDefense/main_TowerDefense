using IGameInterface;
using UnityEngine;

public class MapBoundsInfoProvider : MonoBehaviour, IMapInfoProvider
{
    [Header("맵 경계")]
    [SerializeField] Collider mapBoundsCollider;
    [SerializeField] Collider cameraBoundsCollider;

    bool registered;
    MapInfo map;

    public MapInfo Info => map;

    #region 생명 주기
    void Reset()
    {
        mapBoundsCollider = GetComponent<Collider>();
        cameraBoundsCollider = mapBoundsCollider;
    }

    void OnEnable()
    {
        TryRegister();
    }

    void Start()
    {
        TryRegister();
    }

    void Update()
    {
        TryRegister();
    }

    void OnDisable()
    {
        if (registered && ServiceLocator.TryGet(out IMapService mapService))
            mapService.Unregister(this);

        registered = false;
    }
    #endregion

    public bool TryGetInfo(out MapInfo info)
    {
        if (mapBoundsCollider == null)
        {
            info = default;
            return false;
        }

        Bounds mapBounds = mapBoundsCollider.bounds;
        Bounds cameraBounds = cameraBoundsCollider != null ? cameraBoundsCollider.bounds : mapBounds;
        info = new MapInfo(mapBounds, cameraBounds);
        map = info;
        return true;
    }

    void TryRegister()
    {
        if (registered) return;
        if (!ServiceLocator.TryGet(out IMapService mapService)) return;

        mapService.Register(this);
        registered = true;
    }
}
