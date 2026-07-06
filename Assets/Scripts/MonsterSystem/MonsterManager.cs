using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathData
{
    public string pathName;
    public MonsterData monsterData;
    public List<Transform> waypoints;
    public int countPerSpawn = 5;
    public float spawnInterval = 3.0f;
    [HideInInspector] public float spawnTimer = 0f;
}

public class MonsterManager : MonoBehaviour
{
    private Dictionary<Vector2Int, Tile> pathTiles = new Dictionary<Vector2Int, Tile>();

    [SerializeField] bool useAutoSpawn = true;

    private static MonsterManager _instance;
    public static MonsterManager Instance
    {
        get
        {
            // Instance가 null이라면, 씬에서 자동으로 찾아 할당함 (Lazy Loading)
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<MonsterManager>();
            }
            return _instance;
        }
    }
    public GameObject monsterPrefab;
    public float spawnY = 0f;

    public float separationRadius = 1.5f, separationStrength = 2.0f;
    public float tileSize = 1, pathWidth = 1.5f, containmentStrength = 3.0f;
    public List<PathData> paths;

    private Dictionary<Vector2Int, List<Monster>> gridBuckets = new Dictionary<Vector2Int, List<Monster>>();
    private List<Monster> activeMonsters = new List<Monster>();

    void Awake()
    {
        _instance = this; // 자기 자신을 등록
    }
    private void Start()
    {
        // 게임 시작 시 한 번만 실행
        StartCoroutine(DelayedInitialization());
    }
    void Update()
    {
        UpdateGridBuckets();
        ProcessMonsters();
    }
    public void SpawnPathGroup(PathData pathData, int count, float interval)
    {
        StartCoroutine(SpawnMonsterGroup(pathData, count, interval));
    }

    private IEnumerator SpawnMonsterGroup(PathData pathData, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            Monster m = ObjectPoolManager.Instance.Spawn<Monster>(
                pathData.monsterData.Prefab,
                pathData.waypoints[0].position,
                Quaternion.identity
            );

            if (m != null)
            {
                
                
                m.Setup(pathData.waypoints, spawnY, pathData.monsterData, separationRadius, separationStrength);
                m.UpdateGridPosition();
                m.OnMonsterDie += HandleMonsterDeath;
                m.gameObject.SetActive(true);
                activeMonsters.Add(m);
            }
                
            yield return new WaitForSeconds(interval);
        }
    }

    private void UpdateGridBuckets()
    {
        foreach (var list in gridBuckets.Values) list.Clear();
        foreach (var m in activeMonsters)
        {
            m.UpdateGridPosition();
            Vector2Int pos = m.CurrentGridPos;
            if (!gridBuckets.ContainsKey(pos)) gridBuckets[pos] = new List<Monster>();
            gridBuckets[pos].Add(m);
        }
    }

    private void ProcessMonsters()
    {
        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            Monster m = activeMonsters[i];
            Vector3 force = CalculateSeparation(m);
            m.ManualUpdate(Time.deltaTime, force, pathWidth, containmentStrength, 1.0f);

            if (m.IsReachedEnd()) HandleMonsterReachedEnd(m, i);
        }
    }

    private void HandleMonsterReachedEnd(Monster m, int index)
    {
        if (m.TryGetComponent(out MonsterRuntimeBridge bridge)) bridge.HandleReachedEnd();
        m.OnMonsterDie -= HandleMonsterDeath;
        activeMonsters.RemoveAt(index);
        ObjectPoolManager.Instance.Despawn(m);
    }

    private void HandleMonsterDeath(Monster deadMonster)
    {
        deadMonster.OnMonsterDie -= HandleMonsterDeath;
        if (activeMonsters.Contains(deadMonster)) activeMonsters.Remove(deadMonster);
    }
    public int ActiveMonsterCount => activeMonsters.Count;

    public void StopAutoSpawn()
    {
        foreach (PathData path in paths)
            path.spawnTimer = 0f;

        enabled = false;
    }

    public void StartAutoSpawn()
    {
        enabled = true;
    }

    public void RegisterTile(Tile tile)
    {
        if (!pathTiles.ContainsKey(tile.gridPos))
            pathTiles.Add(tile.gridPos, tile);
    }
    IEnumerator DelayedInitialization()
    {
        yield return null; // 한 프레임 대기 (모든 Awake/Start가 처리될 시간을 줌)
        InitializeTileConnections();
    }
    public void InitializeTileConnections()
    {
        // 1. 모든 타일을 순회
        foreach (var kvp in pathTiles)
        {
            Tile currentTile = kvp.Value;
            Vector2Int pos = kvp.Key;

            // 2. 주변 8방향 확인
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; // 자기 자신은 제외

                    Vector2Int neighborPos = pos + new Vector2Int(x, y);

                    // 3. 해당 좌표에 타일이 존재하면 neighbors 리스트에 추가
                    if (pathTiles.TryGetValue(neighborPos, out Tile neighbor))
                    {
                        if (!currentTile.neighbors.Contains(neighbor))
                        {
                            currentTile.neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }
        Debug.Log("모든 타일의 이웃 연결이 완료되었습니다!");
    }

    public Tile GetTileAt(Vector2Int pos)
    {
        pathTiles.TryGetValue(pos, out Tile tile);
        return tile; // 없으면 null 반환
    }
    private Vector3 CalculateSeparation(Monster m)
    {
        Vector3 force = Vector3.zero;
        Vector2Int pos = m.CurrentGridPos;

      
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkPos = pos + new Vector2Int(x, y);

                // 해당 타일에 몬스터들이 있다면?
                if (gridBuckets.TryGetValue(checkPos, out List<Monster> monsters))
                {
                    foreach (var other in monsters)
                    {
                        // 자기 자신이거나 죽은 몬스터는 패스
                        if (m == other || other.isDead) continue;

                        // 이제 public이 된 GetSeparationForce 호출
                        force += m.GetSeparationForce(other, separationRadius, separationStrength);
                    }
                }
            }
        }
        return force;
    }
}