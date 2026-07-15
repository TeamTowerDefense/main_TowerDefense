using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveSpawner : MonoBehaviour
{
    private static CaveSpawner _instance;
    public static CaveSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<CaveSpawner>();
            }
            return _instance;
        }
    }

    [Header("VFX 프리팹 설정")]
    [SerializeField] private GameObject fogPrefab;
    [SerializeField] private GameObject rockfallPrefab;

    [Header("페이드 인/아웃 연출 시간 설정")]
    [SerializeField] private float spawnFadeInDuration = 1.0f;
    [Tooltip("최대 방출(100%)을 유지할 시간")]
    [SerializeField] private float activeDuration = 1.5f;
    [Tooltip("스폰 양이 서서히 0%로 줄어드는 시간")]
    [SerializeField] private float fadeDuration = 1.5f;

    private List<CaveEntrance> registeredEntrances = new List<CaveEntrance>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void RegisterEntrance(CaveEntrance entrance)
    {
        if (!registeredEntrances.Contains(entrance))
            registeredEntrances.Add(entrance);
    }

    public void UnregisterEntrance(CaveEntrance entrance)
    {
        if (registeredEntrances.Contains(entrance))
            registeredEntrances.Remove(entrance);
    }

    public void PlayCaveEffectAtPosition(Vector3 spawnPosition)
    {
        CaveEntrance closestEntrance = FindClosestEntrance(spawnPosition);

        if (closestEntrance != null)
        {
            PlayCaveEffect(closestEntrance);
        }
        else
        {
            PlayCaveEffectRaw(spawnPosition);
        }
    }

    private CaveEntrance FindClosestEntrance(Vector3 position)
    {
        CaveEntrance closest = null;
        float closestDistance = 1.5f;

        foreach (var entrance in registeredEntrances)
        {
            if (entrance == null) continue;

            float dist = Vector3.Distance(entrance.transform.position, position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = entrance;
            }
        }
        return closest;
    }

    private void PlayCaveEffect(CaveEntrance entrance)
    {
        if (fogPrefab == null || rockfallPrefab == null) return;

        if (entrance.activeFog != null) Destroy(entrance.activeFog);
        if (entrance.activeRockfall != null) Destroy(entrance.activeRockfall);

        Transform fogParent = entrance.fogPoint != null ? entrance.fogPoint : entrance.transform;
        Transform rockfallParent = entrance.rockfallPoint != null ? entrance.rockfallPoint : entrance.transform;

        entrance.activeFog = Instantiate(fogPrefab, fogParent.position, fogParent.rotation, fogParent);
        entrance.activeRockfall = Instantiate(rockfallPrefab, rockfallParent.position, rockfallParent.rotation, rockfallParent);

        // 생성 직후 즉시 멈춤 (Instantiate 시 자동 재생 방지)
        StopParticleSystem(entrance.activeFog);
        StopParticleSystem(entrance.activeRockfall);

        StartCoroutine(ManageEffectLifecycle(entrance.activeFog, spawnFadeInDuration, activeDuration, fadeDuration));
        StartCoroutine(ManageEffectLifecycle(entrance.activeRockfall, spawnFadeInDuration, activeDuration, fadeDuration));
    }

    private void PlayCaveEffectRaw(Vector3 spawnPosition)
    {
        if (fogPrefab == null || rockfallPrefab == null) return;

        GameObject fog = Instantiate(fogPrefab, spawnPosition, Quaternion.identity);
        GameObject rock = Instantiate(rockfallPrefab, spawnPosition + (Vector3.up * 3f), Quaternion.identity);

        // 생성 직후 즉시 멈춤
        StopParticleSystem(fog);
        StopParticleSystem(rock);

        StartCoroutine(ManageEffectLifecycle(fog, spawnFadeInDuration, activeDuration, fadeDuration));
        StartCoroutine(ManageEffectLifecycle(rock, spawnFadeInDuration, activeDuration, fadeDuration));
    }

    // 보조 함수: 파티클 시스템 즉시 정지
    private void StopParticleSystem(GameObject effect)
    {
        var systems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // 생성, 유지, 소멸을 하나로 통합한 라이프사이클 코루틴
    private IEnumerator ManageEffectLifecycle(GameObject effectInstance, float spawnTime, float activeTime, float fadeTime)
    {
        if (effectInstance == null) yield break;

        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();
        Dictionary<ParticleSystem, float> originalMultipliers = new Dictionary<ParticleSystem, float>();

        // 초기 설정: 파티클 정보 저장 및 방출량 0으로 세팅
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                originalMultipliers[ps] = ps.emission.rateOverTimeMultiplier;
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = 0f;
                ps.Play(); // 이제 코루틴 안에서 재생 시작!
            }
        }

        // 1. [Fade In] 서서히 나타나기
        float elapsed = 0f;
        while (elapsed < spawnTime)
        {
            if (effectInstance == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / spawnTime;

            foreach (var ps in particleSystems)
            {
                if (ps != null && originalMultipliers.TryGetValue(ps, out float originalVal))
                {
                    var emission = ps.emission;
                    emission.rateOverTimeMultiplier = Mathf.Lerp(0f, originalVal, t);
                }
            }
            yield return null;
        }

        // 2. [Active] 유지 시간
        yield return new WaitForSeconds(activeTime);

        // 3. [Fade Out] 서서히 사라지기
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            if (effectInstance == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            foreach (var ps in particleSystems)
            {
                if (ps != null && originalMultipliers.TryGetValue(ps, out float originalVal))
                {
                    var emission = ps.emission;
                    emission.rateOverTimeMultiplier = Mathf.Lerp(originalVal, 0f, t);
                }
            }
            yield return null;
        }

        // 4. [Cleanup] 마지막 입자 사라질 때까지 대기 후 삭제
        float maxLifetime = 1.0f;
        foreach (var ps in particleSystems)
            if (ps != null) maxLifetime = Mathf.Max(maxLifetime, ps.main.startLifetime.constantMax);

        yield return new WaitForSeconds(maxLifetime);
        if (effectInstance != null) Destroy(effectInstance);
    }
}