using IGameInterface;
using Unity.Cinemachine;
using UnityEngine;

public enum TopViewZoomMode
{
    BlendTargetAndOffset,
    FixedAngleDistance
}

public class TopViewCameraController : MonoBehaviour, ICameraModule
{
    [Header("시네머신 카메라")]
    [SerializeField] CinemachineCamera topViewCam;

    [Header("탑뷰 타겟")]
    [SerializeField] Transform topViewTarget;
    [SerializeField] float moveSpeed = 20f;
    [SerializeField] bool centerOnMapWhenOutsideBounds = true;

    [Header("화면 모서리 이동")]
    [SerializeField] bool useEdgeMove = true;
    [SerializeField] float edgePixels = 24f;
    [SerializeField] float edgeMoveStartDelay = 0.2f;

    [Header("줌 공통")]
    [SerializeField] TopViewZoomMode zoomMode = TopViewZoomMode.FixedAngleDistance;
    [SerializeField] float zoomSpeed = 50f;
    [SerializeField] float zoomSmoothSpeed = 8f;
    [SerializeField, Range(0f, 1f)] float startZoomRate = 1f;

    [Header("줌 - 인아웃 회전")]
    [SerializeField] float nearTargetY = 5f;
    [SerializeField] float farTargetY = 20f;
    [SerializeField] float nearFollowOffsetZ = -3f;
    [SerializeField] float farFollowOffsetZ = 0f;
    [SerializeField] Vector3 nearRotOffset = Vector3.zero;
    [SerializeField] Vector3 farRotOffset = Vector3.zero;

    [Header("줌 - 고정 각도")]
    [SerializeField, Range(25f, 80f)] float fixedViewAngle = 55f;
    [SerializeField, Min(1f)] float nearZoomDistance = 10f;
    [SerializeField, Min(1f)] float farZoomDistance = 26f;
    [SerializeField] float fixedTargetY = 0f;
    [SerializeField] Vector3 fixedTargetOffset = Vector3.zero;

    IInputService inputService;

    bool active;
    bool startPositionResolved;
    float activatedTime;
    float currentZoomRate;
    float targetZoomRate;

    public CameraViewType ViewType => CameraViewType.TopView;
    public CinemachineCamera Camera => topViewCam;

    #region 생명 주기

    void Awake()
    {
        currentZoomRate = Mathf.Clamp01(startZoomRate);
        targetZoomRate = currentZoomRate;
    }

    void Start()
    {
        ResolveServices();
        ResolveStartPosition();
        ApplyZoomRate(currentZoomRate);
    }

    void LateUpdate()
    {
        if (!active) return;

        ResolveStartPosition();

        currentZoomRate = Mathf.Lerp(
            currentZoomRate,
            targetZoomRate,
            1f - Mathf.Exp(-zoomSmoothSpeed * Time.unscaledDeltaTime));

        ApplyZoomRate(currentZoomRate);
    }

    void OnValidate()
    {
        startZoomRate = Mathf.Clamp01(startZoomRate);
        nearZoomDistance = Mathf.Max(1f, nearZoomDistance);
        farZoomDistance = Mathf.Max(1f, farZoomDistance);
    }

    #endregion

    #region ICameraModule

    public void Activate()
    {
        active = true;
        activatedTime = Time.unscaledTime;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        ResolveStartPosition();
        ApplyZoomRate(currentZoomRate);
    }

