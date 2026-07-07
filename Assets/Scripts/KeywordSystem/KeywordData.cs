using UnityEngine;

public enum KeywordType
{
    Trait, 
    Buff, 
    Debuff
}

public enum KeywordStackBehavior
{
    Stack,          
    RefreshDuration,
}

public abstract class KeywordData : ScriptableObject
{
    [Header("기본 정보")]
    public string keywordName;
    public string displayName;
    public KeywordType type;
    [TextArea] public string description;
    [Header("중첩 설정")]
    [Tooltip("체크 시 중첩 가능(시간도 갱신됨). 해제 시 시간만 갱신됨.")]
    public bool isStackable = false;
    [Tooltip("isStackable이 true일 때 최대 중첩 횟수 (0=무한)")]
    public int maxStack = 1;
    [Header("비주얼 이펙트 (선택)")]
    [Tooltip("키워드가 적용되는 동안 몬스터 몸에 붙어있을 이펙트 프리팹")]
    public GameObject visualEffectPrefab;


    public abstract KeywordEffectBase CreateEffect();
}
