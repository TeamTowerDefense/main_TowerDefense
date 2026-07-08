using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Abilities/Heal")]
public class HealAbilityData : AbilityData
{
    public override AbilityType Type => AbilityType.Heal;

    public int healAmount; // 한 번에 회복할 량
    public float healRange; // 회복 범위 (치유할 수 있는 최대 거리)
    public float healCooldown; // 회복 스킬의 쿨타임 (몇 초마다 회복할 수 있는지)
}