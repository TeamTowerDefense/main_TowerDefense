using UnityEngine;

public class StageTimeController : MonoBehaviour
{
    [Header("¹è¼Ó")]
    [SerializeField, Min(0f)] float defaultTimeScale = 1f;
    [SerializeField, Min(0f)] float maxTimeScale = 3f;

    public float CurrentTimeScale => Time.timeScale;
    public bool IsPaused => Mathf.Approximately(Time.timeScale, 0f);

    private void Awake() => SetTimeScale(1f);
    private void OnDestroy() => Time.timeScale = 1f;

    public void Pause() => SetTimeScale(0f);
    public void Normal() => SetTimeScale(1f);
    public void Slow() => SetTimeScale(0.5f);
    public void Fast() => SetTimeScale(2f);
    public void VeryFast() => SetTimeScale(3f);

    public void SetTimeScale(float timeScale)
        => Time.timeScale = Mathf.Clamp(timeScale, 0f, maxTimeScale);

}
