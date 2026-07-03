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
        boxCollider.isTrigger = true;
        boxCollider.center = data.center;
        boxCollider.size = data.boxSize;
    }

   
}
