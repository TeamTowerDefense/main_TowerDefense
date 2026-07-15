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

    [Header("로비 표시")]
    public int LobbyOrder;
    public Sprite MapPreview;

    [Header("인게임 설정")]
    public int StartResource = 1000;
    public int TowerLimit = 10;
    public int BaseHp = 20;
    public List<StageWaveEntry> Waves = new();
    public List<string> NextStageIds = new();

    [Header("별 조건")]
    public List<StageStarConditionSO> StarConditions = new();

    public List<StageWavePreviewInfo> GetWavePreviewInfos()
    {
        List<StageWavePreviewInfo> results = new();

        if (Waves == null) return results;

        for (int i = 0; i < Waves.Count; i++)
        {
            StageWaveEntry wave = Waves[i];
            MonsterSpawnDataSO spawnData = wave?.SpawnData;

            results.Add(new StageWavePreviewInfo
            {
                WaveNumber = i + 1,
                DisplayName = GetWaveDisplayName(spawnData, i),
                Monsters = spawnData != null
                    ? spawnData.GetMonsterPreviewInfos()
                    : new List<MonsterSpawnPreviewInfo>()
            });
        }

        return results;
    }

    static string GetWaveDisplayName(MonsterSpawnDataSO spawnData, int waveIndex)
    {
        if (spawnData != null && !string.IsNullOrWhiteSpace(spawnData.WaveName))
            return spawnData.WaveName;

        return $"웨이브 {waveIndex + 1}";
    }
}

[Serializable]
public class StageWaveEntry
{
    public MonsterSpawnDataSO SpawnData;
    public float PrepareTime = 5f;
    public bool CanSkipPrepare = true;
    public bool AllowBuildDuringWave;
}

public class StageWavePreviewInfo
{
    public int WaveNumber;
    public string DisplayName;
    public List<MonsterSpawnPreviewInfo> Monsters = new();
}