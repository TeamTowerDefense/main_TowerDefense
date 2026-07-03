using UnityEngine;

public class CapsuleColliderInitializer : MonoBehaviour, IHitBoxShapeInitializer
{
    [SerializeField] private CapsuleCollider capsuleCollider;

    private void Awake()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();
    }

    public void Initialize(HitBoxData data)
    {
        capsuleCollider.isTrigger = true;
        capsuleCollider.center = data.center;
        capsuleCollider.radius = data.capsuleRadius;
        capsuleCollider.height = data.capsuleHeight;
        capsuleCollider.direction = data.capsuleDirection;
    }
}
