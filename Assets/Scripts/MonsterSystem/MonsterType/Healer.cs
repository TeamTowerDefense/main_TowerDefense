using UnityEngine;

public class Healer : MonoBehaviour, IAbility
{
    [SerializeField] private float healRange = 2.0f;
    [SerializeField] private int healAmount = 50;
    [SerializeField] private float healCooldown = 0.5f;
    [SerializeField] private LayerMask allyLayer; // 아군 레이어 선택
    private float timer;

    public void DisableAbility() => enabled = false;
    public void EnableAbility(AbilityData data)
    {
        // 여기서 데이터 타입 캐스팅(확인)을 합니다.
        if (data is HealAbilityData healData)
        {
            this.healAmount = healData.healAmount;
            this.healRange = healData.healRange;
            this.healCooldown = healData.healCooldown;
            enabled = true; // 켜기
        }
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= healCooldown)
        {
            PerformHeal();
            timer = 0;
        }
    }
    void PerformHeal()
    {
        Debug.Log($"{gameObject.name}: 힐 탐색 시작!"); // 1. 함수가 실행되는지 확인

        Collider[] colliders = Physics.OverlapSphere(transform.position, healRange, allyLayer);

        Debug.Log($"{gameObject.name}: 범위 내 감지된 물체 개수 = {colliders.Length}"); // 2. 감지된 게 있는지 확인

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            Monster target = col.GetComponent<Monster>();

            if (target == null)
            {
                Debug.Log($"감지된 물체 {col.name}에 Monster 컴포넌트가 없음!");
                continue;
            }

            if (target.isDead)
            {
                Debug.Log($"{target.name}은 죽어서 힐 불가.");
                continue;
            }

            if (target.currentHp >= target.maxHp)
            {
                Debug.Log($"{target.name}은 체력이 가득 차서 힐 불가 (HP: {target.currentHp}/{target.maxHp})");
                continue;
            }

            // 여기까지 왔다면 힐이 되어야 함
            Debug.Log($"{target.name}에게 힐 시전 성공!");
            target.TakeHeal(healAmount);
        }
    }

    // [덤] 힐 범위 눈으로 보기 (에디터 전용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, healRange);
    }
}