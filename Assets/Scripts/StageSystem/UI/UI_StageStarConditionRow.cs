using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StageStarConditionRow : MonoBehaviour
{
    [Header("결과 표시 텍스트")]
    [SerializeField] TextMeshProUGUI markText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;

    [Header("결과 아이콘")]
    [SerializeField] Image successIcon;
    [SerializeField] Image failIcon;

    [Header("표시")]
    [SerializeField] Sprite successMark;
    [SerializeField] Sprite failMark;


}
