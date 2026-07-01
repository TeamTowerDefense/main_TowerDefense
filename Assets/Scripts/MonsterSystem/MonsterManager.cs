using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathData
{
    public string pathName;
    public List<Transform> waypoints;
    public int countPerSpawn = 5;
    public float spawnInterval = 3.0f;
    [HideInInspector] public float spawnTimer = 0f;
}

public class MonsterManager : MonoBehaviour
{
    [SerializeField] bool useAutoSpawn = true;

    public static MonsterManager Instance; // 어디서든 접근 가능하게 설정

    public GameObject monsterPrefab;
    public int poolSize = 100;
    public float spawnY = 0f;

    public float separationRadius = 1.5f, separationStrength = 2.0f;
    public float tileSize = 1, pathWidth = 1.5f, containmentStrength = 3.0f;
    public List<PathData> paths;

    private Queue<Monster> monsterPool = new Queue<Monster>();

    private List<Monster> activeMonsters = new List<Monster>();
    private Dictionary<Vector2Int, List<Monster>> gridBuckets = new Dictionary<Vector2Int, List<Monster>>();

    void Awake()
    {
        Instance = this; // 자기 자신을 등록
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(monsterPrefab);
            obj.SetActive(false);
            monsterPool.Enqueue(obj.GetComponent<Monster>());
        }
    }
    /* 원본 업데이트
    void Update()
    {
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

        // 2. 그리드 및 물리 연산
        foreach (var list in gridBuckets.Values) list.Clear();
        foreach (var m in activeMonsters)
        {
            m.UpdateGridPosition(tileSize);
            if (!gridBuckets.ContainsKey(m.CurrentGridPos)) gridBuckets[m.CurrentGridPos] = new List<Monster>();
            gridBuckets[m.CurrentGridPos].Add(m);
        }

        bool shouldUpdateCache = (Time.frameCount % 5 == 0);

        for (int i = activeMonsters.Count - 1; i >= 0; i--)
        {
            Monster m = activeMonsters[i];
            Vector3 separationForce = CalculateSeparation(m);
            if (shouldUpdateCache) m.cachedSpeedMultiplier = CalculateSpeedMultiplier(m);
            m.ManualUpdate(Time.deltaTime, separationForce, pathWidth, containmentStrength, m.cachedSpeedMultiplier);

            //기존 함수
            if (m.IsReachedEnd())
            {
                m.gameObject.SetActive(false);
                activeMonsters.RemoveAt(i);
                monsterPool.Enqueue(m);
            }
            
            if (m.IsReachedEnd())
            {
                if (m.TryGetComponent(out MonsterRuntimeBridge bridge))
                    bridge.HandleReachedEnd();

                m.gameObject.SetActive(false);
                activeMonsters.RemoveAt(i);
                monsterPool.Enqueue(m);
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
                Monster m = GetMonster();
                m.OnMonsterDie -= HandleMonsterDeath;
                m.OnMonsterDie += HandleMonsterDeath;
                m.gameObject.SetActive(true);
                m.Initialize(pathData.waypoints, spawnY);

                if (m.TryGetComponent(out MonsterRuntimeBridge bridge))
                    bridge.BindPath(pathData.waypoints);

                activeMonsters.Add(m);

            if (i < pathData.countPerSpawn - 1)
                yield return new WaitForSeconds(Mathf.Max(0.2f, pathData.spawnInterval));
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
        monsterPool.Enqueue(deadMonster);

        Debug.Log("몬스터가 죽어서 풀로 돌아갔습니다.");

    }
    Vector3 CalculateSeparation(Monster m)
    {
        Vector3 force = Vector3.zero;
        float sqrRadius = separationRadius * separationRadius;
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (gridBuckets.TryGetValue(m.CurrentGridPos + new Vector2Int(x, z), out var list))
                {
                    foreach (var other in list)
                    {
                        if (m == other) continue;
                        if (other.isDead) continue;
                        Vector3 diff = m.transform.position - other.transform.position;
                        diff.y = 0;
                        if (diff.sqrMagnitude < sqrRadius) force += diff.normalized * (1.0f - (diff.magnitude / separationRadius));
                    }
                }
            }
        }
        return force * separationStrength;
    }

    float CalculateSpeedMultiplier(Monster m)
    {
        return 1.0f; // 추가적인 가속 제어 시 확장
    }

    public Monster GetMonster()
    {
        // 1. 풀에 여유가 있다면 꺼내서 사용
        if (monsterPool.Count > 0)
        {
            return monsterPool.Dequeue();
        }
        // 2. 풀이 비었다면 새로 생성해서 반환
        else
        {
            GameObject obj = Instantiate(monsterPrefab);
            Monster newMonster = obj.GetComponent<Monster>();
            obj.SetActive(false);
            return newMonster;
        }
    }
}