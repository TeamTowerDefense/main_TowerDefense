using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMonsterPreviewView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text countText;

    public void Bind(MonsterSpawnPreviewInfo info)
    {
        if (info == null)
        {
            Clear();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = info.Icon;
            iconImage.enabled = info.Icon != null;
            iconImage.preserveAspect = true;
        }

        if (countText != null) countText.text = $"× {info.Count}";
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (countText != null) countText.text = string.Empty;
    }
}
