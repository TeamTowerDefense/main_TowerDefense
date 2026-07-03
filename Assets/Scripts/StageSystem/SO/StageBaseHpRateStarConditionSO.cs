using IGameFlowInterface;
using UnityEngine;

[CreateAssetMenu(fileName = "StarCondition_BaseHpRate", menuName = "Game/Stage Star Condition/Base HP Rate")]
public class StageBaseHpRateStarConditionSO : StageStarConditionSO
{
    #region 인스펙터

    [Header("조건")]
    [Range(0f, 1f)]
    [SerializeField] float requiredRate = 0.5f;

    #endregion

    #region 평가

    public override bool Evaluate(StageDataSO stageData, StageResultContext result)
    {
        return result.Cleared && result.BaseHpRate >= requiredRate;
    }

    #endregion
}