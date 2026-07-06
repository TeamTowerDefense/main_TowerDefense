using UnityEngine;

[CreateAssetMenu(menuName = "Keyword/Debuff/Slow")]
public class DebuffData_Slow : KeywordData
{
    public float speedDecreaseRatio = -0.3f; 
    public float duration = 3.0f;
    public override KeywordEffectBase CreateEffect() => new DebuffEffect_Slow();
}
