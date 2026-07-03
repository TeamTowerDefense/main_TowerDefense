using UnityEngine;

public abstract class KeywordData : ScriptableObject
{
    [Header("기본 키워드 정보")]
    public string keywordName;      
    [TextArea]
    public string description;      
    public Sprite keywordIcon;      
}
