using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StageStarConditionRow : MonoBehaviour
{
    #region 인스펙터

    [Header("결과 표시 텍스트")]
    [SerializeField] TextMeshProUGUI markText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;

    [Header("결과 아이콘")]
    [SerializeField] Image resultIcon;

    [Header("표시")]
    [SerializeField] Sprite successMark;
    [SerializeField] Sprite failMark;
    [SerializeField] string successText = "성공";
    [SerializeField] string failText = "실패";

    #endregion

    #region 출력

    public void Set(StageStarConditionSO condition, bool achieved)
    {
        if (condition == null)
        {
            Set("빈 조건", "조건 SO가 비어 있습니다.", false);
            return;
        }

        Set(condition.DisplayName, condition.Description, achieved);
    }

    public void Set(string displayName, string description, bool achieved)
    {
        if (markText)
            markText.text = achieved ? successText : failText;

        if (nameText)
            nameText.text = string.IsNullOrWhiteSpace(displayName) ? "별 조건" : displayName;

        if (descriptionText)
            descriptionText.text = description ?? string.Empty;

        if (resultIcon)
        {
            resultIcon.sprite = achieved ? successMark : failMark;
            resultIcon.enabled = resultIcon.sprite != null;
        }
    }

    #endregion
}