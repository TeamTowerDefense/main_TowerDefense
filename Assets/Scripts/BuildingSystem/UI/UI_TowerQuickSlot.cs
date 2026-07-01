using UnityEngine;
using System;

public class UI_TowerQuickSlot : MonoBehaviour
{
    public event Action<int> OnSlotSelected;

    [Header("½½·Ô ¸®½ºÆ®")]
    [SerializeField]
    private Slot_TowerQuickSlot slotPF;
    [SerializeField]
    private Transform slotParent;

    private Slot_TowerQuickSlot[] slots;
    private IResourceSystem resourceSystem;

    private void OnDestroy()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].OnClicked -= HandleSlotClicked;
            }
        }

        if (resourceSystem != null)
        {
            resourceSystem.OnResourceChanged -= UpdateAllSlotsAffordability;
        }
    }


    public void SetupUI(BuildingData[] buildingDatas, IResourceSystem resourceSystem)
    {
        if (buildingDatas == null)
            return;

        this.resourceSystem = resourceSystem;

        slots = new Slot_TowerQuickSlot[buildingDatas.Length];

        for (int i = 0; i < buildingDatas.Length; i++)
        {
            if (slotPF == null)
                break;

            Slot_TowerQuickSlot newSlot = Instantiate(slotPF, slotParent);
            newSlot.Initialize(i);
            newSlot.SetSlotData(buildingDatas[i]);
            newSlot.OnClicked += HandleSlotClicked;

            slots[i] = newSlot;
        }

        if (resourceSystem != null)
        {
            resourceSystem.OnResourceChanged += UpdateAllSlotsAffordability;
            UpdateAllSlotsAffordability(resourceSystem.CurrentResource);
        }
    }

    private void UpdateAllSlotsAffordability(int currentMoney)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].UpdateAffordability(currentMoney);
            }
        }
    }

    private void HandleSlotClicked(int index)
    {
        UpdateHighlight(index);

        OnSlotSelected?.Invoke(index);
    }

    public void UpdateHighlight(int selectedIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetHighlight(i == selectedIndex);
            }
        }
    }

    public void ClearHighlight()
    {
        foreach (var slot in slots)
        {
            if (slot != null) slot.SetHighlight(false);
        }
    }


}
