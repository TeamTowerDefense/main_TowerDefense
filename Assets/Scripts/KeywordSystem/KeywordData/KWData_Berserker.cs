using UnityEngine;

[CreateAssetMenu(menuName = "Keyword/Buff/Berserker")]
public class KWData_Berserker : KeywordData
{
    public float adBonusRatio = 0.2f;
    public override KeywordEffectBase CreateEffect() => new TraitEffect_Berserker();
}