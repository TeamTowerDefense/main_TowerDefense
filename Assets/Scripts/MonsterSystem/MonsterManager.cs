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
        if (!useAutoSpawn) return;

        // 1. 몬스터 스폰 체크
        foreach (var path in paths)
        {
            path.spawnTimer += Time.deltaTime;
            if (path.spawnTimer >= path.spawnInterval)
            {
                path.spawnTimer = 0f;
                StartCoroutine(SpawnMonsterGroup(path));
            }
        }

        // 2. 통합된 몬스터 로직 루프 (여기에 모든 기능을 합침)
        bool shouldUpdateCache = (Time.frameCount % 5 == 0);

        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            Monster m = activeMonsters[i];

            m.UpdateGridPosition();
            // A. 물리 힘 계산 (몬스터가 직접 계산하게 함)
            Vector3 separationForce = m.CalculateSeparation();

            // B. 속도 캐싱
            if (shouldUpdateCache)
                m.cachedSpeedMultiplier = CalculateSpeedMultiplier(m);

            // C. 업데이트
            m.ManualUpdate(Time.deltaTime, separationForce, pathWidth, containmentStrength, m.cachedSpeedMultiplier);

            // D. 도착 처리 및 풀링 반환
            if (m.IsReachedEnd())
            {
                if (m.TryGetComponent(out MonsterRuntimeBridge bridge))
                    bridge.HandleReachedEnd();

                m.gameObject.SetActive(false);
                activeMonsters.RemoveAt(i);
                ObjectPoolManager.Instance.Despawn(m);
            }
        }
    }*/

    // 임시 변경
    private void Update()
    {
        if (useAutoSpawn)
        {
            foreach (var path in paths)
            {
                path.spawnTimer += Time.deltaTime;

                if (path.spawnTimer < path.spawnInterval) continue;

                path.spawnTimer = 0f;
                StartCoroutine(SpawnMonsterGroup(path));
            }
        }

        foreach (var list in gridBuckets.Values) list.Clear();

        foreach (var m in activeMonsters)
        {
            m.UpdateGridPosition(tileSize);

            if (!gridBuckets.ContainsKey(m.CurrentGridPos))
                gridBuckets[m.CurrentGridPos] = new List<Monster>();

            gridBuckets[m.CurrentGridPos].Add(m);
        }

        bool shouldUpdateCache = Time.frameCount % 5 == 0;

        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            Monster m = activeMonsters[i];
            Vector3 separationForce = CalculateSeparation(m);

            if (shouldUpdateCache)
                m.cachedSpeedMultiplier = CalculateSpeedMultiplier(m);

            m.ManualUpdate(
                Time.deltaTime,
                separationForce,
                pathWidth,
                containmentStrength,
                m.cachedSpeedMultiplier);

            if (!m.IsReachedEnd()) continue;

            if (m.TryGetComponent(out MonsterRuntimeBridge bridge))
                bridge.HandleReachedEnd();

            m.OnMonsterDie -= HandleMonsterDeath;
            m.gameObject.SetActive(false);
            activeMonsters.RemoveAt(i);
            monsterPool.Enqueue(m);
        }
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
    public Coroutine SpawnPathGroup(PathData pathData) => StartCoroutine(SpawnMonsterGroup(pathData));

    IEnumerator SpawnMonsterGroup(PathData pathData)
    {
        for (int i = 0; i < pathData.countPerSpawn; i++)
        {
            Monster m = ObjectPoolManager.Instance.Spawn<Monster>(
                pathData.monsterData.Prefab,
                pathData.waypoints[0].position,
                Quaternion.identity
            );

            if (m == null) continue;

            // [중요!] 여기서 매니저의 설정값들을 직접 넘겨줍니다.
            // Monster는 이제 MonsterManager.Instance를 호출할 필요가 없습니다.
            m.Setup(pathData.waypoints, spawnY, pathData.monsterData, separationRadius, separationStrength);

            activeMonsters.Add(m);
            yield return new WaitForSeconds(pathData.spawnInterval);
        }
    }
    private void HandleMonsterDeath(Monster deadMonster)
    {
        // 이벤트 구독 해지 (메모리 누수 방지)
        deadMonster.OnMonsterDie -= HandleMonsterDeath;

        // 활성 리스트에서 제거
        if (activeMonsters.Contains(deadMonster))
        {
            activeMonsters.Remove(deadMonster);
        }

        // 풀링으로 반환
        ObjectPoolManager.Instance.Despawn(deadMonster);

        Debug.Log("몬스터가 죽어서 풀로 돌아갔습니다.");

    }

    float CalculateSpeedMultiplier(Monster m)
    {
        return 1.0f; // 추가적인 가속 제어 시 확장
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
}