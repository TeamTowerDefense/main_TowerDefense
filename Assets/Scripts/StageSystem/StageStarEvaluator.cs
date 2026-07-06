using IGameFlowInterface;
using UnityEngine;

public static class StageStarEvaluator
{
    public static int EvaluateStarMask(StageDataSO stageData, StageResultContext result)
    {
        if (HasCustomConditions(stageData))
            return EvaluateCustomStarConditions(stageData, result);

        return EvaluateDefaultStarConditions(result);
    }

    public static int GetMaxStarCount(StageDataSO stageData)
    {
        if (HasCustomConditions(stageData))
            return Mathf.Min(stageData.StarConditions.Count, 32);

        return 3;
    }

    public static int CountStars(int mask)
    {
        int count = 0;

        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) count++;

        return count;
    }

    public static bool HasCustomConditions(StageDataSO stageData)
    {
        return stageData != null &&
               stageData.StarConditions != null &&
               stageData.StarConditions.Count > 0;
    }

    static int EvaluateCustomStarConditions(StageDataSO stageData, StageResultContext result)
    {
        int mask = 0;
        int count = Mathf.Min(stageData.StarConditions.Count, 32);

        for (int i = 0; i < count; i++)
        {
            StageStarConditionSO condition = stageData.StarConditions[i];

            if (condition == null) continue;
            if (!condition.Evaluate(stageData, result)) continue;

            mask |= 1 << i;
        }

        return mask;
    }

    static int EvaluateDefaultStarConditions(StageResultContext result)
    {
        int mask = 0;

        if (result.Cleared)
            mask |= 1 << 0;

        if (result.Cleared && result.BaseHpRate >= 0.5f)
            mask |= 1 << 1;

        if (result.Cleared && result.BaseHpRate >= 1f)
            mask |= 1 << 2;

        return mask;
    }
}