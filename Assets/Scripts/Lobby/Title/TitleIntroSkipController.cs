using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class TitleIntroSkipController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] PlayableDirector director;
    [SerializeField] TitleTowerLoopController tlc;

    [Header("최종 표시 UI")]
    [SerializeField] GameObject finalMenuRoot;

    [Header("더블 클릭 인터벌")]
    [SerializeField] float doubleClickTime = 0.35f;

    [Header("스킵 옵션")]
    [SerializeField] bool skipWithKeyboardAnyKey = true;
    [SerializeField] bool skipWithEscape = true;
    [SerializeField] bool skipWithMouseDoubleClick = true;

    bool skipped;
    bool finished;
    float lastClickTime = -999f;

    #region 생명주기
    private void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();
        if (finalMenuRoot) finalMenuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (director) director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (director) director.stopped -= OnTimelineStopped;
    }

    private void Update()
    {
        if (finished || skipped || !director) return;
        if (ShouldSkip()) Skip();
    }
    #endregion

    bool ShouldSkip()
    {
        if (skipWithEscape && Keyboard.current?.escapeKey.wasPressedThisFrame == true) return true;
        if (skipWithKeyboardAnyKey && Keyboard.current?.anyKey.wasPressedThisFrame == true) return true;
        if (skipWithMouseDoubleClick && IsDoubleClick()) return true;

        return false;
    }
    bool IsDoubleClick()
    {
        if (Mouse.current?.leftButton.wasPressedThisFrame != true) return false;

        float now = Time.unscaledTime;
        bool isDoubleClick = now - lastClickTime <= doubleClickTime;
        lastClickTime = now;

        return isDoubleClick;
    }
    void Skip()
    {
        skipped = true;

        director.time = director.duration;
        director.Evaluate();
        director.Stop();

        ShowFinalState();
    }
    void OnTimelineStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != director) return;
        ShowFinalState();
    }
    void ShowFinalState()
    {
        if (finished) return;

        finished = true;
        if (finalMenuRoot) finalMenuRoot.SetActive(true);

        tlc?.StartIdle();
    }
}

