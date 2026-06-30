using IGameInterface;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDataSO", menuName = "Scriptable Objects/StageDataSO")]
public class StageDataSO : ScriptableObject
{
    public List<WaveData> waveData;
    public int TowerLimit;
    public int BaseHp;
}

[Serializable]
public class WaveData
{
    public string WaveName;
    public List<MonsterSpawnGroup> spawnGroups;
    public int Reward;
}