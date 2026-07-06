using UnityEngine;

public class TraitEffect_Berserker: KeywordEffectBase, IStatModifier
{
    public StatType TargetStat => StatType.MoveSpeed;
    public StatModType ModType => StatModType.Flat;
    public float Value => (DataOrigin as KWData_Berserker).adBonusRatio;
}
