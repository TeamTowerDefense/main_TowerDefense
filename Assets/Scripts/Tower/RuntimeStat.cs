using System.Collections.Generic;
using UnityEngine;

public class RuntimeStat
{
    private float baseValue;
    public float CurrentValue { get; private set; }

    public RuntimeStat(float baseValue)
    {
        this.baseValue = baseValue;
        this.CurrentValue = baseValue;
    }

    public void RecalculateStat(List<IStatModifier> modifiers)
    {
        float flatSum = 0f;
        float percentSum = 0f;

        foreach (var mod in modifiers)
        {
            if (mod.ModType == StatModType.Flat) flatSum += mod.Value;
            else if (mod.ModType == StatModType.PercentAdd) percentSum += mod.Value;
        }

        CurrentValue = (baseValue + flatSum) * (1f + percentSum);

    }
}
