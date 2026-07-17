using UnityEngine;
using Unity.Cinemachine; 

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        // 무조건 현재 씬에 배치되어 있는(아까 설정한) 이 오브젝트를 인스턴스로 등록!
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(float duration, float magnitude)
    {
        if (impulseSource != null)
        {
            // 아까 Inspector의 'Default Velocity'와 'Bump' 모양 그대로 강도만 곱해서 발사!
            impulseSource.GenerateImpulseWithForce(magnitude);
            Debug.Log($"[CameraShaker] 시네마신 카메라 흔들기 작동! 강도: {magnitude}");
        }
        else
        {
            Debug.LogError("[CameraShaker] ImpulseSource를 찾을 수 없습니다!");
        }
    }
}