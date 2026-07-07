using UnityEngine;

public class TraitEffect_TowerSlow : KeywordEffectBase, IOnHitModifier
{
    private TraitData_TowerSlow towerSlowData;

    public override void Initialize(KeywordData data, MonoBehaviour owner)
    {
        base.Initialize(data, owner);
        towerSlowData = data as TraitData_TowerSlow;
    }

    public void OnHit(MonoBehaviour towerOwner, KeywordController targetController)
    {
        if (towerSlowData == null || towerSlowData.slowDebuffData == null) return;

        targetController.AddKeyword(towerSlowData.slowDebuffData);
    }
}
