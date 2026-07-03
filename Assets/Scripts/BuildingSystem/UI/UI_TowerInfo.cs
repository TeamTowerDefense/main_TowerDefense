using IGameInterface;
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
    [SerializeField] private TextMeshProUGUI targetModeText;


    private void OnEnable()
    {
        Hide();
    }

    public void SetInfo(TowerData towerData, EnemyTargetMode enemyTargetMode)
    {
        if (towerData == null) return;

        if (nameText != null) nameText.text = towerData.buildingName;

        if (damageText != null) damageText.text = $"공격력 : {towerData.damage}";
        if (rangeText != null) rangeText.text = $"사거리 : {towerData.attackRange}";

        if (attackSpeedText != null)
        {
            string atkRateText = (towerData.attackInterval * Mathf.Max(0.01f, towerData.attackSpeed)).ToString();
            attackSpeedText.text = $"공격속도 : {atkRateText} / s";
        }

        if(targetModeText != null)
        {
            string targetModeStr = "";

            switch (enemyTargetMode)
            { 
                case EnemyTargetMode.ClosestToTower:
                    targetModeStr = "가까운 적 우선";
                    break;
                case EnemyTargetMode.FarthestFromTower:
                    targetModeStr = "먼 적 우선";
                    break;
                case EnemyTargetMode.FrontMost:
                    targetModeStr = "목적지에 가까운 적 우선";
                    break;
                case EnemyTargetMode.BackMost:
                    targetModeStr = "목적지에 먼 적 우선";
                    break;
            }

            targetModeText.text = targetModeStr;
        }
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
