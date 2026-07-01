using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager instance;

    [Header("Pool Parents")]
    [SerializeField] private Transform projectileParent;
    [SerializeField] private Transform effectParent;

    public static ObjectPoolManager Instance { get; private set; }

    private Dictionary<GameObject, Queue<PoolableObject>> pools = new();

    private void Awake()
    {
        Instance = this;
    }


    #region 스폰 메서드
    public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : PoolableObject
    {
        //Debug.Log($"[Pool] Spawn 요청 prefab : {(prefab != null ? prefab.name : "NULL")}, 요청 타입 : {typeof(T).Name}");

        if (prefab == null)
            return null;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<PoolableObject>();  
        }

        PoolableObject obj;

        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();
        }
        else
        {
            GameObject newObj = Instantiate(prefab, parent);
            obj = newObj.GetComponent<PoolableObject>();

            if (obj == null)
            {
                Debug.LogError($"{prefab.name}에 PoolableObject가 없음");
                Destroy(newObj);
                return null;
            }

            obj.SetPrefabKey(prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.OnSpawned();

        Debug.Log($"[Pool] 실제 타입 : {obj.GetType().Name}, 요청 타입 : {typeof(T).Name}");

        return obj as T;
    }
    #endregion

    #region projectile 부모 좌표 가져오기
    public Transform GetProjectileParent()
    {
        return projectileParent != null ? projectileParent : transform;
    }
    #endregion

    #region Effect 부모 좌표 가져오기
    public Transform GetEffectParent()
    {
        return effectParent != null ? effectParent : transform;
    }
    #endregion

    #region 디스폰 메서드
    public void Despawn(PoolableObject obj)
    {
        if (obj == null)
            return;

        GameObject key = obj.prefabKey;

        if(key == null)
        {
            Debug.LogError($"{obj.name} prefabKey 없음");
            Destroy(obj.gameObject);
            return;
        }

        obj.OnDespawned();

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<PoolableObject>();

        pools[key].Enqueue(obj);
    }
    #endregion

}
