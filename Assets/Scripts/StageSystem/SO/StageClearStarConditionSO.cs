using IGameFlowInterface;
using UnityEngine;

[CreateAssetMenu(fileName = "StarCondition_Clear", menuName = "Game/Stage Star Condition/Clear")]
public class StageClearStarConditionSO : StageStarConditionSO
{
    public override bool Evaluate(StageDataSO stageData, StageResultContext result)
    {
        return result.Cleared;
    }
}