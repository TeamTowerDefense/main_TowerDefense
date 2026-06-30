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

    private void OnDestroy()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].OnClicked -= HandleSlotClicked;
            }
        }
    }

    public void SetupUI(BuildingData[] activeTowers)
    {
        if (activeTowers == null)
            return;

        slots = new Slot_TowerQuickSlot[activeTowers.Length];

        for (int i = 0; i < activeTowers.Length; i++)
        {
            if (slotPF == null)
                break;

            Slot_TowerQuickSlot newSlot = Instantiate(slotPF, slotParent);
            newSlot.Initialize(i);
            newSlot.SetSlotData(activeTowers[i]);
            newSlot.OnClicked += HandleSlotClicked;

            slots[i] = newSlot;
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
