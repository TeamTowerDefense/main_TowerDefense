using IGameInterface;
using System.Collections.Generic;
using UnityEngine;

public class MonsterRuntimeBridge : MonoBehaviour, IAttackTarget, IStageDamageSource, IMonsterSpawnContextReceiver
{
    [SerializeField] Monster monster;
    [SerializeField] InterfaceReference<IEnemyInfoWriter> infoWriter;
    [SerializeField] Transform targetTransform;

    MonsterData monsterData;
    List<Transform> path;
    IStageService stageService;
    IResourceSystem resourceSystem;
    IStageMonsterTracker monsterTracker;
    bool reachedHandled;

    public Transform TargetTransform => targetTransform ? targetTransform : transform;
    public bool CanBeDamaged => isActiveAndEnabled && monster && monsterData && !monster.isDead && !reachedHandled;
    public int LeakDamage => monsterData ? monsterData.LeakDamage : 1;

    #region 생명주기

    void Reset()
    {
        monster = GetComponent<Monster>();
        targetTransform = transform;
        TryAutoBindInfoWriter();
    }

    void Awake()
    {
        if (!monster) monster = GetComponent<Monster>();
        if (!targetTransform) targetTransform = transform;

        TryAutoBindInfoWriter();
        RefreshServices();
    }

    void OnEnable()
    {
        reachedHandled = false;

        if (monster)
        {
            monster.OnMonsterDie -= HandleMonsterDie;
            monster.OnMonsterDie += HandleMonsterDie;
        }

        TryAutoBindInfoWriter();
        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(false, false, 0f);
    }

    void OnDisable()
    {
        if (monster) monster.OnMonsterDie -= HandleMonsterDie;
        ClearSpawnContext();
    }

    void Update()
    {
        UpdatePathProgress();
    }

    #endregion

    #region 바인딩

    public void BindSpawnContext(Monster owner, MonsterData data, List<Transform> waypoints)
    {
        if (!monster) monster = owner ? owner : GetComponent<Monster>();
        if (!targetTransform) targetTransform = transform;

        RefreshServices();

        monsterData = data;
        path = waypoints != null ? new List<Transform>(waypoints) : null;
        reachedHandled = false;

        monsterTracker?.Register(gameObject);

        TryAutoBindInfoWriter();
        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(data, data, 0f);
    }

    public void BindPath(List<Transform> movePath)
    {
        path = movePath != null ? new List<Transform>(movePath) : null;
        reachedHandled = false;

        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(monsterData, monsterData, 0f);
    }

    public void ClearSpawnContext()
    {
        monsterTracker?.Unregister(gameObject);

        monsterData = null;
        path = null;
        reachedHandled = false;

        SetEnemyInfo(false, false, 0f);
    }

    void TryAutoBindInfoWriter()
    {
        if (infoWriter.HasValue) return;
        if (TryGetComponent(out IEnemyInfoWriter writer) && writer is Object obj) infoWriter.SetTarget(obj);
    }

    #endregion

    #region 전투/스테이지

    public void TakeDamage(float damage)
    {
        if (!CanBeDamaged) return;
        monster.TakeDamage((int)damage);
    }

    public void HandleReachedEnd()
    {
        if (reachedHandled || !monsterData) return;

        reachedHandled = true;
        RefreshServices();

        stageService?.TakeBaseDamage(LeakDamage);
        SetEnemyInfo(false, false);
    }

    void HandleMonsterDie(Monster deadMonster)
    {
        RefreshServices();
        SetEnemyInfo(false, false);

        if (!monsterData)
        {
            Debug.LogWarning("[MonsterRuntimeBridge] MonsterData 없이 사망 처리됨. BindSpawnContext 호출을 확인하세요.", this);
            return;
        }

        resourceSystem?.Earn(monsterData.amount);
    }

    void RefreshServices()
    {
        ServiceLocator.TryGet(out stageService);
        ServiceLocator.TryGet(out resourceSystem);
        ServiceLocator.TryGet(out monsterTracker);
    }

    #endregion

    #region 정보 표시

    void SetEnemyInfo(bool alive, bool targetable, float? progress = null)
    {
        IEnemyInfoWriter writer = infoWriter.Value;
        if (writer == null) return;

        writer.SetAlive(alive);
        writer.SetTargetable(targetable);
        if (progress.HasValue) writer.SetPathProgress(progress.Value);
    }

    void UpdatePathProgress()
    {
        if (reachedHandled || !monsterData || !monster) return;
        if (path == null || path.Count < 2 || !monster.gameObject.activeInHierarchy) return;
        if (!infoWriter.TryGet(out IEnemyInfoWriter writer)) return;

        writer.SetPathProgress(EstimatePathProgress(transform.position));
    }

    float EstimatePathProgress(Vector3 position)
    {
        float totalLength = 0f;
        float passedLength = 0f;
        float closestDistanceSqr = float.MaxValue;
        float closestPassedLength = 0f;

        Vector3 p = position;
        p.y = 0f;

        for (int i = 1; i < path.Count; i++)
        {
            Transform prev = path[i - 1];
            Transform next = path[i];

            if (!prev || !next) continue;

            Vector3 a = prev.position;
            Vector3 b = next.position;

            a.y = 0f;
            b.y = 0f;

            Vector3 ab = b - a;
            float segmentLength = ab.magnitude;
            if (segmentLength <= 0.001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / (segmentLength * segmentLength));
            Vector3 closest = a + ab * t;
            float distanceSqr = (p - closest).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestPassedLength = passedLength + segmentLength * t;
            }

            passedLength += segmentLength;
            totalLength += segmentLength;
        }

        return totalLength <= 0.001f ? 0f : Mathf.Clamp01(closestPassedLength / totalLength);
    }

    #endregion
}