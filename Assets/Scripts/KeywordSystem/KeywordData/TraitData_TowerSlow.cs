using UnityEngine;

[CreateAssetMenu(menuName = "Keyword/Trait/TowerSlowAttack")]
public class TraitData_TowerSlow : KeywordData
{
    [Header("¹¯Èú µð¹öÇÁ ¿øº» µ¥ÀÌÅÍ")]
    public KeywordData slowDebuffData; 

    public override KeywordEffectBase CreateEffect() => new TraitEffect_TowerSlow();
}