    public void Deactivate()
    {
        active = false;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Move(Vector2 input)
    {
        if (!active || topViewTarget == null) return;

        Vector2 finalInput = Vector2.ClampMagnitude(input + GetEdgeMoveInput(), 1f);
        if (finalInput.sqrMagnitude < 0.0001f) return;

        Vector3 moveDelta = new(finalInput.x, 0f, finalInput.y);
        Vector3 nextPosition = topViewTarget.position + moveDelta * (moveSpeed * Time.unscaledDeltaTime);

        topViewTarget.position = ClampToMapBounds(nextPosition);
    }

    public void Zoom(float input)
    {
        if (!active || Mathf.Abs(input) < 0.01f) return;
        targetZoomRate = Mathf.Clamp01(targetZoomRate - input * zoomSpeed * Time.unscaledDeltaTime);
    }

    public void Rotate(Vector2 input)
    {
    }

    #endregion

    #region 내부 함수

    void ResolveServices()
    {
        if (inputService == null) ServiceLocator.TryGet(out inputService);
    }

    Vector2 GetEdgeMoveInput()
    {
        if (!useEdgeMove) return Vector2.zero;
        if (Time.unscaledTime - activatedTime < edgeMoveStartDelay) return Vector2.zero;

        ResolveServices();
        if (inputService == null) return Vector2.zero;

        Vector2 pointerPosition = inputService.PointerScreenPosition;
        if (pointerPosition.x < 0f || pointerPosition.y < 0f) return Vector2.zero;
        if (pointerPosition.x > Screen.width || pointerPosition.y > Screen.height) return Vector2.zero;

        Vector2 result = Vector2.zero;

        if (pointerPosition.x <= edgePixels) result.x = -1f;
        else if (pointerPosition.x >= Screen.width - edgePixels) result.x = 1f;

        if (pointerPosition.y <= edgePixels) result.y = -1f;
        else if (pointerPosition.y >= Screen.height - edgePixels) result.y = 1f;

        return result;
    }

    Vector3 ClampToMapBounds(Vector3 position)
    {
        if (!ServiceLocator.TryGet(out IMapService mapService) || !mapService.HasBounds)
            return position;

        return mapService.ClampCameraPosition(position);
    }

    void ResolveStartPosition()
    {
        if (startPositionResolved || !centerOnMapWhenOutsideBounds || topViewTarget == null)
            return;

        if (!ServiceLocator.TryGet(out IMapService mapService) || !mapService.HasBounds)
            return;

        Bounds bounds = mapService.CameraBounds;
        Vector3 position = topViewTarget.position;

        bool outsideX = position.x < bounds.min.x || position.x > bounds.max.x;
        bool outsideZ = position.z < bounds.min.z || position.z > bounds.max.z;

        position.x = bounds.center.x;
        position.z = bounds.center.z;

        topViewTarget.position = position;

        if (outsideX || outsideZ)
        {

        }

        startPositionResolved = true;
    }

    void ApplyZoomRate(float zoomRate)
    {
        if (topViewTarget == null || topViewCam == null) return;
        if (!topViewCam.TryGetComponent(out CinemachineFollow follower) ||
            !topViewCam.TryGetComponent(out CinemachineRotationComposer rotComposer)) return;

        switch (zoomMode)
        {
            case TopViewZoomMode.FixedAngleDistance:
                ApplyFixedAngleZoom(zoomRate, follower, rotComposer);
                break;

            case TopViewZoomMode.BlendTargetAndOffset:
            default:
                ApplyBlendTargetZoom(zoomRate, follower, rotComposer);
                break;
        }
    }

    void ApplyBlendTargetZoom(float zoomRate, CinemachineFollow follower, CinemachineRotationComposer rotComposer)
    {
        Vector3 targetPos = topViewTarget.position;
        targetPos.y = Mathf.Lerp(nearTargetY, farTargetY, zoomRate);
        topViewTarget.position = targetPos;

        Vector3 followOffset = follower.FollowOffset;
        followOffset.z = Mathf.Lerp(nearFollowOffsetZ, farFollowOffsetZ, zoomRate);
        follower.FollowOffset = followOffset;

        rotComposer.TargetOffset = Vector3.Lerp(nearRotOffset, farRotOffset, zoomRate);
    }

    void ApplyFixedAngleZoom(float zoomRate, CinemachineFollow follower, CinemachineRotationComposer rotComposer)
    {
        Vector3 targetPos = topViewTarget.position;
        targetPos.y = fixedTargetY;
        topViewTarget.position = targetPos;

        float distance = Mathf.Lerp(nearZoomDistance, farZoomDistance, zoomRate);
        float angleRad = fixedViewAngle * Mathf.Deg2Rad;

        follower.FollowOffset = new Vector3(
            0f,
            Mathf.Sin(angleRad) * distance,
            -Mathf.Cos(angleRad) * distance);

        rotComposer.TargetOffset = fixedTargetOffset;
    }

    #endregion
}