using IGameFlowInterface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InGamePauseMenuController : MonoBehaviour
{
    [Header("프리팹 UI")]
    [SerializeField] GameObject overlayRoot;
    [SerializeField] GameObject pausePage;
    [SerializeField] Button settingsButton;
    [SerializeField] Button retryButton;
    [SerializeField] Button exitButton;

    SettingsPanelController settingsPanel;
    StageTimeController stageTimeController;
    TopViewCameraController topViewCameraController;
    bool stageTimeControllerWasEnabled;
    bool cameraInputWasBlocked;
    bool isOpen;
    bool sceneChangeRequested;
    float timeScaleBeforePause = 1f;
    CursorLockMode cursorLockBeforePause;
    bool cursorVisibleBeforePause;

    void Awake()
    {
        BindButtons();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;

        if (!isOpen)
        {
            OpenPauseMenu();
            return;
        }

        if (settingsPanel != null && settingsPanel.IsOpen)
        {
            settingsPanel.Close();
            return;
        }

        ResumeGame();
    }

    void OnDestroy()
    {
        UnbindButtons();

        if (sceneChangeRequested)
        {
            Time.timeScale = 1f;
            return;
        }

        if (isOpen)
            RestoreGameplayState();
    }

    void BindButtons()
    {
        if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPage);
        if (retryButton != null) retryButton.onClick.AddListener(RetryStage);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToLobby);
    }

    void UnbindButtons()
    {
        if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettingsPage);
        if (retryButton != null) retryButton.onClick.RemoveListener(RetryStage);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitToLobby);
    }

    void OpenPauseMenu()
    {
        if (isOpen || sceneChangeRequested || overlayRoot == null) return;

        isOpen = true;
        timeScaleBeforePause = Time.timeScale;
        cursorLockBeforePause = Cursor.lockState;
        cursorVisibleBeforePause = Cursor.visible;

        stageTimeController = FindFirstObjectByType<StageTimeController>();
        if (stageTimeController != null)
        {
            stageTimeControllerWasEnabled = stageTimeController.enabled;
            stageTimeController.enabled = false;
        }

        topViewCameraController = FindFirstObjectByType<TopViewCameraController>();
        if (topViewCameraController != null)
        {
            cameraInputWasBlocked = topViewCameraController.InputBlocked;
            topViewCameraController.SetInputBlocked(true);
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        overlayRoot.SetActive(true);
        ShowPausePage();
    }

    void ResumeGame()
    {
        if (!isOpen || sceneChangeRequested) return;

        overlayRoot.SetActive(false);
        isOpen = false;
        RestoreGameplayState();
    }

    void RestoreGameplayState()
    {
        Time.timeScale = timeScaleBeforePause;
        Cursor.lockState = cursorLockBeforePause;
        Cursor.visible = cursorVisibleBeforePause;

        if (stageTimeController != null && stageTimeControllerWasEnabled)
            stageTimeController.enabled = true;

        if (topViewCameraController != null)
            topViewCameraController.SetInputBlocked(cameraInputWasBlocked);

        stageTimeController = null;
        topViewCameraController = null;
    }

    void ShowPausePage()
    {
        if (!isOpen) return;

        if (pausePage != null)
            pausePage.SetActive(true);

        overlayRoot.SetActive(true);
    }

    void ShowSettingsPage()
    {
        settingsPanel = SettingsPanelController.Current;
        if (settingsPanel == null)
        {
            Debug.LogWarning("[InGamePauseMenuController] SettingsPanel 프리팹 인스턴스를 찾지 못했습니다.", this);
            return;
        }

        if (pausePage != null)
            pausePage.SetActive(false);

        settingsPanel.Open(ShowPausePage);
    }

    void RetryStage()
    {
        if (sceneChangeRequested) return;
        PrepareForSceneChange();

        bool hasRunContext =
            ServiceLocator.TryGet(out IStageRunContextService runContext) &&
            runContext.HasValidRun;

        if (hasRunContext &&
            ServiceLocator.TryGet(out IGameFlowService gameFlowService) &&
            ServiceLocator.TryGet(out ISceneLoadService _))
        {
            gameFlowService.RetryStage();
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    void ExitToLobby()
    {
        if (sceneChangeRequested) return;
        PrepareForSceneChange();

        if (ServiceLocator.TryGet(out IGameFlowService gameFlowService) &&
            ServiceLocator.TryGet(out ISceneLoadService _))
        {
            gameFlowService.ExitToLobbyWithStageSelect();
            return;
        }

        SceneManager.LoadScene("Lobby");
    }

    void PrepareForSceneChange()
    {
        sceneChangeRequested = true;
        isOpen = false;
        PlayerPrefs.Save();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        // 로딩이 시작되는 사이에 새 몬스터가 생성되지 않도록 먼저 스폰을 멈춘다.
        MonsterManager monsterManager = FindFirstObjectByType<MonsterManager>();
        if (monsterManager != null)
        {
            monsterManager.StopAllCoroutines();
            monsterManager.enabled = false;
        }

        // DontDestroyOnLoad 풀 부모 아래에 남을 수 있는 몬스터, 투사체,
        // 히트박스, VFX를 씬 전환 전에 모두 안전하게 반환한다.
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.DespawnAllActive();

        Time.timeScale = 1f;
    }

}
