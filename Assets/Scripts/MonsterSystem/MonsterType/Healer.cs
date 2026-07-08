using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class Healer : MonoBehaviour, IAbility
{
    [SerializeField] private float healRange = 2.0f;
    [SerializeField] private int healAmount = 50;
    [SerializeField] private float healCooldown = 0.5f;
    [SerializeField] private LayerMask allyLayer; // 아군 레이어 선택
    private float timer;
    [SerializeField] private int effectID;
    [SerializeField] private Transform parent;

    public void DisableAbility()
    {
        enabled = false;
    }
    public void EnableAbility(AbilityData data)
    {
        // 여기서 데이터 타입 캐스팅(확인)을 합니다.
        if (data is HealAbilityData healData)
        {
            healAmount = healData.healAmount;
            healRange = healData.healRange;
            healCooldown = healData.healCooldown;
            enabled = true;
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
        bool canHealAny = false;
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
            canHealAny = true;
            // 여기까지 왔다면 힐이 되어야 함
            target.TakeHeal(healAmount);
        }
        if (canHealAny)
        {
            GameObject effectPF = ObjectPoolManager.Instance.GetMonsterEffect(effectID);
  
                if (effectPF != null)
                {
                    Quaternion quaternion = Quaternion.LookRotation(Vector3.up);
                    Vector3 transformPosition = transform.position + Vector3.up * 0.1f; // 이펙트 위치를 약간 위로 올림
                    ObjectPoolManager.Instance.Spawn<PoolableObject>(
                        effectPF,
                        transformPosition,
                        quaternion,
                        parent
                    );
                    Debug.Log("몬스터 힐 이펙트 풀링 스폰 완료!");
                }
            if (effectPF != null)
            {
                // healRange는 '반지름'이기 때문에, 전체 직경(지름)인 healRange * 2를 곱해줍니다.
                // (기본 프리팹의 크기가 1도형(지름1) 기준일 때 정확히 일치합니다)
                float targetScale = healRange * 2f;
                effectPF.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
            }
        }
    }

    // [덤] 힐 범위 눈으로 보기 (에디터 전용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, healRange);
    }

}