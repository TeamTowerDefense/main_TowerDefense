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
        Debug.Log(
       $"[Sound Play 진입] " +
       $"Object={name}, " +
       $"ParameterClip={(clip != null ? clip.name : "NULL")}, " +
       $"SoundData={(soundData != null ? soundData.name : "NULL")}, " +
       $"AudioSource={(audioSource != null ? audioSource.name : "NULL")}");

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            Debug.Log(
                $"[Sound AudioSource 재탐색] " +
                $"Result={(audioSource != null ? audioSource.name : "NULL")}");
        }

        if (audioSource == null)
        {
            Debug.LogError(
                $"[Sound Play 실패] AudioSource가 없습니다. Object={name}");

            SoundManager.Instance?.Despawn(this);
            return;
        }

        if (clip == null)
        {
            Debug.LogError(
                $"[Sound Play 실패] 전달받은 AudioClip이 null입니다. " +
                $"SoundData={(soundData != null ? soundData.name : "NULL")}");

            SoundManager.Instance?.Despawn(this);
            return;
        }

        if (soundData == null)
        {
            Debug.LogError(
                $"[Sound Play 실패] SoundData가 null입니다.");

            SoundManager.Instance?.Despawn(this);
            return;
        }

        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }


        audioSource.Stop();
        audioSource.clip = clip;
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
        Debug.Log(
      $"[Sound Despawn 대기 시작] " +
      $"Object={name}, " +
      $"Clip={audioSource.clip?.name}, " +
      $"IsPlaying={audioSource.isPlaying}");


        while (audioSource.isPlaying)
            yield return null;

        Debug.Log(
       $"[Sound Despawn 실행] " +
       $"Object={name}");


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
