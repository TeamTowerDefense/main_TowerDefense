using IGameFlowInterface;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectEntryView : MonoBehaviour
{
    #region 인스펙터

    [Header("스테이지 데이터")]
    [SerializeField] StageDataSO stageData;

    [Header("UI")]
    [SerializeField] Button button;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text lockText;
    [SerializeField] GameObject lockRoot;

    [Header("별")]
    [Tooltip("빈 별 오브젝트. 획득한 별은 앞에서부터 SetActive(false) 처리")]
    [SerializeField] GameObject[] emptyStars;

    #endregion

    #region 필드

    Action<StageDataSO> onClicked;

    #endregion

    #region 프로퍼티

    public StageDataSO StageData => stageData;

    #endregion

    #region 생명주기

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        if (button != null) button.onClick.AddListener(Click);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Click);
    }

    #endregion

    #region 초기화 / 갱신

    public void Init(StageDataSO data, Action<StageDataSO> clicked)
    {
        stageData = data;
        onClicked = clicked;
        Refresh(null);
    }

    public void SetClickCallback(Action<StageDataSO> clicked)
    {
        onClicked = clicked;
    }

    public void Refresh(IStageProgressService progressService)
    {
        bool hasData = stageData != null;
        bool unlocked = hasData && (progressService == null || progressService.IsStageUnlocked(stageData));
        int starMask = hasData && progressService != null ? progressService.GetStarMask(stageData.StageId) : 0;

        if (titleText != null)
            titleText.text = hasData ? stageData.DisplayName : "Empty";

        if (button != null)
            button.interactable = hasData && unlocked;

        if (lockRoot != null)
            lockRoot.SetActive(hasData && !unlocked);

        if (lockText != null)
            lockText.gameObject.SetActive(hasData && !unlocked);

        RefreshStars(starMask);
    }

    void RefreshStars(int starMask)
    {
        if (emptyStars == null) return;

        for (int i = 0; i < emptyStars.Length; i++)
        {
            if (emptyStars[i] == null) continue;

            bool hasStar = (starMask & (1 << i)) != 0;

            emptyStars[i].SetActive(!hasStar);
        }
    }

    #endregion

    #region 클릭

    void Click()
    {
        if (stageData == null) return;
        onClicked?.Invoke(stageData);
    }

    #endregion
}