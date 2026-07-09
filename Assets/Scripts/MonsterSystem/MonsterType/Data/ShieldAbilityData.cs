using UnityEngine;

[CreateAssetMenu(menuName = "Monster/Abilities/Shield")]
public class ShieldAbilityData : AbilityData
{
    public override AbilityType Type => AbilityType.Shield;

    public int shieldCount = 3; // 부여할 총 쉴드 개수 (스택)
}