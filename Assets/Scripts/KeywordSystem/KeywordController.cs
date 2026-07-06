using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KeywordController : MonoBehaviour
{
    private List<KeywordEffectBase> activeKeywords = new List<KeywordEffectBase>();

    // 스탯 갱신을 타워/몬스터에게 알리는 방송
    public event Action OnKeywordChanged;

    private void Update()
    {
        // IUpdateModifier 권한이 있는 키워드(디버프 등)에게 매 프레임 시간을 전달
        for (int i = activeKeywords.Count - 1; i >= 0; i--)
        {
            if (activeKeywords[i] is IUpdateModifier updateMod)
            {
                updateMod.OnUpdate(Time.deltaTime);
            }
        }
    }

    public void ClearAllKeywords()
    {
        foreach (var kw in activeKeywords)
        {
            kw.OnRemove(); // 이펙트 꺼주기
        }
        activeKeywords.Clear();
    }

    public void AddKeyword(KeywordData data)
    {
        var existingStacks = activeKeywords.Where(k => k.DataOrigin == data).ToList();

        if (existingStacks.Count > 0)
        {
            // 중첩 불가능하거나, 이미 최대 중첩이라면? -> 시간만 리셋!
            if (!data.isStackable || (data.maxStack > 0 && existingStacks.Count >= data.maxStack))
            {
                foreach (var stack in existingStacks)
                {
                    stack.OnRefresh();
                }
                return; // 입구 컷
            }
        }

        KeywordEffectBase effect = data.CreateEffect();
        if (effect == null)
            return;

        if (effect is ITargetingModifier)
        {
            var existingTargetingMods = GetKeywords<ITargetingModifier>();

            foreach (var mod in existingTargetingMods)
            {
                RemoveKeyword(mod as KeywordEffectBase);
            }
        }

        effect.Initialize(data, this);
        activeKeywords.Add(effect);
        effect.OnApply();

        OnKeywordChanged?.Invoke(); // 스탯 재계산 지시
    }

    public void RemoveKeyword(KeywordEffectBase effect)
    {
        if (activeKeywords.Contains(effect))
        {
            effect.OnRemove();
            activeKeywords.Remove(effect);

            OnKeywordChanged?.Invoke(); // 스탯 재계산 지시
        }
    }

    // 특정 인터페이스(권한)를 가진 키워드들만 쏙쏙 뽑아주는 헬퍼 함수
    public List<T> GetKeywords<T>() where T : class
    {
        List<T> results = new List<T>();
        foreach (var kw in activeKeywords)
        {
            if (kw is T target) results.Add(target);
        }
        return results;
    }
}
