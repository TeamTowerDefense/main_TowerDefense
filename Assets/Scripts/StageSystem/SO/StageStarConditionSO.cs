using IGameFlowInterface;
using UnityEngine;

public abstract class StageStarConditionSO : ScriptableObject
{
    [Header("표시 정보")]
    [SerializeField] string displayName;
    [TextArea]
    [SerializeField] string description;

    public string DisplayName => displayName;
    public string Description => description;

    public abstract bool Evaluate(StageDataSO stageData, StageResultContext result);

}