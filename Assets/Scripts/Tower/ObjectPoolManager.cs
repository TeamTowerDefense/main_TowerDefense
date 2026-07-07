
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
    [SerializeField] private Transform monsterParent;

    [Header("DataBase")]
    [SerializeField] private ProjectileDB projectileDB;
    [SerializeField] private HitBoxDB hitBoxDB;
    [SerializeField] private EffectDB effectDatabase;
    public static ObjectPoolManager Instance { get; private set; }

    // 오브젝트 풀
    private Dictionary<GameObject, Queue<PoolableObject>> pools = new();

    private Dictionary<int, ProjectileData> projectileTable = new();
    private Dictionary<int, HitBoxData> hitBoxTable = new();
    private Dictionary<int, EffectData> effectTable = new();
    private Dictionary<int, MonsterEffectData> monsterEffectTable = new();
    private Dictionary<int, MonsterData> monsterTable = new();

    private Dictionary<string, GameObject> loadedPrefabs = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private IEnumerator Start()
    {
        yield return LoadProjectileAssets();
        yield return LoadHitBoxAssets();
        yield return LoadEffectAssets();
        yield return LoadMonsterEffectAssets();
        yield return LoadMonsterAssets();


        Debug.Log("모든 Addressable 로드 완료");
    }

    private IEnumerator LoadProjectileAssets()
    {
        foreach (ProjectileData data in projectileDB.projectiles)
        {
            AsyncOperationHandle handle =
                data.projectilePF.LoadAssetAsync<GameObject>();
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[projectile Load 실패] Data: {data.name}");
                continue;
            }

            GameObject prefab = (GameObject)handle.Result;

            data.loadedPrefab = prefab;
            projectileTable[data.projectileID] = data;

            Debug.Log($"[projectileTable 등록] ID: {data.projectileID}, Data: {data.name}, Prefab: {prefab.name}");
        }
    }
    private IEnumerator LoadHitBoxAssets()
    {
        foreach (HitBoxData data in hitBoxDB.hitBoxes)
        {
            AsyncOperationHandle handle =
                data.hitboxPF.LoadAssetAsync<GameObject>();
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[HitBox Load 실패] Data: {data.name}");
                continue;
            }

            GameObject prefab = (GameObject)handle.Result;

            data.loadedPrefab = prefab;
            hitBoxTable[data.hitBoxID] = data;

            Debug.Log($"[HitBoxTable 등록] ID: {data.hitBoxID}, Data: {data.name}, Prefab: {prefab.name}");
        }
    }

    private IEnumerator LoadEffectAssets()
    {
        foreach (EffectData data in effectDatabase.effects)
        {
            if (data == null)
                continue;

            if (data.effectPF == null || !data.effectPF.RuntimeKeyIsValid())
            {
                Debug.LogError($"[Effect] AssetReference 유효하지 않음: {data.name}");
                continue;
            }

            var handle = data.effectPF.LoadAssetAsync<GameObject>();
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[Effect Load 실패] Data: {data.name}");
                continue;
            }

            data.loadedPrefab = handle.Result;
            effectTable[data.effectID] = data;

            Debug.Log($"[EffectTable 등록] ID: {data.effectID}, Data: {data.name}, Prefab: {data.loadedPrefab.name}");
        }
    }

    private IEnumerator LoadMonsterEffectAssets()
    {
        // effectDatabase 안에 Monstereffects 리스트가 있다고 가정합니다.
        foreach (var data in effectDatabase.monsterEffects)
        {
            if (data == null)
                continue;

            if (data.effectPF == null || !data.effectPF.RuntimeKeyIsValid())
            {
                Debug.LogError($"[MonsterEffect] AssetReference 유효하지 않음: {data.name}");
                continue;
            }

            var handle = data.effectPF.LoadAssetAsync<GameObject>();
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[MonsterEffect Load 실패] Data: {data.name}");
                continue;
            }

            data.loadedPrefab = handle.Result;

            // 같은 테이블에 등록하므로 GetEffect(id)를 그대로 사용할 수 있습니다.
            monsterEffectTable[data.effectID] = data;

            Debug.Log($"[MonsterEffectTable 등록] ID: {data.effectID}, Data: {data.name}, Prefab: {data.loadedPrefab.name}");
        }
    }

    private async Task LoadMonsterAssets()
    {
        AsyncOperationHandle<IList<MonsterData>> handler =
            Addressables.LoadAssetsAsync<MonsterData>("MonsterData");

        await handler.Task;

        foreach (MonsterData data in handler.Result)
        {
            monsterTable.Add(data.monsterId, data);
        }

        Addressables.Release(handler);
    }
    public ProjectileData GetProjectileData(int id)
    {
        if (projectileTable.TryGetValue(id, out ProjectileData data))
            return data;

        return null;
    }
    public GameObject   GetProjectile(int id)
    {
        ProjectileData data = GetProjectileData(id);
        return data != null ? data.loadedPrefab : null;
    }

    public HitBoxData GetHitBoxData(int id)
    {
        if (hitBoxTable.TryGetValue(id, out HitBoxData data))
            return data;

        return null;
    }

    public GameObject GetHitBox(int id)
    {
        HitBoxData data = GetHitBoxData(id);

        if (data == null)
        {
            Debug.LogError($"[HitBox] Data 없음 ID: {id}");
            return null;
        }

        if (data.loadedPrefab == null)
        {
            Debug.LogError($"[HitBox] loadedPrefab 없음 ID: {id}, Data: {data.name}");
            return null;
        }

        return data != null ? data.loadedPrefab : null;
    }

    public EffectData GetEffectData(int id)
    {
        if (effectTable.TryGetValue(id, out EffectData data))
            return data;

        return null;
    }

    public GameObject GetEffect(int id)
    {
        EffectData data = GetEffectData(id);
        return data != null ? data.loadedPrefab : null;
    }
    public MonsterEffectData GetMonsterEffectData(int id)
    {
        if (monsterEffectTable.TryGetValue(id, out MonsterEffectData data))
            return data;

        return null;
    }

    public GameObject GetMonsterEffect(int id)
    {
        MonsterEffectData data = GetMonsterEffectData(id);
        return data != null ? data.loadedPrefab : null;
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

    public Transform GetMonsterParent()
    {
        return monsterParent != null ? monsterParent : transform;
    }

    

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
