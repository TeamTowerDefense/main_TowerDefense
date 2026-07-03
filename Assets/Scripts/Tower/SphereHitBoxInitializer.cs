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
        spherCollider.isTrigger = true;
        spherCollider.center = data.center;
        spherCollider.radius = data.sphereRadius;
    }

}
