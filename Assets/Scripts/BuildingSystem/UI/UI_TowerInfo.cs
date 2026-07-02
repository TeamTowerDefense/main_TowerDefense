using TMPro;
using UnityEngine;

public class UI_TowerInfo : MonoBehaviour
{
    [Header("캔버스 그룹")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("텍스트 UI 연결")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;


    private void OnEnable()
    {
        Hide();
    }

    public void SetInfo(TowerData towerData)
    {
        if (towerData == null) return;

        if (nameText != null) nameText.text = towerData.buildingName;

        if (damageText != null) damageText.text = $"공격력 : {towerData.damage}";
        if (rangeText != null) rangeText.text = $"사거리 : {towerData.attackRange}";

        // 공격 속도는 보통 '1초당 몇 번 때리는가' 혹은 '몇 초마다 때리는가'로 표현합니다.
        if (attackSpeedText != null) attackSpeedText.text = $"공격속도 : {towerData.attackInterval}초";
    }

    public void Show()
    {
        canvasGroup.alpha = 1;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
    }
}
