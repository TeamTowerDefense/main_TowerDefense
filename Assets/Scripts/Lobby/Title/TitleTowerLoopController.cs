using UnityEngine;

public class TitleTowerLoopController : MonoBehaviour
{
    [System.Serializable]
    public class TowerPresenter
    {
        public string name;
        public Transform root;
        public float phaseOffset;
        public float positionAmount = 0.035f;
        public float scaleAmount = 0.035f;
        public float rotateAmount = 1.5f;
    }

    [Header("타워 목록")]
    [SerializeField] TowerPresenter[] towers;
    [Header("루프 시작")]
    [SerializeField] bool playOnEnable = true;
    [SerializeField] float speed = 2.2f;

    bool playing;
    Vector3[] basePos;
    Vector3[] baseScales;
    Quaternion[] baseRot;

    #region 생명주기
    private void OnEnable()
    {
        if (playOnEnable) StartIdle();
    }
    private void OnDisable()
    {
        if (playOnEnable) StopIdle();
    }
    private void Update()
    {
        if (!playing || towers == null) return;

        float time = Time.unscaledTime * speed;

        for (int i = 0; i < towers.Length; i++)
        {
            TowerPresenter tp = towers[i];
            if (!tp.root) continue;

            float wave = Mathf.Sin(time + tp.phaseOffset);
            float softWave = (wave + 1f) * 0.5f;

            tp.root.localPosition = basePos[i] + Vector3.up * (wave * tp.positionAmount);
            tp.root.localScale = baseScales[i] * (1f + softWave * tp.scaleAmount);
            tp.root.localRotation = baseRot[i] * Quaternion.Euler(0f, 0f, wave * tp.rotateAmount);
        }
    }
    #endregion




    [ContextMenu("대기 시작")]
    public void StartIdle()
    {
        CacheBasePose();
        playing = true;
    }

    [ContextMenu("대기 정지")]
    public void StopIdle()
    {
        if (!playing) return;

        playing = false;
        RestoreBasePose();
    }

    void CacheBasePose()
    {
        int count = towers?.Length ?? 0;

        basePos = new Vector3[count];
        baseScales = new Vector3[count];
        baseRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            Transform root = towers[i].root;
            if (!root) continue;

            basePos[i] = root.localPosition;
            baseScales[i] = root.localScale;
            baseRot[i] = root.localRotation;
        }
    }
    void RestoreBasePose()
    {
        if (towers == null || basePos == null) return;

        for (int i = 0;i < towers.Length;i++)
        {
            Transform root = towers[i].root;
            if (!root) continue;

            if (i < basePos.Length) root.localPosition = basePos[i];
            if (i < baseScales.Length) root.localScale = baseScales[i];
            if (i < baseRot.Length) root.localRotation = baseRot[i];
        }
    }
}
