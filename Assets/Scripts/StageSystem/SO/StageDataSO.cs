using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDataSO", menuName = "Scriptable Objects/StageDataSO")]
public class StageDataSO : ScriptableObject
{
    [Header("스테이지 씬 설정")]
    public string StageId;
    public string DisplayName;

    [TextArea]
    public string Description;

    public Sprite Icon;

    [Header("인게임 설정")]
    public int StartResource = 1000;
    public int TowerLimit = 10;
    public int BaseHp = 20;
    public List<StageWaveEntry> Waves;
    public List<string> NextStageIds;

    [Header("별 조건")]
    public List<StageStarConditionSO> StarConditions = new();
}

[Serializable]
public class StageWaveEntry
{
    public string DisplayName;
    public MonsterSpawnDataSO SpawnData;

    public float PrepareTime = 5f;
    public bool CanSkipPrepare = true;
    public bool AllowBuildDuringWave = false;
}