using UnityEngine;

public class HpBar : MonoBehaviour
{
    [SerializeField] private Transform fillTransform; 
    private Vector3 originalScale;
    [SerializeField]
    private Camera mainCam;


    void Awake()
    {
        originalScale = fillTransform.localScale;
        mainCam = Camera.main;
    }

    public void UpdateHp(float ratio) // 0.0f ~ 1.0f
    {
        fillTransform.localScale = new Vector3(originalScale.x * Mathf.Clamp01(ratio),
                                              originalScale.y,
                                              originalScale.z);
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        transform.rotation = Camera.main.transform.rotation;
    }
}