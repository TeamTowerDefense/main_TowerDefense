using UnityEngine;

public class CaveEntrance : MonoBehaviour
{
    [Header("이펙트 위치 설정")]
    public Transform fogPoint;
    public Transform rockfallPoint;

    // 현재 동굴에 생성된 이펙트 기억
    [HideInInspector] public GameObject activeFog;
    [HideInInspector] public GameObject activeRockfall;

    private void Start()
    {
        // 💡 생성되자마자 스패너 매니저에 자신을 등록합니다.
        if (CaveSpawner.Instance != null)
        {
            CaveSpawner.Instance.RegisterEntrance(this);
        }
    }

    private void OnDestroy()
    {
        // 💡 파괴될 때 등록을 해제합니다.
        if (CaveSpawner.Instance != null)
        {
            CaveSpawner.Instance.UnregisterEntrance(this);
        }
    }

    private void OnDrawGizmos()
    {
        if (fogPoint != null)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawSphere(fogPoint.position, 0.3f);
            Gizmos.DrawWireSphere(fogPoint.position, 0.3f);
        }

        if (rockfallPoint != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawSphere(rockfallPoint.position, 0.3f);
            Gizmos.DrawWireSphere(rockfallPoint.position, 0.3f);
        }
    }
}