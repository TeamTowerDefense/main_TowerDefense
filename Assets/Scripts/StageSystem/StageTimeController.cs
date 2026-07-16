using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StageTimeController : MonoBehaviour
{
    [Header("배속")]
    [SerializeField, Min(0f)] float defaultTimeScale = 1f;
    [SerializeField, Min(0f)] float maxTimeScale = 3f;

    [Header("UI")]
    [SerializeField] Image togglePlayImage;
    [SerializeField] Sprite pauseImage;
    [SerializeField] Sprite playImage;

    [Header("입력")]
    [SerializeField]
    InputAction togglePlayAction =
        new("Toggle Play", InputActionType.Button, "<Keyboard>/space");

    [SerializeField]
    InputAction speedDownAction =
        new("Speed Down", InputActionType.Button, "<Keyboard>/z");

    [SerializeField]
    InputAction speedUpAction =
        new("Speed Up", InputActionType.Button, "<Keyboard>/c");

    float lastPlayingTimeScale = 1f;

    public float CurrentTimeScale => Time.timeScale;
    public bool IsPaused => Mathf.Approximately(Time.timeScale, 0f);

    void Awake()
    {
        lastPlayingTimeScale = Mathf.Clamp(defaultTimeScale, 0.01f, maxTimeScale);
        SetTimeScale(defaultTimeScale);
    }

    void OnEnable()
    {
        togglePlayAction.performed += OnTogglePlay;
        speedDownAction.performed += OnSpeedDown;
        speedUpAction.performed += OnSpeedUp;

        togglePlayAction.Enable();
        speedDownAction.Enable();
        speedUpAction.Enable();
    }

    void OnDisable()
    {
        togglePlayAction.performed -= OnTogglePlay;
        speedDownAction.performed -= OnSpeedDown;
        speedUpAction.performed -= OnSpeedUp;

        togglePlayAction.Disable();
        speedDownAction.Disable();
        speedUpAction.Disable();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void Pause() => SetTimeScale(0f);
    public void Normal() => SetTimeScale(1f);
    public void Slow() => SetTimeScale(0.5f);
    public void Fast() => SetTimeScale(2f);
    public void VeryFast() => SetTimeScale(3f);

    public void SetTimeScale(float timeScale)
    {
        float clampedTimeScale = Mathf.Clamp(timeScale, 0f, maxTimeScale);

        if (clampedTimeScale > 0f)
            lastPlayingTimeScale = clampedTimeScale;

        Time.timeScale = clampedTimeScale;
        RefreshPlayImage();
    }

    public void TogglePlay()
    {
        if (IsPaused)
            SetTimeScale(lastPlayingTimeScale);
        else
            Pause();
    }

    public void SpeedDown()
    {
        float currentScale = IsPaused
            ? lastPlayingTimeScale
            : CurrentTimeScale;

        if (currentScale > 2f)
            Fast();
        else if (currentScale > 1f)
            Normal();
        else
            Slow();
    }

    public void SpeedUp()
    {
        float currentScale = IsPaused
            ? lastPlayingTimeScale
            : CurrentTimeScale;

        if (currentScale < 1f)
            Normal();
        else if (currentScale < 2f)
            Fast();
        else
            VeryFast();
    }

    void RefreshPlayImage()
    {
        if (togglePlayImage == null)
            return;

        togglePlayImage.sprite = IsPaused
            ? playImage
            : pauseImage;
    }

    void OnTogglePlay(InputAction.CallbackContext context) => TogglePlay();
    void OnSpeedDown(InputAction.CallbackContext context) => SpeedDown();
    void OnSpeedUp(InputAction.CallbackContext context) => SpeedUp();
}