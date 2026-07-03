using UnityEngine;

public class KeywordEffectBase
{   
    // 키워드 데이터
    public KeywordData DataOrigin { get; private set; }

    // 키워드가 부착된 개체
    protected MonoBehaviour owner;

    //초기화 함수
    public virtual void Initialize(KeywordData data, MonoBehaviour owner)
    {
        this.DataOrigin = data;
        this.owner = owner;
    }

    // 효과 적용 / 해제
    public virtual void OnApply() { }
    public virtual void OnRemove() { }
}
