using IGameInterface;
using UnityEngine;

[CreateAssetMenu(menuName = "Keyword/Trait/TowerTargetingModifier")]
public class TraitData_TowerTargeting : KeywordData
{
    [Header("타겟팅 설정")]
    [Tooltip("이 키워드가 장착되면 아래의 타겟팅 방식으로 강제 변경됩니다.")]
    public EnemyTargetMode overrideTargetMode;

    public override KeywordEffectBase CreateEffect() => new TraitEffect_TowerTargeting();
}
