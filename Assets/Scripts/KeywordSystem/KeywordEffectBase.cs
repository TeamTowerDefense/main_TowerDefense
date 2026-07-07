using UnityEngine;

public class KeywordEffectBase
{
    public KeywordData DataOrigin { get; private set; }
    protected MonoBehaviour owner;

    protected PoolEffect activeVisualEffect;

    public virtual void Initialize(KeywordData data, MonoBehaviour owner)
    {
        this.DataOrigin = data;
        this.owner = owner;
    }

    public virtual void OnApply() 
    {
        if (DataOrigin.visualEffectPrefab != null)
        {
            // ObjectPoolManager의 Spawn 방식을 사용하여 프리팹 기반으로 스폰합니다.
            activeVisualEffect = ObjectPoolManager.Instance.Spawn<PoolEffect>(
                DataOrigin.visualEffectPrefab,
                owner.transform.position,      
                Quaternion.identity,
                owner.transform              
            );

            if (activeVisualEffect != null)
            {
                // 디버프 이펙트는 파티클 수명이 아니라, 디버프 지속시간에 맞춰 수동으로 꺼야 하기 때문입니다.
                activeVisualEffect.StopAllCoroutines();
                // 이펙트 레이어 및 내부 파티클들 재생
                activeVisualEffect.Play();
            }
        }
    }
    public virtual void OnRemove() 
    {
        if (activeVisualEffect != null)
        {
            activeVisualEffect.transform.SetParent(ObjectPoolManager.Instance.GetEffectParent());

            ObjectPoolManager.Instance.Despawn(activeVisualEffect);
            activeVisualEffect = null;
        }
    }
    public virtual void OnRefresh() 
    {
        if (activeVisualEffect != null)
        {
            activeVisualEffect.Play();
        }
    }
}
