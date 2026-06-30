using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDataSO", menuName = "Scriptable Objects/StageDataSO")]
public class StageDataSO : ScriptableObject
{
    public int TowerLimit = 10;
    public int BaseHp = 20;
    public List<StageWaveEntry> Waves;
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