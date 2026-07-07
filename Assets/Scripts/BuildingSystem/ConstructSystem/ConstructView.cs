using System;
using UnityEngine.UI;
using UnityEngine;
using IGameInterface;

public class ConstructView : MonoBehaviour
{
    [Header("시각적 렌더러")]
    public BuildingPreviewRenderer previewRenderer;

    [Header("UI 컴포넌트")]
    [SerializeField] private UI_TowerInteract towerInteractUI;
    [SerializeField] private UI_TowerQuickSlot quickSlotUI;
    [SerializeField]
    private UI_TowerInfo towerInfoUI;


    public UI_TowerInteract TowerInteractUI => towerInteractUI;

    #region 타워 미리보기 제어
    public void CreatePreview(GameObject previewPF) => previewRenderer.CreatePreview(previewPF);
    public void HidePreview() => previewRenderer.ShowPreview(false);

    public void UpdatePreview(Vector3 pos, bool isValid)
    {
        previewRenderer.UpdateTransform(pos, Quaternion.identity);
        previewRenderer.SetValidityColor(isValid);
        previewRenderer.ShowPreview(true);
    }
    #endregion


    #region 타워 상호작용 메뉴 UI
    public void InitalizeTowerInteractUI(Action onDestroyBtnClicked)
    {
        if(towerInteractUI != null)
            towerInteractUI.OnDestroyClicked += onDestroyBtnClicked;
    }

    public void UnbindTowerInteractUI(Action onDestroyBtnClicked)
    {
        if (towerInteractUI != null)
            towerInteractUI.OnDestroyClicked -= onDestroyBtnClicked;
    }

    public void ShowTowerMenu(Vector3 worldPos) 
    { 
        if (towerInteractUI != null) towerInteractUI.Show(worldPos); 
    }
    
    public void HideTowerMenu() 
    { 
        if (towerInteractUI != null) towerInteractUI.Hide(); 
    }
    #endregion

    #region 퀵슬롯 UI 제어
    public void InitializeQuickSlot(BuildingData[] deck, IResourceSystem resourceSystem, Action<int> onSlotSelected)
    {
        if (quickSlotUI != null)
        {
            quickSlotUI.SetupUI(deck, resourceSystem);
            quickSlotUI.OnSlotSelected += onSlotSelected;
        }
    }

    public void UnbindQuickSlot(Action<int> onSlotSelected)
    {
        if (quickSlotUI != null) quickSlotUI.OnSlotSelected -= onSlotSelected;
    }

    public void UpdateQuickSlotHighlight(int index) => quickSlotUI?.UpdateHighlight(index);
    public void ClearQuickSlotHighlight() => quickSlotUI?.ClearHighlight();
    #endregion

    #region 타워 정보 UI 제어
    public void ShowTowerInfo(Tower tower)
    {
        if (tower != null)
        {
            towerInfoUI.SetTowerInfo(tower);
            towerInfoUI.Show();
        }
    }

    public void HideTowerInfo()
    {
        if (towerInfoUI != null)
        {
            towerInfoUI.Hide();
        }
    }
    #endregion
}
