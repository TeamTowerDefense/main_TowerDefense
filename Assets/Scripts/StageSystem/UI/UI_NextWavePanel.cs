using IGameInterface;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_NextWavePanel : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform wavecontent;
    [SerializeField] LobbyStageWaveView wavePrefab;

    [Header("버튼")]
    [SerializeField] Button startWaveButton;

    IStageService stageController;
    LobbyStageWaveView currentWaveView;

    private void Awake()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(OnClickStartWave);
    }
    private void Start()
    {
        Bind();
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(OnClickStartWave);

        Unbind();
    }

    void Bind()
    {
        Unbind();

        if (!ServiceLocator.TryGet(out stageController))
        {
            Hide();
            return;
        }

        stageController.StateChanged += OnStageStateChanged;
        Refresh();
    }
    void Unbind()
    {
        if (stageController != null) stageController.StateChanged -= OnStageStateChanged;
        stageController = null;
    }
    void OnStageStateChanged(StageState state) => Refresh();
    void Refresh()
    {
        if (stageController == null)
        {
            Hide();
            return;
        }

        bool shouldShow = stageController.CurrentState == IGameInterface.StageState.Prepare ||
            stageController.CurrentState == IGameInterface.StageState.WaveClear;

        if (!shouldShow)
        {
            Hide();
            return;
        }

        if (!stageController.TryGetNextWavePreviewInfo(out StageWavePreviewInfo nextInfo))
        {
            Hide();
            return;
        }

        Show(nextInfo);
    }
    void Show(StageWavePreviewInfo info)
    {
        ClearWaveView();

        if (panelRoot != null) panelRoot.SetActive(true);
        if (startWaveButton != null) startWaveButton.interactable = true;
        if (wavePrefab == null || wavecontent == null) return;

        currentWaveView = Instantiate(wavePrefab, wavecontent);
        currentWaveView.Bind(info);
    }
    void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        ClearWaveView();
    }
    void OnClickStartWave()
    {
        if (stageController == null) return;

        if (startWaveButton != null) startWaveButton.interactable = false;

        stageController.StartWave();

        bool failedToStart =
            stageController.CurrentState == StageState.Prepare ||
            stageController.CurrentState == StageState.WaveClear;

        if (failedToStart && startWaveButton != null)
            startWaveButton.interactable = true;
    }
    void ClearWaveView()
    {
        if (currentWaveView == null) return;

        currentWaveView.gameObject.SetActive(false);
        Destroy(currentWaveView.gameObject);
        currentWaveView = null;
    }
}
