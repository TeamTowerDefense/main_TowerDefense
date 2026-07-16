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

    StageController stageController;
    LobbyStageWaveView currentWaveView;

    private void Awake()
    {
        
    }
}
