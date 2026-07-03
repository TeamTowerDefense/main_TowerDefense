using System.Collections.Generic;
using UnityEngine;

public class KeywordController : MonoBehaviour
{
    // 현재 오브젝트에 활성화된 키워드 리스트
    private List<KeywordEffectBase> activeKeywords = new List<KeywordEffectBase>();

    /// <summary>
    /// 키워드 추가 함수
    /// </summary>
    public void AddKeyword(KeywordData data)
    {
        // 원래는 팩토리(Factory) 패턴을 써서 data 타입에 맞는 클래스를 
        // 동적 생성해야 하지만, 이해를 돕기 위해 임시 서술합니다.
        KeywordEffectBase effect = CreateEffectInstance(data);

        if (effect != null)
        {
            effect.Initialize(data, this.GetComponent<MonoBehaviour>());
            activeKeywords.Add(effect);
            effect.OnApply();
            Debug.Log($"{gameObject.name}에 [{data.keywordName}] 키워드가 장착되었습니다.");
        }
    }

    /// <summary>
    /// 키워드 삭제 함수
    /// </summary>
    public void RemoveKeyword(KeywordEffectBase effect)
    {
        if (activeKeywords.Contains(effect))
        {
            effect.OnRemove();
            activeKeywords.Remove(effect);
            Debug.Log($"{gameObject.name}에서 키워드가 제거되었습니다.");
        }
    }

    /// <summary>
    /// 특정 인터페이스를 가진 키워드들만 골라서 리턴
    /// </summary>
    public List<T> GetKeywords<T>() where T : class
    {
        List<T> results = new List<T>();
        foreach (var kw in activeKeywords)
        {
            if (kw is T targetInterface)
            {
                results.Add(targetInterface);
            }
        }
        return results;
    }

    // 임시 팩토리 함수
    private KeywordEffectBase CreateEffectInstance(KeywordData data)
    {
        if (data is KWData_Berserker)
        {
            return new BerserkerEffect();
        }

        // 나중에 언데드, 상처 같은 키워드를 만들면 여기에 한 줄씩 추가하시면 됩니다.
        // else if (data is UndeadData) return new UndeadEffect();
        // else if (data is WoundData) return new WoundEffect();

        return null; // 매칭되는 게 없으면 null 반환[cite: 3]
    }
}
