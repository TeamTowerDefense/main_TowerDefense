using UnityEngine;
using IGameFlowInterface;

public class StageRunContextService : GlobalServiceBase, IStageRunContextService
{
    StageDataSO currentStageData;
    StageLoadoutContext loadout = new();

    public bool HasValidRun => currentStageData != null && !string.IsNullOrWhiteSpace(BattleSceneName);

    public string StageId => currentStageData ? currentStageData.StageId : string.Empty;
    public string BattleSceneName => StageId;
    public StageDataSO StageData => currentStageData;
    public StageLoadoutContext Loadout => loadout;

    public void SetLoadout(StageLoadoutContext loadout)
    {
        this.loadout = loadout == null 
            ? new StageLoadoutContext() 
            : new StageLoadoutContext(loadout.SelectedTowerIds);
    }

    public void SetStage(StageDataSO stageData)
    {
        if (stageData == null || currentStageData == stageData) return;
        currentStageData = stageData;
    }
    public void Clear()
    {
        currentStageData = null;
        loadout.Clear();
    }
}
