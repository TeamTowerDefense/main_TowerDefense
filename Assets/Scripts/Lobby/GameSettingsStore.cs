using UnityEngine;

public static class GameSettingsStore
{
    const string MasterVolumeKey = "TD.Settings.MasterVolume";
    const string BgmVolumeKey = "TD.Settings.BgmVolume";
    const string SfxVolumeKey = "TD.Settings.SfxVolume";
    const string ResolutionWidthKey = "TD.Settings.ResolutionWidth";
    const string ResolutionHeightKey = "TD.Settings.ResolutionHeight";
    const string FullscreenKey = "TD.Settings.Fullscreen";

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    public static float BgmVolume => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySavedDisplay()
    {
        if (!PlayerPrefs.HasKey(ResolutionWidthKey) || !PlayerPrefs.HasKey(ResolutionHeightKey)) return;

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        if (width > 0 && height > 0) Screen.SetResolution(width, height, mode);
    }

    public static void ApplyAudio(SoundManager soundManager)
    {
        if (soundManager == null) return;

        soundManager.SetMasterVolume(MasterVolume);
        soundManager.SetBgmVolume(BgmVolume);
        soundManager.SetSfxVolume(SfxVolume);
    }

    public static void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        SoundManager.Instance?.SetMasterVolume(value);
    }

    public static void SetBgmVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BgmVolumeKey, value);
        SoundManager.Instance?.SetBgmVolume(value);
    }

    public static void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        SoundManager.Instance?.SetSfxVolume(value);
    }

    public static void SaveDisplay(int width, int height, bool fullscreen)
    {
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
