using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Monster : PoolableObject
{
    [Header("Components")]
    private Animator anim;
    private Collider col;


    public float currentHp { get; private set; }
    public float maxHp { get; private set; }    
    private float speed;
    private float moveWeight ;
    private float separationWeight;
    private float boundaryWeight; 
    private float containmentMultiplier;

    private Tile currentTile;

    private List<Transform> movePath;
    private int currentPathIndex = 1;
    private Vector3 pathOffset;
    public bool isDead { get; private set; } = false;
    public float cachedSpeedMultiplier = 1.0f;
    public Vector2Int CurrentGridPos { get; private set; }
    public event Action<Monster> OnMonsterDie;

    private EnemyInfoProvider enemyInfoProvider;

    [SerializeField]
    private HpBar hpBar;

    private IAbility[] allAbilities;
    private MonsterStatus status;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
        enemyInfoProvider = GetComponent<EnemyInfoProvider>();
        allAbilities = GetComponents<IAbility>();
        status = GetComponent<MonsterStatus>();
    }
    private void OnDisable()
    {
        ClearCurrentTile();
    }
    // 초기화 로직 통합
    public void Setup(List<Transform> path, float spawnY, MonsterData data,float separationRadius, float separationStrength)
    {
        foreach (var ability in allAbilities)
        {
            ability.DisableAbility();
        }
        foreach (var abilityData in data.abilities)
        {
            // 몬스터에 붙어있는 능력들 중에서 데이터 타입이 맞는 놈을 찾아서 켭니다.
            foreach (var ability in allAbilities)
            {
                // 이 능력 스크립트가 해당 데이터를 처리할 수 있는지 확인
                // (간단하게 하려면 타입 비교 후 EnableAbility 호출)
                if (CanHandle(ability, abilityData))
                {
                    ability.EnableAbility(abilityData);
                }
            }
        }

        currentHp = data.maxHP;
        maxHp = data.maxHP;
        speed = data.speed;
        moveWeight = data.moveWeight;
        separationWeight = data.separationWeight;
        boundaryWeight = data.boundaryWeight;
        containmentMultiplier = data.containmentMultiplier;
        status.Setup(data.StunGauge);


        isDead = false;

        hpBar.UpdateHp(1.0f);

        hpBar.gameObject.SetActive(false);

        if (col != null) col.enabled = true;
        if (anim != null) anim.ResetTrigger("Die");

        movePath = path;
        currentPathIndex = 1;
        pathOffset = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0, UnityEngine.Random.Range(-0.4f, 0.4f));

        if (movePath != null && movePath.Count > 0)
        {
            transform.position = movePath[0].position + new Vector3(pathOffset.x, spawnY, pathOffset.z);
        }

        if (TryGetComponent(out MonsterRuntimeBridge bridge))
            bridge.BindPath(movePath);

        gameObject.SetActive(true);
        
    }

    public void UpdateGridPosition()
    {
        Vector2Int newGridPos = new Vector2Int(
        Mathf.RoundToInt(transform.position.x / MonsterManager.Instance.tileSize),
        Mathf.RoundToInt(transform.position.z / MonsterManager.Instance.tileSize));
        if (newGridPos == CurrentGridPos) return;

        // 3. 이제 진짜로 타일이 바뀐 경우에만 처리
        Tile oldTile = currentTile;
        Tile newTile = MonsterManager.Instance.GetTileAt(newGridPos);

        // 이전 타일에서 나가고
        oldTile?.RemoveMonster(this);
        ClearCurrentTile(); // 이전 타일 참조 해제
        // 새 타일로 들어가고
        newTile?.AddMonster(this);
       
        // 상태 업데이트
        currentTile = newTile;
        CurrentGridPos = newGridPos;
    }

    public void ManualUpdate(float deltaTime, Vector3 separationForce, float pathWidth, float containmentStrength, float speedMultiplier)
    {
        if (isDead || movePath == null || currentPathIndex >= movePath.Count || status.IsStunned) return;

        Transform targetTile = movePath[currentPathIndex];
        Vector3 startPos = movePath[currentPathIndex - 1].position;
        Vector3 lineDir = (targetTile.position - startPos).normalized;
        lineDir.y = 0;

        Vector3 toMonster = transform.position - startPos;
        toMonster.y = 0;
        float projection = Vector3.Dot(toMonster, lineDir);
        Vector3 centerPointOnLine = startPos + (lineDir * projection);
        float distFromCenter = Vector3.Distance(transform.position, centerPointOnLine);

        // 1. 경로 복귀 힘 계산
        Vector3 boundaryForce = Vector3.zero;
        if (distFromCenter > pathWidth)
        {
            float forceMagnitude = (distFromCenter - pathWidth) * containmentStrength * containmentMultiplier;
            boundaryForce = (centerPointOnLine - transform.position).normalized * forceMagnitude;
        }

        // 2. 우선순위 적용: 경로를 벗어나면 밀어내는 힘(Separation) 무효화
        Vector3 effectiveSeparation = (distFromCenter > pathWidth) ? Vector3.zero : (separationForce * separationWeight);

        // 3. 이동 방향
        Vector3 toTarget = (targetTile.position + pathOffset) - transform.position;
        toTarget.y = 0;
        Vector3 moveDir = toTarget.normalized;

        // 4. 최종 방향 (가중치 기반 계산)
        Vector3 finalDir = (moveDir * moveWeight + effectiveSeparation + (boundaryForce * boundaryWeight)).normalized;
        // 5. 최종 속도
        float finalSpeed = speed * speedMultiplier * status.SlowMultiplier;
        transform.position += finalDir * finalSpeed * deltaTime;

        if (finalDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalDir), 10f * deltaTime);

        if (toTarget.magnitude < 0.5f) currentPathIndex++;
    }
    public bool IsReachedEnd() => movePath == null || currentPathIndex >= movePath.Count;

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;

        float ratio = currentHp / maxHp;
        hpBar.UpdateHp(ratio);
        bool isDamaged = (currentHp < maxHp);
        if (hpBar.gameObject.activeSelf != isDamaged)
        {
            hpBar.gameObject.SetActive(isDamaged);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void TakeHeal(int healAmount)
    {
        if (isDead) return;
        Debug.Log("TakeHeal 호출됨");
        currentHp += healAmount;

        // 최대 체력 넘지 않게 고정
        if (currentHp > maxHp) currentHp = maxHp;

        // HP바 UI 갱신 (이미 만들어둔 로직 재사용)
        float ratio = (float)currentHp / maxHp;
        hpBar.UpdateHp(ratio);

        // 치유 효과 파티클/텍스트 생성 코드 (선택 사항)
        // Instantiate(healEffect, transform.position, Quaternion.identity);
    }
    public void Die()
    {
        if (isDead || !gameObject.activeInHierarchy) return;
        isDead = true;
        enemyInfoProvider.SetAlive(false);
        enemyInfoProvider.SetTargetable(false);
        if (currentTile != null)
        {
            currentTile.RemoveMonster(this);
            currentTile = null;
        }
        
        if (col != null) col.enabled = false;
       
        StartCoroutine(DieCoroutine());

    }

    private IEnumerator DieCoroutine()
    {
        anim.SetTrigger("Die");
        // 애니메이션 재생되는 시간 동안
        yield return new WaitForSeconds(2f);
        ObjectPoolManager.Instance.Despawn(this);
        OnMonsterDie?.Invoke(this);


    }
    public Vector3 GetSeparationForce(Monster other, float radius, float strength)
    {
        // 1. 거리 계산
        Vector3 diff = transform.position - other.transform.position;
        diff.y = 0; // 지상 게임이므로 y축은 제외

        float dist = diff.magnitude;

        // 2. 너무 멀면 힘을 가하지 않음 (최적화)
        if (dist > radius || dist < 0.0001f) return Vector3.zero;

        // 3. 거리 기반으로 힘 계산 (Linear Falloff)
        // 거리가 가까울수록 1에 가까운 값이 곱해져서 더 강하게 밉니다.
        float forceMagnitude = (1.0f - (dist / radius)) * strength;

        return diff.normalized * forceMagnitude;
    }
    // 능력 처리 가능 여부 확인
    private bool CanHandle(IAbility ability, AbilityData data)
    {
        // 힐러 컴포넌트인지 확인하는 예시
        if (ability is Healer && data is HealAbilityData) return true;
        return false;
    }
    public void ClearCurrentTile()
    {
        if (currentTile != null)
        {
            currentTile.RemoveMonster(this);
            currentTile = null; // 참조 해제
        }
    }
    public override void OnSpawned()
    {
        base.OnSpawned();
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
    }
}