using UnityEngine;

public class BoxHitBoxInitializer : MonoBehaviour, IHitBoxShapeInitializer
{
    [SerializeField] private BoxCollider boxCollider;

    private void Awake()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();
    }

    public void Initialize(HitBoxData data)
    {
        BoxHitboxData hitboxData = data as BoxHitboxData;

        boxCollider.isTrigger = true;
        boxCollider.center = hitboxData.center;
        boxCollider.size = hitboxData.boxSize;
    }
}
