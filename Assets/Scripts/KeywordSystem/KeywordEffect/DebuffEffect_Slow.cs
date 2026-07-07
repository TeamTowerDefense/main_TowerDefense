using UnityEngine;

public class DebuffEffect_Slow : KeywordEffectBase, IStatModifier, IUpdateModifier
{
    private float timer;

    public StatType TargetStat => StatType.MoveSpeed;
    public StatModType ModType => StatModType.PercentAdd;
    public float Value => (DataOrigin as DebuffData_Slow).speedDecreaseRatio;

    public override void Initialize(KeywordData data, MonoBehaviour owner)
    {
        base.Initialize(data, owner);
        timer = (data as DebuffData_Slow).duration;
    }

    public void OnUpdate(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0)
        {
            // 수명이 다하면 스스로를 삭제
            owner.GetComponent<KeywordController>().RemoveKeyword(this);
        }
    }

    public override void OnApply()
    {
        Debug.Log($"[디버프] {owner.name}에게 슬로우 디버프가 적용되었습니다. (속도 {Value * 100}%)");
        // TODO: 몬스터 렌더러 컬러를 푸른색으로 변경하거나 파란 이펙트 부착
    }

    public override void OnRemove()
    {
        Debug.Log($"[디버프 해제] {owner.name}의 슬로우 디버프가 해제되었습니다.");
        // TODO: 몬스터 원래 색상으로 복구
    }

    public override void OnRefresh()
    {
        base.OnRefresh();

        timer = (DataOrigin as DebuffData_Slow).duration;
    }
}
