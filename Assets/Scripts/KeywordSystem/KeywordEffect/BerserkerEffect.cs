using UnityEngine;

public class BerserkerEffect: KeywordEffectBase, IADModifier
{
    private KWData_Berserker berserkerData;

    public override void Initialize(KeywordData data, MonoBehaviour owner)
    {
        base.Initialize(data, owner);

        berserkerData = data as KWData_Berserker;
    }

    public int ModifyAD(int baseAD)
    {
        if (berserkerData == null) return baseAD;

        return Mathf.RoundToInt(baseAD * (1f + berserkerData.adBonusRatio));
    }

    public override void OnApply()
    {
        Debug.Log($"{owner.name}가 광전사 모드에 돌입하여 공격력이 상승합니다!");
    }
}
