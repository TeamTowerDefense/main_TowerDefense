using UnityEngine;

public class SphereHitBoxInitializer : MonoBehaviour, IHitBoxShapeInitializer
{
    [SerializeField] private SphereCollider spherCollider;

    private void Awake()
    {
        if (spherCollider == null)
            spherCollider = GetComponent<SphereCollider>();
    }

    public void Initialize(HitBoxData data)
    {
        SphereHitboxData hitboxData = data as SphereHitboxData;

        spherCollider.isTrigger = true;
        spherCollider.center = hitboxData.center;
        spherCollider.radius = hitboxData.sphereRadius;
    }

}
