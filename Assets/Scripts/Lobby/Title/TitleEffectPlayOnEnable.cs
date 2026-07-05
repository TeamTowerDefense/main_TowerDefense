using UnityEngine;

public class TitleEffectPlayOnEnable : MonoBehaviour
{
    #region 인스펙터

    [SerializeField] bool includeChildren = true;
    [SerializeField] bool clearBeforePlay = true;
    [SerializeField] bool disableAfterMaxDuration = true;
    [SerializeField] float fallbackDuration = 0.6f;

    #endregion

    #region 필드

    ParticleSystem[] particles;
    float timer;
    bool playing;

    #endregion

    #region 유니티 생명주기

    void Awake()
    {
        particles = includeChildren
            ? GetComponentsInChildren<ParticleSystem>(true)
            : GetComponents<ParticleSystem>();
    }

    void OnEnable()
    {
        Play();
    }

    void Update()
    {
        if (!playing || !disableAfterMaxDuration) return;

        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;

        playing = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region 재생

    [ContextMenu("재생")]
    public void Play()
    {
        if (particles == null || particles.Length == 0) return;

        float maxDuration = 0f;

        foreach (ParticleSystem particle in particles)
        {
            if (!particle) continue;

            if (clearBeforePlay)
                particle.Clear(true);

            particle.Play(true);

            ParticleSystem.MainModule main = particle.main;
            maxDuration = Mathf.Max(maxDuration, main.duration + main.startLifetime.constantMax);
        }

        timer = maxDuration > 0f ? maxDuration : fallbackDuration;
        playing = true;
    }

    #endregion
}