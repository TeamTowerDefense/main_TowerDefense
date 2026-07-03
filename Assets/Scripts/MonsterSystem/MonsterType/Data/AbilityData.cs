using UnityEngine;

public enum AbilityType
{
    Heal,
}
public abstract class AbilityData : ScriptableObject
{
    public abstract AbilityType Type { get; }
}