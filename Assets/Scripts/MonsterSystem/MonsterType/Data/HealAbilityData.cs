using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Abilities/Heal")]
public class HealAbilityData : AbilityData
{
    public override AbilityType Type => AbilityType.Heal;

    public int healAmount;
    public float healRange;
    public float healCooldown;
}