using TMPro;
using UnityEngine;

public class UI_Resource : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI resourceText;
    [SerializeField]
    private InterfaceReference<IResourceSystem> resourceInterfaceRef;

    private IResourceSystem resourceSystem;

    private void OnEnable()
    {
        resourceSystem = resourceInterfaceRef.Value;

        if (resourceSystem != null)
        {
            // 자원 변경 이벤트 구독
            resourceSystem.OnResourceChanged += UpdateResourceText;

            // 현재 자원량으로 첫 UI 즉시 갱신
            UpdateResourceText(resourceSystem.CurrentResource);
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼지거나 파괴될 때 구독 해제 (메모리 누수 방지)
        if (resourceSystem != null)
        {
            resourceSystem.OnResourceChanged -= UpdateResourceText;
        }
    }

    private void UpdateResourceText(int currentMoney)
    {
        if (resourceText != null)
        {
            resourceText.text = currentMoney.ToString("N0"); // 천 단위 콤마 포맷
        }
    }
}
