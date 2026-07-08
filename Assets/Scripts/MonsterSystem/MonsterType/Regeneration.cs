using UnityEngine;

public class Regeneration : MonoBehaviour, IAbility
{
    [SerializeField] private int regenAmount = 20;
    [SerializeField] private float regenInterval = 1.0f;
    private float timer;

    private Monster selfMonster; // 자기 자신의 Monster 컴포넌트

    private void Awake()
    {
        // 자기 자신의 Monster 컴포넌트를 미리 캐싱해둡니다.
        selfMonster = GetComponent<Monster>();
    }
        
    public void DisableAbility()
    {
        enabled = false;
    }

    public void EnableAbility(AbilityData data)
    {
        // 재생 데이터 타입으로 안전하게 캐스팅합니다.
        if (data is RegenAbilityData regenData)
        {
            regenAmount = regenData.regenAmount;
            regenInterval = regenData.regenInterval;
            enabled = true;
        }
    }

    void Update()
    {
        // 몬스터가 죽어있다면 재생 타이머를 굴리지 않습니다.
        if (selfMonster != null && selfMonster.isDead) return;

        timer += Time.deltaTime;
        if (timer >= regenInterval)
        {
            PerformRegen();
            timer = 0;
        }
    }

    void PerformRegen()
    {
        if (selfMonster == null)
        {
            Debug.LogError($"{gameObject.name}: Monster 컴포넌트를 찾을 수 없어 재생 불가!");
            return;
        }

        // 이미 체력이 가득 차 있다면 재생 건너뛰기
        if (selfMonster.currentHp >= selfMonster.maxHp)
        {
            Debug.Log($"{gameObject.name}: 이미 체력이 가득 차서 재생 스킵 (HP: {selfMonster.currentHp}/{selfMonster.maxHp})");
            return;
        }

        Debug.Log($"{gameObject.name}: 초당 자체 재생 발동! (+{regenAmount})");

        // 자신에게 힐 적용
        selfMonster.TakeHeal(regenAmount);
    }
}