using UnityEngine;

public enum DebuffType
{
    Slow,
    DefenseDown,
    Stun,
    Burn,
    Poison
}

[System.Serializable]
public class DebuffEffectData
{
    public DebuffType debuffType;

    // 디버프 지속시간
    public float duration = 2f;
    // 디버프 수치
    public float value = 0.2f;
    
    // 스택 수치
    public float stackAmount = 1;
    // 최대 스택
    public float maxStack = 1;

    public bool refreshDuration = true;
}
