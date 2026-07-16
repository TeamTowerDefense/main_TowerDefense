using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSpawnDataSO", menuName = "Scriptable Objects/MonsterSpawnDataSO")]
public class MonsterSpawnDataSO : ScriptableObject
{
    public string WaveName;
    public List<MonsterSpawnGroup> SpawnGroups = new();
    public int Reward;

    public bool IsEmpty => SpawnGroups == null || SpawnGroups.Count == 0;

    public Queue<MonsterSpawnGroup> CreateQueue()
    {
        if (IsEmpty) return new Queue<MonsterSpawnGroup>();
        return new Queue<MonsterSpawnGroup>(SpawnGroups);
    }

    public List<MonsterSpawnPreviewInfo> GetMonsterPreviewInfos()
    {
        List<MonsterSpawnPreviewInfo> results = new();
        Dictionary<MonsterData, MonsterSpawnPreviewInfo> lookup = new();

        if (SpawnGroups == null) return results;

        foreach (MonsterSpawnGroup group in SpawnGroups)
        {
            if (group?.Elements == null) continue;

            foreach (MonsterSpawnElement element in group.Elements)
            {
                if (element?.MonsterData == null || element.Count <= 0) continue;

                if (lookup.TryGetValue(element.MonsterData, out MonsterSpawnPreviewInfo info))
                {
                    info.Count += element.Count;
                    continue;
                }

                info = new MonsterSpawnPreviewInfo
                {
                    MonsterData = element.MonsterData,
                    Count = element.Count
                };

                lookup.Add(element.MonsterData, info);
                results.Add(info);
            }
        }

        return results;
    }
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
    public List<MonsterSpawnElement> Elements = new();
    public float elementInterval;
    public float Interval = 1f;
    public float StartDelay;
}

public class MonsterSpawnPreviewInfo
{
    public MonsterData MonsterData;
    public int Count;

    public string DisplayName => MonsterData != null ? MonsterData.monsterName : string.Empty;
    public Sprite Icon => MonsterData != null ? MonsterData.Icon : null;
}