using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    public GameObject prefabKey { get; private set; }

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
        gameObject.SetActive(false);
    }
}
