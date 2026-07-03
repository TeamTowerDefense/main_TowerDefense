using IGameInterface;
using TMPro;
using UnityEngine;

public class UI_WaveCount : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField]
    private TextMeshProUGUI text_waveCount;
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IStageService> stageInterfactRef;

    public IStageService stageService { get; private set; }

    private void Start()
    {
        if (stageInterfactRef != null)
        {
            stageService = stageInterfactRef.Value;
        }

        if(stageService !=null)
        {
            stageService.StateChanged += OnStageStateChanged;
        }
    }

    private void OnStageStateChanged(StageState state)
    {
        if (text_waveCount == null)
            return;

        string text = stageService.CurrentWaveIndex + 1 + " / " + stageService.WaveCount;

        switch(state)
        {
            case StageState.Playing:
                SetWaveText(text);
                break;
            case StageState.Prepare:
                string newText = "준비 " + text;
                SetWaveText(newText);
                break;
        }
    }

    private void SetWaveText(string text)
    { 
        text_waveCount.text = text;
    }
}
