using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 2f;

    public void Init(float radius)
    {
        explosionRadius = radius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

}
