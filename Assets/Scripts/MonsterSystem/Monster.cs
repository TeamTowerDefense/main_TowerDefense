using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    private Animator anim;
    private Collider col;
    [SerializeField]
    private int hp = 100;

    public float speed = 1f;
    private List<Transform> movePath;
    private int currentPathIndex = 1;
    private Vector3 pathOffset;
    public bool isDead { get; private set; } = false;


    public float cachedSpeedMultiplier = 1.0f;

    public Vector2Int CurrentGridPos { get; private set; }
    public event Action<Monster> OnMonsterDie;

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
    }

    public void Initialize(List<Transform> path, float spawnY)
    {

        if (anim != null) anim.ResetTrigger("Die");
        if (col != null) col.enabled = true;
        isDead = false;

        movePath = path;
        currentPathIndex = 1;
        pathOffset = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0, UnityEngine.Random.Range(-0.4f, 0.4f));

        if (movePath != null && movePath.Count > 0)
        {
            transform.position = movePath[0].position + new Vector3(pathOffset.x, spawnY, pathOffset.z);
        }
    }

    public void UpdateGridPosition(float tileSize)
    {
        CurrentGridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x / tileSize), Mathf.RoundToInt(transform.position.z / tileSize));
    }

    public void ManualUpdate(float deltaTime, Vector3 separationForce, float pathWidth, float containmentStrength, float speedMultiplier)
    {
        if (isDead) return;
        if (movePath == null || currentPathIndex >= movePath.Count) return;

        Transform targetTile = movePath[currentPathIndex];
        Vector3 startPos = movePath[currentPathIndex - 1].position;
        Vector3 lineDir = (targetTile.position - startPos).normalized;
        lineDir.y = 0;

        Vector3 toMonster = transform.position - startPos;
        toMonster.y = 0;
        float projection = Vector3.Dot(toMonster, lineDir);
        Vector3 centerPointOnLine = startPos + (lineDir * projection);
        float distFromCenter = Vector3.Distance(transform.position, centerPointOnLine);

        Vector3 boundaryForce = Vector3.zero;
        if (distFromCenter > pathWidth)
        {
            float forceMagnitude = (distFromCenter - pathWidth) * containmentStrength * 5f;
            Vector3 dir = (centerPointOnLine - transform.position);
            dir.y = 0;
            boundaryForce = (centerPointOnLine - transform.position).normalized * forceMagnitude;
        }

        Vector3 toTarget = (targetTile.position + pathOffset) - transform.position;
        toTarget.y = 0;

        Vector3 moveDir = toTarget.normalized;
        Vector3 finalDir = (moveDir + separationForce + boundaryForce).normalized;

        transform.position += finalDir * speed * speedMultiplier * deltaTime;

        if (finalDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalDir), 10f * deltaTime);

        if (toTarget.magnitude < 0.5f) currentPathIndex++;
    }

    public bool IsReachedEnd() => movePath == null || currentPathIndex >= movePath.Count;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        if (col != null) col.enabled = false;
  
        StartCoroutine(DieCoroutine());
        
    }

    private IEnumerator DieCoroutine()
    {
        // 1. 애니메이터에 Die 트리거 발동 (Animator Controller에 'Die' 파라미터가 있어야 합니다)
        anim.SetTrigger("Die");

        // 애니메이션 재생되는 시간 동안
        yield return new WaitForSeconds(1.5f);

        OnMonsterDie?.Invoke(this);

        gameObject.SetActive(false);
    }
}