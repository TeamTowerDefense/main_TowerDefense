using System;

using System.Collections;

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

using UnityEngine.ResourceManagement.AsyncOperations;


public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager instance;

    [Header("Pool Parents")]
    [SerializeField] private Transform projectileParent;
    [SerializeField] private Transform effectParent;

    [Header("DataBase")]
    [SerializeField] private ProjectileDB projectileDB;
    [SerializeField] private HitBoxDB hitBoxDB;
    [SerializeField] private EffectDB effectDatabase;
    public static ObjectPoolManager Instance { get; private set; }

    // 오브젝트 풀
    private Dictionary<GameObject, Queue<PoolableObject>> pools = new();

    private Dictionary<int, GameObject> projectileTable = new();
    private Dictionary<int, GameObject> hitBoxTable = new();
    private Dictionary<int, GameObject> effectTable = new();

    private Dictionary<string, GameObject> loadedPrefabs = new();


    private void Awake()
    {
        Instance = this;
    }
    private IEnumerator Start()
    {
        yield return LoadProjectileAssets();
        yield return LoadHitBoxAssets();
        yield return LoadEffectAssets();

        Debug.Log("모든 Addressable 로드 완료");
    }

    private IEnumerator LoadProjectileAssets()
    {
        foreach (ProjectileData data in projectileDB.projectiles)
        {
            AsyncOperationHandle handle =
                Addressables.LoadAssetsAsync<GameObject>(
                    data.label,
                    prefab =>
                    {
                        projectileTable[data.projectileID] = prefab;

                        Debug.Log(
                            $"Projectile 등록 : {data.projectileID}");
                    });

            yield return handle;
        }
    }
    private IEnumerator LoadHitBoxAssets()
    {
        foreach (HitBoxData data in hitBoxDB.hitBoxes)
        {

            AsyncOperationHandle handle =
                Addressables.LoadAssetsAsync<GameObject>(
                    data.label,
                    prefab =>
                    {
                        hitBoxTable[data.hitBoxID] = prefab;
                        
                        Debug.Log($"HitBox 등록 : {data.hitBoxID}");
                    });

            yield return handle;
        }
    }

    private IEnumerator LoadEffectAssets()
    {


        foreach (EffectData data in effectDatabase.effects)
        {
            Debug.Log($"[Effect] 로드 시도 ID={data.effectID}, Label={data.label}");
            AsyncOperationHandle handle = 
                Addressables.LoadAssetsAsync<GameObject>(
                data.label,
                prefab =>
                {
                    effectTable[data.effectID] = prefab;
                    Debug.Log($"Effect 등록 시도 ID={data.effectID}, Label={data.label}, Prefab={prefab.name}");
                    //Debug.Log($"Effect 등록 : {data.effectID} / {prefab.name}");
                });
            

            yield return handle;
        }
    }

    public GameObject GetProjectile(int id)
    {
        projectileTable.TryGetValue(id, out GameObject prefab);
        return prefab;
    }

    public GameObject GetHitBox(int id)
    {
        //Debug.Log($"HitBox 요청 ID : {id}");

        if (hitBoxTable.TryGetValue(id, out GameObject prefab))
        {
            //Debug.Log($"HitBox 찾음 : {prefab.name}");
            return prefab;
        }

        //Debug.LogError($"HitBox ID 없음 : {id}");
        return null;
    }

    public GameObject GetEffect(int id)
    {
        //Debug.Log($"[Effect] 요청 ID={id}");

        if (effectTable.TryGetValue(id, out GameObject prefab))
        {
            //Debug.Log($"[Effect] 찾음 {prefab.name}");
            return prefab;
        }

       // Debug.LogError($"[Effect] ID 없음 : {id}");
        return null;
    }

    public async Task<GameObject> LoadPrefabAsync(AssetReferenceGameObject reference)
    {
        if (reference == null || !reference.RuntimeKeyIsValid())
        {
            //Debug.Log("Addresable Reference 없음");
            return null;
        }

        string key = reference.RuntimeKey.ToString();

        if (loadedPrefabs.TryGetValue(key, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        AsyncOperationHandle<GameObject> handle = reference.LoadAssetAsync<GameObject>();

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            //Debug.Log($"Addressable Load 실패 : {key}");
            return null;
        }

        loadedPrefabs[key] = handle.Result;
        return handle.Result;
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
                //Debug.LogError($"{prefab.name}에 PoolableObject가 없음");
                Destroy(newObj);
                return null;
            }

            obj.SetPrefabKey(prefab);
        }

        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.OnSpawned();

        //Debug.Log($"[Pool] 실제 타입 : {obj.GetType().Name}, 요청 타입 : {typeof(T).Name}");

        return obj as T;
    }
    #endregion

    #region 디스폰 메서드
    public void Despawn(PoolableObject obj)
    {
        if (obj == null)
            return;

        GameObject key = obj.prefabKey;

        if (key == null)
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

    #region 코루틴용 로드 메서드
    public IEnumerator LoadPrefabCoroutine(
        AssetReferenceGameObject reference, System.Action<GameObject> onLoaded)
    {
        if (reference == null || !reference.RuntimeKeyIsValid())
        {
            Debug.LogError("Addressable Reference 없음");
            onLoaded?.Invoke(null);
            yield break;
        }

        string key = reference.RuntimeKey.ToString();

        if (loadedPrefabs.TryGetValue(key, out GameObject cachedPrefab))
        {
            onLoaded?.Invoke(cachedPrefab);
            yield break;
        }
        AsyncOperationHandle<GameObject> handle = reference.LoadAssetAsync<GameObject>();

        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Addressable Load 실패 : {key}");
            onLoaded?.Invoke(null);
            yield break;
        }
        loadedPrefabs[key] = handle.Result;
        onLoaded?.Invoke(handle.Result);
    }
    #endregion
}
