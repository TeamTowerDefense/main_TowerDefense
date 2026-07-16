using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyStageWaveView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text waveTitleText;
    [SerializeField] Transform monsterContent;
    [SerializeField] LobbyMonsterPreviewView monsterPrefab;
    [SerializeField] GameObject emptyRoot;

    readonly List<LobbyMonsterPreviewView> monsterViews = new();

    public void Bind(StageWavePreviewInfo info)
    {
        Clear();

        if (info == null)
        {
            if (emptyRoot != null) emptyRoot.SetActive(true);
            return;
        }

        if (waveTitleText != null)
            waveTitleText.text = string.IsNullOrWhiteSpace(info.DisplayName)
                ? $"웨이브 {info.WaveNumber}" : info.DisplayName;

        bool hasMonsters = info.Monsters != null && info.Monsters.Count > 0;

        if (emptyRoot != null) emptyRoot.SetActive(!hasMonsters);

        if (!hasMonsters || monsterPrefab == null || monsterContent == null) return;

        foreach (MonsterSpawnPreviewInfo monsterInfo in info.Monsters)
        {
            if (monsterInfo == null || monsterInfo.MonsterData == null) continue;

            LobbyMonsterPreviewView view = Instantiate(monsterPrefab, monsterContent);
            view.Bind(monsterInfo);

            monsterViews.Add(view);
        }
    }

    public void Clear()
    {
        foreach (LobbyMonsterPreviewView view in monsterViews)
        {
            if (view != null) Destroy(view.gameObject);
        }

        monsterViews.Clear();

        if (waveTitleText != null) waveTitleText.text = string.Empty;
    }
}
