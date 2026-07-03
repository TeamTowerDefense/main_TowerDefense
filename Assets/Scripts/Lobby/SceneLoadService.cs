using IGameFlowInterface;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadService : GlobalServiceBase, ISceneLoadService
{
    [Header("로딩 씬")] 
    [SerializeField]string loadingSceneName = "LoadingScene";

    [Header("로그 옵션")]
    [SerializeField] bool logOption = true;

    Coroutine loadRoutine;

    public event Action<string> LoadStarted;
    public event Action<float> LoadProgressChanged;
    public event Action<string> LoadCompleted;

    public bool IsLoading { get; private set; }
    public string CurrentSceneName => SceneManager.GetActiveScene().name;
    public string PendingSceneName { get; private set; }

    #region 인터페이스 구현
    public void LoadScene(string sceneName, Action onLoaded = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoadService] 로드할 씬 이름이 비었습니다", this);
            return;
        }

        StartLoad(LoadSceneRoutine(sceneName, onLoaded));
    }
    public void LoadSceneWithLoading(string targetSceneName, Action onLoaded = null)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[SceneLoadService] 로드할 대상 씬 이름이 비어 있습니다.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            LoadScene(targetSceneName, onLoaded);
            return;
        }

        StartLoad(LoadSceneWithLoadingRoutine(targetSceneName, onLoaded));
    }

    public void ReloadCurrentScene(Action onLoaded = null)
    {
        LoadSceneWithLoading(CurrentSceneName, onLoaded);
    }
    #endregion

    #region 로드 루틴
    void StartLoad(IEnumerator routine)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoadService] 이미 로딩 중입니다. Pending: {PendingSceneName}", this);
            return;
        }

        loadRoutine = StartCoroutine(routine);
    }

    IEnumerator LoadSceneRoutine(string sceneName, Action onLoaded)
    {
        IsLoading = true;
        PendingSceneName = sceneName;

        LoadStarted?.Invoke(sceneName);
        LoadProgressChanged?.Invoke(0f);

        if (logOption) Debug.Log($"[SceneLoadService] Load Start: {sceneName}", this);

        yield return LoadAsync(sceneName, 0f, 1f);

        FinishLoad(sceneName, onLoaded);
    }

    IEnumerator LoadSceneWithLoadingRoutine(string targetSceneName, Action onLoaded)
    {
        IsLoading = true;
        PendingSceneName = targetSceneName;

        LoadStarted?.Invoke(targetSceneName);
        LoadProgressChanged?.Invoke(0f);

        if (logOption) Debug.Log($"[SceneLoadService] LoadingScene -> {targetSceneName}", this);

        yield return LoadAsync(loadingSceneName, 0f, 0.1f);

        yield return null;

        yield return LoadAsync(targetSceneName, 0.1f, 1f);

        FinishLoad(targetSceneName, onLoaded);
    }

    IEnumerator LoadAsync(string sceneName, float progressFrom, float progressTo)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            LoadProgressChanged?.Invoke(Mathf.Lerp(progressFrom, progressTo, progress));
            yield return null;
        }

        LoadProgressChanged?.Invoke(progressTo);
    }

    void FinishLoad(string sceneName, Action onLoaded)
    {
        LoadProgressChanged?.Invoke(1f);
        LoadCompleted?.Invoke(sceneName);

        if (logOption) Debug.Log($"[SceneLoadService] Load Complete: {sceneName}", this);

        PendingSceneName = string.Empty;
        IsLoading = false;
        loadRoutine = null;

        onLoaded?.Invoke();
    }
    #endregion
}
