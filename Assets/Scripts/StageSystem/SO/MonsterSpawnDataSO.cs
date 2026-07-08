using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSpawnDataSO", menuName = "Scriptable Objects/MonsterSpawnDataSO")]
public class MonsterSpawnDataSO : ScriptableObject
{
    public string WaveName;
    public List<MonsterSpawnGroup> SpawnGroups;
    public int Reward;

    public Queue<MonsterSpawnGroup> CreateQueue()
    {
        if (SpawnGroups == null || SpawnGroups.Count == 0) return new Queue<MonsterSpawnGroup>();
        return new Queue<MonsterSpawnGroup>(SpawnGroups);
    }

    public bool IsEmpty => SpawnGroups == null || SpawnGroups.Count <= 0;
}

[Serializable]
public class MonsterSpawnElement
{
    public MonsterData MonsterData;
    public int Count = 1;
}

[Serializable]
public class MonsterSpawnGroup
{
    public List<MonsterSpawnElement> Elements;
    public float elementInterval = 0;
    public float Interval = 1f;
    public float StartDelay = 0f;
}