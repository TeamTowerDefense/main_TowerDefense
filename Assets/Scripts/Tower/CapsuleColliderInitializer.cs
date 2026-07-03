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
        CapsuleHitboxData hitboxData = data as CapsuleHitboxData;

        capsuleCollider.isTrigger = true;
        capsuleCollider.center = hitboxData.center;
        capsuleCollider.radius = hitboxData.capsuleRadius;
        capsuleCollider.height = hitboxData.capsuleHeight;
        capsuleCollider.direction = hitboxData.capsuleDirection;
    }
}
