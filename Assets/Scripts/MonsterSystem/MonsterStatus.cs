using System.Collections;
using UnityEngine;

public class MonsterStatus : MonoBehaviour
{
    // 외부에서 가져다 쓸 변수
    public bool IsStunned { get; private set; }
    public float SlowMultiplier { get; private set; } = 1.0f; // 1이면 기본 속도, 0.5면 50% 슬로우

    [Header("스턴 세팅")]
    public float StunGauge { get; private set; }     // 스턴이 발동되는 기준점
    public float CurrentStunStack { get; private set; }  // 현재 스턴 스택

    private Coroutine stunRoutine;
    private Coroutine slowRoutine;

    public void Setup(float stunGauge)
    {
        StunGauge = stunGauge;
        CurrentStunStack = 0f;
    }


    // 타워가 호출할 함수 
    public void AddStunStack(float amount, float duration)
    {
        if (IsStunned) return; // 이미 스턴 중이면 스택 무시

        CurrentStunStack += amount;

        // 스택이 찼는지 확인
        if (CurrentStunStack >= StunGauge)
        {
            ApplyStun(duration);
        }
    }

    private void ApplyStun(float duration)
    {
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (multiplier < SlowMultiplier)
        {
            if (slowRoutine != null) StopCoroutine(slowRoutine);
            slowRoutine = StartCoroutine(SlowRoutine(multiplier, duration));
        }
    }

    private IEnumerator StunRoutine(float duration)
    {
        IsStunned = true;
        yield return new WaitForSeconds(duration);
        CurrentStunStack = 0f;
        IsStunned = false;
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        SlowMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        SlowMultiplier = 1.0f;
    }
}