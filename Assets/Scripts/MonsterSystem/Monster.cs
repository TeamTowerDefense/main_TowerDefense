using System;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    private Animator anim;

    [SerializeField]
    private int hp = 100;

    public float speed = 1f;
    private List<Transform> movePath;
    private int currentPathIndex = 1;
    private Vector3 pathOffset;

    public float cachedSpeedMultiplier = 1.0f;

    public Vector2Int CurrentGridPos { get; private set; }
    // �ý��۰� ���յ��� ���߱� ���� 
    public event Action<Monster> OnMonsterDie;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Initialize(List<Transform> path, float spawnY)
    {
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
        if (movePath == null || currentPathIndex >= movePath.Count) return;

        // 1. ����� ���ؼ� ���
        Transform targetTile = movePath[currentPathIndex];
        Vector3 startPos = movePath[currentPathIndex - 1].position;
        Vector3 lineDir = (targetTile.position - startPos).normalized;
        lineDir.y = 0;

        // 2. ���Ͱ� �߽ɼ����� �󸶳� ������ �ִ��� ���
        Vector3 toMonster = transform.position - startPos;
        toMonster.y = 0;
        float projection = Vector3.Dot(toMonster, lineDir);
        Vector3 centerPointOnLine = startPos + (lineDir * projection);
        float distFromCenter = Vector3.Distance(transform.position, centerPointOnLine);

        // 3. �� �ٽ�: ������ ���� (Soft Boundary Force)
        // ���(pathWidth)�� ����� ����, �Ÿ��� ����Ͽ� ������ ������ �������� �Ӵϴ�.
        Vector3 boundaryForce = Vector3.zero;
        if (distFromCenter > pathWidth)
        {
            // (�Ÿ��� * ����)��ŭ�� ���� ����. �Ÿ��� �־������� ���� Ŀ�� (������ ȿ��)
            float forceMagnitude = (distFromCenter - pathWidth) * containmentStrength * 5f;
            boundaryForce = (centerPointOnLine - transform.position).normalized * forceMagnitude;
        }

        // 4. ���� �� �ջ� (Steering Behaviors)
        // �̵� ���� + �о�� ��(Separation) + ��� ���� ��(BoundaryForce)
        Vector3 toTarget = (targetTile.position + pathOffset) - transform.position;
        toTarget.y = 0;

        // �ε巯�� ȸ���� ���� ���� ���͵��� ��Ĩ�ϴ�.
        Vector3 moveDir = toTarget.normalized;
        Vector3 finalDir = (moveDir + separationForce + boundaryForce).normalized;

        // 5. ���� �̵� (������ ��ġ ���� ���� ����, �ε巯�� ���ӵ� �̵�)
        transform.position += finalDir * speed * speedMultiplier * deltaTime;

        // ȸ�� ����
        if (finalDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalDir), 10f * deltaTime);

        // ������ ���� üũ
        if (toTarget.magnitude < 0.5f) currentPathIndex++;
    }

    public bool IsReachedEnd() => movePath == null || currentPathIndex >= movePath.Count;


    /// <summary>
    /// ������ HP�� 0���Ϸ� �������� ���� �� �Լ�
    /// </summary>
    private void Die()
    {
        OnMonsterDie?.Invoke(this); // ���� ���
        gameObject.SetActive(false);
    }
    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
        }
    }
}