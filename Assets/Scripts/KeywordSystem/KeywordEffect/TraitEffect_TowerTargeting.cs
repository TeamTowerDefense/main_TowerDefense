using IGameInterface;
using UnityEngine;

public class TraitEffect_TowerTargeting : KeywordEffectBase, ITargetingModifier
{
    public EnemyTargetMode GetTargetMode()
    {
        return (DataOrigin as TraitData_TowerTargeting).overrideTargetMode;
    }
}
