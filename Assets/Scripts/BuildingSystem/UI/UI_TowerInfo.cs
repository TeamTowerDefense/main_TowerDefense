using IGameInterface;
using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI keywordText;


    private void OnEnable()
    {
        Hide();
    }

    public void SetTowerInfo(Tower selectedTower)
    {
        if (selectedTower == null) return;

        BuildingData data = selectedTower.BuildingData;
        if (data != null && nameText != null)
        {
            nameText.text = data.buildingName;
        }

        if (damageText != null)
            damageText.text = $"공격력 : {selectedTower.GetStat(StatType.AttackDamage)}";

        if (rangeText != null)
            rangeText.text = $"사거리 : {selectedTower.GetStat(StatType.AttackRange)}";

        if (attackSpeedText != null)
        {
            float currentAtkSpeed = selectedTower.GetStat(StatType.AttackSpeed);
            attackSpeedText.text = $"공격속도 : {currentAtkSpeed} / s";
        }

        KeywordController keywordController = selectedTower.GetComponent<KeywordController>();
        Debug.Log(keywordController == null);
        Debug.Log(keywordText == null);
        if (keywordController != null && keywordText != null)
        {
            List<string> keywordNames = keywordController.GetActiveKeywordNames();
            Debug.Log(keywordNames.Count);

            if (keywordNames.Count > 0)
                keywordText.text = "<color=#FFD700>#" + string.Join(" #", keywordNames) + "</color>";
            else
                keywordText.text = "<color=#888888>특성 없음</color>";
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
