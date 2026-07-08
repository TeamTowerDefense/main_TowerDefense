using UnityEngine;

public enum AbilityType
{
    Heal,
    Regen,
    Shield,
}
public abstract class AbilityData : ScriptableObject
{
    public abstract AbilityType Type { get; }
}