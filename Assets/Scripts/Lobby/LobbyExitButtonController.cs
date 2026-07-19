using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyExitButtonController : MonoBehaviour
{
    [SerializeField] Button exitButton;

    void Awake()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
