using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsPanelController : MonoBehaviour
{
    readonly struct DisplaySize
    {
        public readonly int Width;
        public readonly int Height;

        public DisplaySize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    static SettingsPanelController current;

    readonly List<DisplaySize> resolutions = new();

    [Header("프리팹 UI")]
    [SerializeField] GameObject overlayRoot;
    [SerializeField] GameObject launcherRoot;
    [SerializeField] Button launcherButton;
    [SerializeField] Button previousResolutionButton;
    [SerializeField] Button nextResolutionButton;
    [SerializeField] TMP_Text resolutionValue;
    [SerializeField] Button fullscreenButton;
    [SerializeField] GameObject fullscreenOnVisual;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button applyButton;
    [SerializeField] Button closeButton;

    [Header("씬 옵션")]
    [Tooltip("로비처럼 별도 설정 버튼이 없는 씬에서 프리팹 내 설정 버튼을 표시합니다.")]
    [SerializeField] bool showLauncherButton;

    Action closedCallback;
    int resolutionIndex;
    bool fullscreenValue;

    public static SettingsPanelController Current => current;
    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    void Awake()
    {
        current = this;
        BuildResolutionList();
        BindButtons();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (launcherRoot != null)
            launcherRoot.SetActive(showLauncherButton);
    }

    void OnEnable()
    {
        current = this;
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        UnbindButtons();

        if (current == this)
            current = null;
    }

    void Update()
    {
        if (IsOpen && closedCallback == null && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Close();
    }

    public void Open() => Open(null);

    public void Open(Action onClosed)
    {
        if (overlayRoot == null)
        {
            Debug.LogWarning("[SettingsPanelController] Overlay Root가 연결되지 않았습니다.", this);
            return;
        }

        closedCallback = onClosed;
        RefreshValues();
        overlayRoot.SetActive(true);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Close()
    {
        if (!IsOpen) return;

        PlayerPrefs.Save();
        overlayRoot.SetActive(false);

        Action callback = closedCallback;
        closedCallback = null;
        callback?.Invoke();
    }

    void BindButtons()
    {
        if (launcherButton != null) launcherButton.onClick.AddListener(OpenFromLauncher);
        if (previousResolutionButton != null) previousResolutionButton.onClick.AddListener(PreviousResolution);
        if (nextResolutionButton != null) nextResolutionButton.onClick.AddListener(NextResolution);
        if (fullscreenButton != null) fullscreenButton.onClick.AddListener(ToggleFullscreen);
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (applyButton != null) applyButton.onClick.AddListener(ApplyDisplay);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void UnbindButtons()
    {
        if (launcherButton != null) launcherButton.onClick.RemoveListener(OpenFromLauncher);
        if (previousResolutionButton != null) previousResolutionButton.onClick.RemoveListener(PreviousResolution);
        if (nextResolutionButton != null) nextResolutionButton.onClick.RemoveListener(NextResolution);
        if (fullscreenButton != null) fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        if (applyButton != null) applyButton.onClick.RemoveListener(ApplyDisplay);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    void OpenFromLauncher() => Open();
    void OnMasterVolumeChanged(float value) => GameSettingsStore.SetMasterVolume(value);
    void OnBgmVolumeChanged(float value) => GameSettingsStore.SetBgmVolume(value);
    void OnSfxVolumeChanged(float value) => GameSettingsStore.SetSfxVolume(value);

    void BuildResolutionList()
    {
        resolutions.Clear();
        HashSet<long> unique = new();

        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width < 1024 || resolution.height < 576) continue;

            long key = ((long)resolution.width << 32) | (uint)resolution.height;
            if (unique.Add(key))
                resolutions.Add(new DisplaySize(resolution.width, resolution.height));
        }

        long currentKey = ((long)Screen.width << 32) | (uint)Screen.height;
        if (unique.Add(currentKey))
            resolutions.Add(new DisplaySize(Screen.width, Screen.height));

        resolutions.Sort((left, right) =>
        {
            long leftArea = (long)left.Width * left.Height;
            long rightArea = (long)right.Width * right.Height;
            int areaCompare = leftArea.CompareTo(rightArea);
            return areaCompare != 0 ? areaCompare : left.Width.CompareTo(right.Width);
        });

        resolutionIndex = resolutions.FindIndex(
            value => value.Width == Screen.width && value.Height == Screen.height);

        if (resolutionIndex < 0)
            resolutionIndex = Mathf.Max(0, resolutions.Count - 1);
    }

    void RefreshValues()
    {
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(GameSettingsStore.MasterVolume);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(GameSettingsStore.BgmVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(GameSettingsStore.SfxVolume);

        fullscreenValue = GameSettingsStore.Fullscreen;
        RefreshFullscreenVisual();
        UpdateResolutionText();
    }

    void PreviousResolution()
    {
        if (resolutions.Count == 0) return;
        resolutionIndex = (resolutionIndex - 1 + resolutions.Count) % resolutions.Count;
        UpdateResolutionText();
    }

    void NextResolution()
    {
        if (resolutions.Count == 0) return;
        resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
        UpdateResolutionText();
    }

    void UpdateResolutionText()
    {
        if (resolutionValue == null || resolutions.Count == 0) return;

        DisplaySize value = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        resolutionValue.text = $"{value.Width} x {value.Height}";
    }

    void ToggleFullscreen()
    {
        fullscreenValue = !fullscreenValue;
        RefreshFullscreenVisual();
    }

    void RefreshFullscreenVisual()
    {
        if (fullscreenOnVisual != null)
            fullscreenOnVisual.SetActive(fullscreenValue);
    }

    void ApplyDisplay()
    {
        if (resolutions.Count == 0) return;

        DisplaySize value = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        FullScreenMode mode = fullscreenValue
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(value.Width, value.Height, mode);
        GameSettingsStore.SaveDisplay(value.Width, value.Height, fullscreenValue);
    }
}
