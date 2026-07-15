using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;

public class LobbyStageInfoView : MonoBehaviour
{
    [Header("기본 정보")]
    [SerializeField] TMP_Text stageNameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image mapPreviewImage;
    [SerializeField] GameObject mapPreviewEmptyRoot;

    [Header("별")]
    [SerializeField] Image[] starImages;
    [SerializeField] Sprite filledStarSprite;
    [SerializeField] Sprite emptyStarSprite;

    [Header("웨이브")]
    [SerializeField] TMP_Text waveCountText;
    [SerializeField] Transform waveContent;
    [SerializeField] LobbyStageWaveView wavePrefab;
    [SerializeField] GameObject waveEmptyRoot;

    readonly List<LobbyStageWaveView> waveViews = new();

    public void Bind(StageDataSO stageData, int starMark)
    {
        ClearWaves();

        if (stageData == null)
        {
            Clear();
            return;
        }
        if (stageNameText != null) stageNameText.text = stageData.DisplayName;
        if (descriptionText != null) descriptionText.text = stageData.Description;
        if (mapPreviewImage != null)
        {
            mapPreviewImage.sprite = stageData.MapPreview;
            mapPreviewImage.enabled = stageData.MapPreview != null;
            mapPreviewImage.preserveAspect = true;
        }
        if (mapPreviewEmptyRoot != null)
            mapPreviewEmptyRoot.SetActive(stageData.MapPreview == null);

        RefreshStars(starMark);

        List<StageWavePreviewInfo> waves = stageData.GetWavePreviewInfos();
        bool hasWaves = waves != null && waves.Count > 0;

        if (waveCountText != null) waveCountText.text = hasWaves ? $"총 {waves.Count} 웨이브" : "웨이브 없음";
        if (waveEmptyRoot != null) waveEmptyRoot.SetActive(!hasWaves);
        if (!hasWaves || wavePrefab == null || waveContent == null) return;

        foreach(StageWavePreviewInfo waveInfo in waves)
        {
            LobbyStageWaveView view = Instantiate(wavePrefab, waveContent);
            view.Bind(waveInfo);

            waveViews.Add(view);
        }
    }
    public void Clear()
    {
        if (stageNameText != null) stageNameText.text = "스테이지를 선택하세요";
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (mapPreviewImage != null)
        {
            mapPreviewImage.sprite = null;
            mapPreviewImage.enabled = false;
        }
        if (mapPreviewEmptyRoot != null) mapPreviewEmptyRoot.SetActive(true);
        if (waveCountText != null) waveCountText.text = string.Empty;
        if (waveEmptyRoot != null) waveEmptyRoot.SetActive(true);

        RefreshStars(0);
        ClearWaves();
    }
    void RefreshStars(int starMark)
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            Image starImage = starImages[i];
            if (starImage == null) continue;

            bool earned = (starMark & (1 << i)) != 0;

            if (filledStarSprite != null && emptyStarSprite != null)
            {
                starImage.sprite = earned ? filledStarSprite : emptyStarSprite;
                starImage.color = Color.white;
                continue;
            }

            starImage.color = earned ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
    }
    void ClearWaves()
    {
        foreach (LobbyStageWaveView view in waveViews)
        {
            if (view != null) Destroy(view.gameObject);
        }

        waveViews.Clear();
    }
}
