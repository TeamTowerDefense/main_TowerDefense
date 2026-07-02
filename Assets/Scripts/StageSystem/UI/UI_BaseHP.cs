using IGameInterface;
using TMPro;
using UnityEngine;

public class UI_BaseHP : MonoBehaviour
{
    [Header("외부 시스템 참조")]
    [SerializeField]
    private InterfaceReference<IStageService> stageInterfactRef;
    [Header("텍스트 UI")]
    [SerializeField]
    private TextMeshProUGUI curHpText;

    public IStageService stageService { get; private set; }

    private void Awake()
    {

    }

    private void Start()
    {
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
