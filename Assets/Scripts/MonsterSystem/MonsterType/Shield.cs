using UnityEngine;
using System.Collections.Generic;

public class Shield : MonoBehaviour, IAbility
{
    [Header("능력 데이터")]
    [SerializeField] private int shieldCount = 0;

    [Header("시각 연출 설정")]
    [SerializeField] private GameObject shieldPrefab;        // 인스펙터에서 등록할 방패 프리팹
    [SerializeField] private float offsetFromMonster =0f; // 몬스터 외곽에서 떨어질 거리
    [SerializeField] private float rotationSpeed = 60f;      // 초당 회전 속도

    private GameObject shieldContainer;
    private List<GameObject> spawnedVisuals = new List<GameObject>();

    private Collider monsterCollider;
    private bool isInitialized = false;

    [Header("생성될 부모 위치 (지정 안 하면 최상위에 생성)")]
    [SerializeField] private Transform parent;

    private void Awake()
    {
        monsterCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (enabled && shieldCount > 0 && shieldContainer == null && !isInitialized)
        {
            // 추후 맞는 순간에만 시각적 쉴드를 생성하도록 변경 하면 좋음
            SpawnVisualShields();
        }

        if (shieldContainer == null) return;

        Vector3 monsterCenter = monsterCollider != null ? monsterCollider.bounds.center : transform.position;
        shieldContainer.transform.position = monsterCenter;

        shieldContainer.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    public void DisableAbility()
    {
        shieldCount = 0;
        ClearVisualShields();
        isInitialized = false;
        enabled = false;
    }

    public void EnableAbility(AbilityData data)
    {
        if (data is ShieldAbilityData shieldData)
        {
            shieldCount = shieldData.shieldCount;
            enabled = true;
            isInitialized = false;
            Debug.Log($"{gameObject.name}: {shieldCount}개의 보호막 활성화!");
        }
    }

    public bool TryUseShield()
    {
        if (!enabled || shieldCount <= 0) return false;

        shieldCount--;
        Debug.Log($"{gameObject.name}: 쉴드가 공격을 방어함! (남은 쉴드: {shieldCount})");

        RemoveOneVisualShield();

        if (shieldCount <= 0)
        {
            DisableAbility();
            Debug.Log($"{gameObject.name}: 모든 쉴드가 깨졌습니다!");
        }

        return true;
    }

    private void SpawnVisualShields()
    {
        ClearVisualShields();
        isInitialized = true;

        if (shieldPrefab == null)
        {
            Debug.LogError($"[Shield - {gameObject.name}] shieldPrefab이 인스펙터 창에 등록되지 않았습니다!");
            return;
        }

        Vector3 monsterCenter = monsterCollider != null ? monsterCollider.bounds.center : transform.position;

        shieldContainer = new GameObject($"[ShieldContainer] {gameObject.name}");
        shieldContainer.transform.position = monsterCenter;
        if (parent != null) shieldContainer.transform.SetParent(parent);

        for (int i = 0; i < shieldCount; i++)
        {
            float angleDeg = i * (360f / shieldCount);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * (GetMonsterRadius() + offsetFromMonster);
            float z = Mathf.Sin(angleRad) * (GetMonsterRadius() + offsetFromMonster);

            Vector3 spawnPos = monsterCenter + new Vector3(x, 0, z);

            GameObject visual = Instantiate(shieldPrefab, spawnPos, Quaternion.identity, shieldContainer.transform);

            if (visual != null)
            {
                visual.transform.localScale = shieldPrefab.transform.localScale;

                visual.transform.LookAt(monsterCenter);

                spawnedVisuals.Add(visual);
            }
        }
    }

    private void RemoveOneVisualShield()
    {
        if (spawnedVisuals.Count > 0)
        {
            int lastIndex = spawnedVisuals.Count - 1;
            GameObject target = spawnedVisuals[lastIndex];
            spawnedVisuals.RemoveAt(lastIndex);

            if (target != null)
            {
                Destroy(target);
            }
        }
    }

    private void ClearVisualShields()
    {
        foreach (var visual in spawnedVisuals)
        {
            if (visual != null) Destroy(visual);
        }
        spawnedVisuals.Clear();

        if (shieldContainer != null)
        {
            Destroy(shieldContainer);
            shieldContainer = null;
        }
    }

    private float GetMonsterRadius()
    {
        if (monsterCollider == null) return 0.5f;
        return Mathf.Max(monsterCollider.bounds.extents.x, monsterCollider.bounds.extents.z);
    }
}