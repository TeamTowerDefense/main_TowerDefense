using UnityEngine;

public class EffectAutoDespawn : MonoBehaviour
{
    private PoolableObject poolable;

    private void Awake()
    {
        poolable = GetComponent<PoolableObject>();
    }

    private void OnParticleSystemStopped()
    {
        if (ObjectPoolManager.Instance == null)
            return;

        transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());

        ObjectPoolManager.Instance.Despawn(poolable);
    }
}
