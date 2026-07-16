using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyStageWaveView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text waveTitleText;
    [SerializeField] Transform monsterContent;
    [SerializeField] LobbyMonsterPreviewView monsterPrefab;
    [SerializeField] GameObject emptyRoot;

    [Header("동적 너비")]
    [SerializeField] RectTransform widthTarget;
    [SerializeField] RectTransform preferredWidthSource;
    [SerializeField] LayoutElement widthLayoutElement;
    [SerializeField, Min(0f)] float minWidth = 250f;
    [SerializeField, Min(0f)] float maxWidth = 850f;

    readonly List<LobbyMonsterPreviewView> monsterViews = new();

    Coroutine widthRoutine;

    void Awake()
    {
        if (widthTarget == null)
            widthTarget = transform as RectTransform;

        if (preferredWidthSource == null)
            preferredWidthSource = widthTarget;

        if (widthLayoutElement == null && widthTarget != null)
            widthLayoutElement = widthTarget.GetComponent<LayoutElement>();
    }

    public void Bind(StageWavePreviewInfo info)
    {
        Clear();

        if (info == null)
        {
            if (emptyRoot != null)
                emptyRoot.SetActive(true);

            RequestWidthRefresh();
            return;
        }

        if (waveTitleText != null)
        {
            waveTitleText.text = string.IsNullOrWhiteSpace(info.DisplayName)
                ? $"웨이브 {info.WaveNumber}"
                : info.DisplayName;
        }

        bool hasMonsters =
            info.Monsters != null &&
            info.Monsters.Count > 0;

        if (emptyRoot != null)
            emptyRoot.SetActive(!hasMonsters);

        if (hasMonsters && monsterPrefab != null && monsterContent != null)
        {
            foreach (MonsterSpawnPreviewInfo monsterInfo in info.Monsters)
            {
                if (monsterInfo?.MonsterData == null)
                    continue;

                LobbyMonsterPreviewView view =
                    Instantiate(monsterPrefab, monsterContent);

                view.Bind(monsterInfo);
                monsterViews.Add(view);
            }
        }

        RequestWidthRefresh();
    }

    public void Clear()
    {
        if (widthRoutine != null)
        {
            StopCoroutine(widthRoutine);
            widthRoutine = null;
        }

        foreach (LobbyMonsterPreviewView view in monsterViews)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }

        monsterViews.Clear();

        if (waveTitleText != null)
            waveTitleText.text = string.Empty;
    }

    void RequestWidthRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (widthRoutine != null)
            StopCoroutine(widthRoutine);

        widthRoutine = StartCoroutine(RefreshWidthRoutine());
    }

    IEnumerator RefreshWidthRoutine()
    {
        yield return null;

        if (widthTarget == null || preferredWidthSource == null)
        {
            widthRoutine = null;
            yield break;
        }


        if (widthLayoutElement != null)
        {
            widthLayoutElement.minWidth = -1f;
            widthLayoutElement.preferredWidth = -1f;
            widthLayoutElement.flexibleWidth = 0f;
        }

        Canvas.ForceUpdateCanvases();

        if (monsterContent is RectTransform monsterContentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(monsterContentRect);

        LayoutRebuilder.ForceRebuildLayoutImmediate(preferredWidthSource);

        float preferredWidth =
            LayoutUtility.GetPreferredWidth(preferredWidthSource);

        float targetWidth = Mathf.Clamp(
            preferredWidth,
            minWidth,
            maxWidth);

        if (widthLayoutElement != null)
        {
            widthLayoutElement.minWidth = targetWidth;
            widthLayoutElement.preferredWidth = targetWidth;
            widthLayoutElement.flexibleWidth = 0f;
        }
        else
        {
            widthTarget.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                targetWidth);
        }

        if (widthTarget.parent is RectTransform parentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        Canvas.ForceUpdateCanvases();
        widthRoutine = null;
    }
}