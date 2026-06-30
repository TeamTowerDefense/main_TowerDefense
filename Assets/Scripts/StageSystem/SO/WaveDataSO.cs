using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSpawnDataSO", menuName = "Scriptable Objects/MonsterSpawnDataSO")]
public class MonsterSpawnDataSO : ScriptableObject
{
    public string WaveName;
    public List<MonsterSpawnGroup> waveList;
    public int reward;

    public Queue<MonsterSpawnGroup> waveQueue
    {
        get
        {
            if (waveList == null || waveList.Count == 0) return null;
            return new Queue<MonsterSpawnGroup>(waveList);
        }
    }
}

[Serializable]
public class MonsterSpawnGroup
{
    public MonsterDataSO MonsterData;
    public int Count;
    public float interval;
}
