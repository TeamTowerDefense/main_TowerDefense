using UnityEngine;

public enum StatModType 
{ 
    Flat, 
    PercentAdd 
}

public interface IStatModifier
{
    StatType TargetStat { get; }
    StatModType ModType { get; }
    float Value { get; }
}
