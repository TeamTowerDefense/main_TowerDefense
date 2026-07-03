using IGameFlowInterface;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuController : MonoBehaviour
{
    #region 인스펙터

    [Header("버튼")]
    [SerializeField] Button playButton;
    [SerializeField] Button settingButton;
    [SerializeField] Button exitButton;

    [Header("패널")]
    [SerializeField] GameObject settingPanel;

    [Header("옵션")]
    [SerializeField] bool hideSettingPanelOnAwake = true;

    #endregion

    #region 생명주기

    void Awake()
    {
        if (hideSettingPanelOnAwake && settingPanel != null)
            settingPanel.SetActive(false);

        BindButtons();
    }

    void OnDestroy()
    {
        UnbindButtons();
    }

    #endregion

    #region 버튼 바인딩

    void BindButtons()
    {
        if (playButton != null) playButton.onClick.AddListener(OnClickPlay);
        if (settingButton != null) settingButton.onClick.AddListener(OnClickSetting);
        if (exitButton != null) exitButton.onClick.AddListener(OnClickExit);
    }

    void UnbindButtons()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnClickPlay);
        if (settingButton != null) settingButton.onClick.RemoveListener(OnClickSetting);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnClickExit);
    }

    #endregion

    #region 버튼 이벤트

    public void OnClickPlay()
    {
        if (!ServiceLocator.TryGet(out IGameFlowService gameFlowService))
        {
            Debug.LogError("[TitleMenuController] IGameFlowService를 찾지 못했습니다.", this);
            return;
        }

        gameFlowService.EnterLobby();
    }

    public void OnClickSetting()
    {
        if (settingPanel == null)
        {
            Debug.LogWarning("[TitleMenuController] Setting Panel이 없습니다.", this);
            return;
        }

        settingPanel.SetActive(!settingPanel.activeSelf);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}