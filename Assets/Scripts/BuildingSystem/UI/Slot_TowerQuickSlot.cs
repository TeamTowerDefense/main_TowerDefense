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
    private int requireCost;

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
        if(data != null)
        {
            requireCost = data.cost;
        }
        else
        {
            if(slotIcon != null)
                slotIcon.enabled = false;
            if(costText != null)
                costText.enabled = false;
            return;
        }

        if (slotIcon != null)
        {
            //slotIcon.sprite = data.towerIcon;
            slotIcon.enabled = true;
        }

        if(costText != null)
        {
            costText.enabled = true;
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

    public void UpdateAffordability(int currentMoney)
    {
        bool canAfford = currentMoney >= requireCost;

        if (slotButton != null)
        {
            slotButton.interactable = canAfford;
        }
    }
}
