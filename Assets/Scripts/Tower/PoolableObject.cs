using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    public GameObject prefabKey { get; private set; }

    private Transform poolParent;

    public void SetPrefabKey(GameObject key)
    {
        prefabKey = key;
    }

    public virtual void OnSpawned()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnDespawned()
    {
        if (poolParent != null)
        {
            transform.SetParent(poolParent, false);
        }
        gameObject.SetActive(false);
    }
}
