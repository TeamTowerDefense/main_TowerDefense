using UnityEngine;

public class KeywordEffectBase
{
    public KeywordData DataOrigin { get; private set; }
    protected MonoBehaviour owner;

    public virtual void Initialize(KeywordData data, MonoBehaviour owner)
    {
        this.DataOrigin = data;
        this.owner = owner;
    }

    public virtual void OnApply() { }
    public virtual void OnRemove() { }
    public virtual void OnRefresh() { }
}
