using IGameInterface;
using TMPro;
using UnityEngine;

public class UI_BaseHP : MonoBehaviour
{
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IStageService> stageInterfactRef;
    [Header("UI 컴포넌트")]
    [SerializeField]
    private TextMeshProUGUI curHpText;

    public IStageService stageService { get; private set; }

    private void Awake()
    {

    }

    private void Start()
    {
        if (stageInterfactRef != null)
        {
            stageService = stageInterfactRef.Value;
        }

        if (stageService != null)
        {
            stageService.BaseHpChanged += SetHpText;
        }

        if (curHpText != null)
        {
            curHpText.text = stageService.CurrentBaseHp.ToString();
        }
        else
        {
            curHpText.text = "";
        }
    }

    private void SetHpText(int curHP, int maxHP)
    {
        if (curHpText == null)
            return;

        curHpText.text = curHP.ToString();
    }

}
