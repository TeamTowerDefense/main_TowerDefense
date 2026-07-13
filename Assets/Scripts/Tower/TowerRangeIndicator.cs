using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    [Header("시각적 설정")]
    [Tooltip("바닥과 겹쳐서 깜빡거리는 현상(Z-Fighting)을 막기 위한 미세한 높이")]
    [SerializeField] private float yOffset = 0.05f;

    [Tooltip("외곽선 두께의 절반 정도 값을 넣어주세요 (예: 0.2 ~ 0.5)")]
    [SerializeField] private float visualPadding = 0.2f;

    private void Awake()
    {
        // 방향과 초기 위치 설정
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        transform.localPosition = new Vector3(0f, yOffset, 0f);

        gameObject.SetActive(false);
    }

    public void Show(float radius)
    {
        Debug.Log("Show Range");

        float paddedRadius = radius + visualPadding;
        float diameter = paddedRadius * 2f;

        transform.localScale = new Vector3(diameter, diameter, 1f);
        gameObject.SetActive(true);
    }

    // 타워 선택이 해제되었을 때 호출할 함수
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
