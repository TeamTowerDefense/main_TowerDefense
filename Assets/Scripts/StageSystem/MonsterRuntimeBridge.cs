using IGameInterface;
using System.Collections.Generic;
using UnityEngine;

public class MonsterRuntimeBridge : MonoBehaviour, IAttackTarget, IStageDamageSource
{
    [SerializeField] Monster monster;
    [SerializeField] InterfaceReference<IEnemyInfoWriter> infoWriter;
    [SerializeField] Transform targetTransform;

    MonsterData monsterData;
    List<Transform> path;
    IStageService stageService;
    bool reachedHandled;

    public Transform TargetTransform => targetTransform ? targetTransform : transform;
    public bool CanBeDamaged => isActiveAndEnabled && monster != null;
    public int LeakDamage => monsterData != null ? monsterData.LeakDamage : 1;

    #region 생명주기
    private void Reset()
    {
        monster = GetComponent<Monster>();
        targetTransform = transform;

        if (TryGetComponent(out IEnemyInfoWriter writer) && writer is Object obj)
            infoWriter.SetTarget(obj);
    }
    private void Awake()
    {
        if (!monster) monster = GetComponent<Monster>();
        if (!targetTransform) targetTransform = transform;

        if (!infoWriter.HasValue && TryGetComponent(out IEnemyInfoWriter writer) && writer is Object obj)
            infoWriter.SetTarget(obj);

        ServiceLocator.TryGet(out stageService);
    }
    void OnEnable()
    {
        reachedHandled = false;

        if (monster != null)
            monster.OnMonsterDie += HandleMonsterDie;

        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(true, true, 0f);
    }

    void OnDisable()
    {
        if (monster != null)
            monster.OnMonsterDie -= HandleMonsterDie;

        SetEnemyInfo(false, false);
    }

    void Update()
    {
        UpdatePathProgress();

        CheckReachedEnd();
    }
    #endregion

    public void Initialize(MonsterData data, List<Transform> movePath, float spawnY, float separationRadius, float separationStrength)
    {
        monsterData = data;
        path = movePath;
        reachedHandled = false;

        if (monster != null) monster.Setup(movePath, spawnY, data, separationRadius, separationRadius);

        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(true, true, 0f);
    }

    public void TakeDamage(float damage)
    {
        if (monster == null || !CanBeDamaged) return;
        monster.TakeDamage((int)damage);
    }

    public void HandleReachedEnd()
    {
        if (reachedHandled) return;

        reachedHandled = true;

        if (stageService == null) ServiceLocator.TryGet(out stageService);

        stageService?.TakeBaseDamage(LeakDamage);

        SetEnemyInfo(false, false);
    }

    public void BindPath(List<Transform> movePath)
    {
        path = movePath;
        reachedHandled = false;

        infoWriter.Value?.SetAttackTarget(this);
        SetEnemyInfo(true, true, 0f);
    }

    void CheckReachedEnd()
    {
        if (reachedHandled || monster == null || !monster.IsReachedEnd()) return;

        reachedHandled = true;

        if (stageService == null) ServiceLocator.TryGet(out stageService);

        stageService?.TakeBaseDamage(LeakDamage);

        SetEnemyInfo(false, false);
        gameObject.SetActive(false);
    }

    void HandleMonsterDie(Monster deadMonster) => SetEnemyInfo(false, false);

    void SetEnemyInfo(bool alive, bool targetTable, float? progress = null)
    {
        IEnemyInfoWriter writer = infoWriter.Value;
        if (writer == null) return;

        writer.SetAlive(alive);
        writer.SetTargetable(targetTable);

        if (progress.HasValue) writer.SetPathProgress(progress.Value);
    }
    void UpdatePathProgress()
    {
        if (path == null || path.Count < 2 || !infoWriter.TryGet(out IEnemyInfoWriter writer)) return;

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
            Vector3 a = path[i - 1].position;
            Vector3 b = path[i].position;

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

}
