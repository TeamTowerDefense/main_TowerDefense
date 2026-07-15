using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundPoolObject : MonoBehaviour
{
    private AudioSource audioSource;
    private Coroutine despawnCoroutine;

    public AudioSource AudioSource => audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Play(AudioClip clip, SoundData soundData, float volumeScale = 1f)
    {
        if (clip == null || soundData == null)
        {
            SoundManager.Instance?.Despawn(this);
            return;
        }

        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }

        audioSource.Stop();

        audioSource.volume = Mathf.Clamp01(soundData.volume * volumeScale);

        audioSource.spatialBlend = soundData.spatialBlend;
        audioSource.minDistance = soundData.minDistance;
        audioSource.maxDistance = soundData.maxDistance;
        audioSource.loop = soundData.loop;

        audioSource.Play();

        if (!soundData.loop)
        {
            despawnCoroutine = StartCoroutine(DespawnAfterPlay());
        }

    }

    public void StopSound()
    {
        if(despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }

        audioSource.Stop();
        SoundManager.Instance?.Despawn(this);
    }

    private IEnumerator DespawnAfterPlay()
    {
        while (audioSource.isPlaying)
            yield return null;

        despawnCoroutine = null;
        SoundManager.Instance?.Despawn(this);
    }

    public void ResetSound()
    {
        if(despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;
        audioSource.pitch = 1f;
        audioSource.volume = 1f;
    }

}
