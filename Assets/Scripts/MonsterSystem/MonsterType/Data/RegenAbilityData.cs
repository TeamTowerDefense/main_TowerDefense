using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Abilities/Regen")]
public class RegenAbilityData : AbilityData
{
    public override AbilityType Type => AbilityType.Regen;

    public int regenAmount = 20;       // 한 번에 회복할 량
    public float regenInterval = 1.0f;  // 재생 주기 (몇 초마다 회복할 것인가)
}