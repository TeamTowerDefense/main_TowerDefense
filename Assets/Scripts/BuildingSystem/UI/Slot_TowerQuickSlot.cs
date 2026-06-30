using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class Slot_TowerQuickSlot : MonoBehaviour
{
    public event Action<int> OnClicked;

    [Header("슬롯 내부 UI 요소")]
    [SerializeField] private Button slotButton;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image slotIcon;
    [SerializeField] private GameObject highlightFrame;

    private int slotIndex;

    public void Initialize(int index)
    {
        this.slotIndex = index;

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(HandleClick);
        }
    }

    public void SetSlotData(BuildingData data)
    {
        if (slotIcon != null)
            return;
        if (costText == null)
            return;

        if (data != null && slotIcon != null)
        {
            //slotIcon.sprite = data.towerIcon;
            slotIcon.enabled = true;
        }
        else if (slotIcon != null)
        {
            slotIcon.enabled = false;
        }

        if(data != null &&  costText != null)
        {
            costText.text = data.cost.ToString();
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightFrame != null)
        {
            highlightFrame.SetActive(isActive);
        }
    }

    private void HandleClick()
    {
        OnClicked?.Invoke(slotIndex);
    }
}
