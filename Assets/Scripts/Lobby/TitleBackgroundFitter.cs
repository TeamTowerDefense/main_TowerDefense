using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class TitleBackgroundFitter : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField, Min(0f)] float extraWorldMargin = 0.25f;
    [SerializeField, Min(0.1f)] float artworkAspect = 4f / 3f;

    RectTransform rectTransform;
    int lastScreenWidth = -1;
    int lastScreenHeight = -1;
    float lastOrthographicSize = -1f;

    void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        Fit();
    }

    void OnValidate()
    {
        rectTransform = (RectTransform)transform;
        Fit();
    }

    void LateUpdate()
    {
        Camera cameraToUse = ResolveCamera();
        if (cameraToUse == null || !cameraToUse.orthographic) return;

        if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height ||
            !Mathf.Approximately(lastOrthographicSize, cameraToUse.orthographicSize)) Fit();
    }

    Camera ResolveCamera()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        return targetCamera;
    }

    void Fit()
    {
        Camera cameraToUse = ResolveCamera();
        if (cameraToUse == null || !cameraToUse.orthographic) return;
        if (rectTransform == null) rectTransform = (RectTransform)transform;

        float visibleHeight = cameraToUse.orthographicSize * 2f + extraWorldMargin * 2f;
        float visibleWidth = visibleHeight * cameraToUse.aspect;
        float width;
        float height;

        if (cameraToUse.aspect >= artworkAspect)
        {
            width = visibleWidth;
            height = width / artworkAspect;
        }
        else
        {
            height = visibleHeight;
            width = height * artworkAspect;
        }

        rectTransform.sizeDelta = new Vector2(width, height);

        Vector3 position = rectTransform.position;
        position.x = cameraToUse.transform.position.x;
        position.y = cameraToUse.transform.position.y;
        rectTransform.position = position;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastOrthographicSize = cameraToUse.orthographicSize;
    }
}
