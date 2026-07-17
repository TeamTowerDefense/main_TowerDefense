using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    private int currentBgmID = -1;
    [Header("Scene BGM")]
    [SerializeField] private int lobbyBgmID = 30001;
    [SerializeField] private int stageBgmID = 30002;

    [Header("SFX")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Database")]
    [SerializeField]
    private SoundDB soundDB;

    [Header("Pool")]
    [SerializeField]
    private SoundPoolObject soundPrefab;

    [SerializeField]
    private Transform soundParent;

   

    [SerializeField]
    private int initialPoolSize = 20;

    private readonly Dictionary<int, SoundData> soundTable = new Dictionary<int, SoundData>();

    private readonly Dictionary<int, AudioClip> clipTable = new Dictionary<int, AudioClip>();

    private readonly Dictionary<int, AsyncOperationHandle<AudioClip>> clipHandles = 
        new Dictionary<int, AsyncOperationHandle<AudioClip>>();

    private readonly Queue<SoundPoolObject> soundPool = new Queue<SoundPoolObject>();

    private readonly HashSet<int> loadingSoundIDs = new HashSet<int>();

    private void Awake()
    {;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log($"[SoundManager] Instance 등록 완료: {Instance.name}");
        BuildSoundTable();
        CreateInitialPool();
    }

    private void Start()
    {
        PreloadAllSounds();
        Scene currentScene = SceneManager.GetActiveScene();

        OnSceneLoaded(currentScene, LoadSceneMode.Single);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
        $"[BGM Scene] 씬 로드됨. Scene={scene.name}");

        switch (scene.name)
        {
            case "BootStrap":
            case "Loading...":
            case "Title":
            case "Lobby":
                PlayBGM(lobbyBgmID);
                break;

            case "Stage_001":
            case "Stage_002":
            case "Stage_003":
                PlayBGM(stageBgmID);
                break;

            default:
                Debug.Log(
                    $"[BGM Scene] 지정된 BGM이 없는 씬입니다. " +
                    $"Scene={scene.name}");
                break;
        }
    }

    private void BuildSoundTable()
    {
        soundTable.Clear();

        if (soundDB == null)
        {
            Debug.LogError("[SoundManger] SoundDB가 없습니다.");
            return;
        }

        Debug.Log(
        $"[SoundManager] SoundDB 등록 개수={soundDB.sounds.Count}");

        foreach (SoundData soundData in soundDB.sounds)
        {
            if (soundData == null)
                continue;

            if (soundTable.ContainsKey(soundData.soundID))
            {
                Debug.LogError($"[SoundManger] 중복 SoundID: {soundData.soundID}");
            }

            soundTable.Add(soundData.soundID, soundData);
        }

    }

    private void CreateInitialPool()
    {
        if (soundPrefab == null)
        {
            Debug.LogError("[SoundManager] SoundPrefab이 없습니다.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            SoundPoolObject soundObject = CreateSoundObject();
            soundPool.Enqueue(soundObject);
        }
    }

    private SoundPoolObject CreateSoundObject()
    {
        SoundPoolObject soundObject = Instantiate(soundPrefab, soundParent);

        AudioSource source = soundObject.AudioSource;

        if (source != null && sfxMixerGroup != null)
        {
            source.outputAudioMixerGroup = sfxMixerGroup;
        }

        soundObject.gameObject.SetActive(false);

        return soundObject;
    }

    private SoundPoolObject Spawn(Vector3 position)
    {
        Debug.Log(
       $"[Sound Spawn] 호출됨 " +
       $"Position={position}, " +
       $"PoolCount={soundPool.Count}");
        SoundPoolObject soundObject;

        if(soundPool.Count > 0)
        {
            soundObject = soundPool.Dequeue();
            Debug.Log(
           $"[Sound Spawn] 풀에서 꺼냄 " +
           $"Object={soundObject.name}");
        }
        else
        {
            soundObject = CreateSoundObject();
            Debug.Log(
           $"[Sound Spawn] 풀 부족으로 새로 생성 " +
           $"Object={(soundObject != null ? soundObject.name : "NULL")}");
        }
        if (soundObject == null)
        {
            Debug.LogError("[Sound Spawn] SoundPoolObject가 null입니다.");
            return null;
        }
        Transform soundTransform = soundObject.transform;
        soundTransform.SetParent(soundParent);
        soundTransform.position = position;
        soundTransform.rotation = Quaternion.identity;

        soundObject.gameObject.SetActive(true);
        Debug.Log(
       $"[Sound Spawn] 활성화 완료 " +
       $"Object={soundObject.name}, " +
       $"ActiveSelf={soundObject.gameObject.activeSelf}, " +
       $"ActiveHierarchy={soundObject.gameObject.activeInHierarchy}");

        return soundObject;
    }

    public void Despawn(SoundPoolObject soundObject)
    {

        if (soundObject == null)
        {
            Debug.LogError(
                "[Sound Despawn] soundObject가 null입니다.");

            return;
        }

        Debug.Log(
        $"[Sound Despawn] 비활성화 시작 " +
        $"Object={soundObject.name}, " +
        $"ActiveBefore={soundObject.gameObject.activeSelf}");

        soundObject.ResetSound();
        soundObject.gameObject.SetActive(false);
        soundObject.transform.SetParent(soundParent);

        soundPool.Enqueue(soundObject);

        Debug.Log(
       $"[Sound Despawn] 비활성화 완료 " +
       $"Object={soundObject.name}, " +
       $"ActiveAfter={soundObject.gameObject.activeSelf}, " +
       $"PoolCount={soundPool.Count}");
    }

    public void PlaySound(int soundID, Vector3 position, float volumeScale = 1f)
    {
        if (soundID <= 0)
            return;

        if(!soundTable.TryGetValue(soundID, out SoundData soundData))
        {
            Debug.LogWarning($"[SoundManager] 등록되지 않은 Sound ID: {soundID}");
            return;
        }

        if (clipTable.TryGetValue(soundID, out AudioClip loadedClip))
        {
            SpawnAndPlay(soundData, loadedClip, position, volumeScale);
            return;
        }

        StartCoroutine(LoadAndPlayCoroutine(soundData, position, volumeScale));

    }

    private IEnumerator LoadAndPlayCoroutine(SoundData soundData, Vector3 position, float volumeScale)
    {
        int soundID = soundData.soundID;

        if (loadingSoundIDs.Contains(soundID))
        {
            while (loadingSoundIDs.Contains(soundID))
                yield return null;

            if (clipTable.TryGetValue(soundID, out AudioClip cachedClip))
            {
                SpawnAndPlay(soundData, cachedClip, position, volumeScale);
            }

            yield break;
        }

        if(soundData.audioClipReference == null || ! soundData.audioClipReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"[SoundManager] AudioClip Reference가 유효하지 않습니다. ID: {soundID}");

            yield break;
        }

        loadingSoundIDs.Add(soundID);

        AsyncOperationHandle<AudioClip> handle = soundData.audioClipReference.LoadAssetAsync<AudioClip>();

        yield return handle;


        loadingSoundIDs.Remove(soundID);

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SoundManager] 사운드 로드 실패. ID : {soundID}");
            
            yield break;
        }

        AudioClip clip = handle.Result;

        clipTable[soundID] = clip;
        clipHandles[soundID] = handle;

        SpawnAndPlay(soundData, clip, position, volumeScale);
    }

    private void SpawnAndPlay(SoundData soundData, AudioClip clip, Vector3 position, float volumeScale)
    {
        SoundPoolObject soundObject = Spawn(position);

        soundObject.Play(clip, soundData, volumeScale);

    }

    public SoundPoolObject PlayLoopSound(int soundID, Vector3 position, float volumeScale = 1f)
    {
        if (soundID < 0) 
            return null;

        if (!soundTable.TryGetValue(soundID, out SoundData soundData))
        {
            return null;
        }

        if (!clipTable.TryGetValue(soundID, out AudioClip clip))
        {
            Debug.LogWarning($"루프 사운드는 미리 로드되어야 합니다. ID: {soundID}");
            return null;
        }

        SoundPoolObject soundObject = Spawn(position);

        soundObject.Play(clip, soundData, volumeScale);

        return soundObject;

    }
    public void PreloadAllSounds()
    {
        foreach (SoundData soundData in soundTable.Values)
        {
            if (soundData == null)
                continue;

            if (clipTable.ContainsKey(soundData.soundID))
                continue;

            StartCoroutine(
                PreloadSoundCoroutine(soundData));
        }
    }

    private IEnumerator PreloadSoundCoroutine(SoundData soundData)
    {
        int soundID = soundData.soundID;

        if (loadingSoundIDs.Contains(soundID))
            yield break;

        if (soundData.audioClipReference == null || !soundData.audioClipReference.RuntimeKeyIsValid())
        {
            yield break;
        }

        loadingSoundIDs.Add(soundID);

        AsyncOperationHandle<AudioClip> handle = soundData.audioClipReference.LoadAssetAsync<AudioClip>();

        yield return handle;

        loadingSoundIDs.Remove(soundID);

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"사운드 사전 로드 실패: {soundID}");

            yield break;
        }

        clipTable[soundID] = handle.Result;
        clipHandles[soundID] = handle;
    }

    public void PlayBGM(int soundID)
    {
        if(soundID <= 0)
        {
            Debug.LogWarning($"[BGM] 유효하지 않은 ID입니다. ID={soundID}");
            return;
        }
        if (bgmSource == null)
        {
            Debug.LogError(
                "[BGM] BGM AudioSource가 연결되지 않았습니다.");
            return;
        }

        if (currentBgmID == soundID && bgmSource.isPlaying)
        {
            Debug.Log($"[BGM] 이미 재생 중입니다. ID={soundID}");
            return;
        }

        if (!soundTable.TryGetValue(soundID, out SoundData soundData))
        {
            Debug.LogError($"[BGM] SoundTable에 ID가 없습니다. ID={soundID}");
            return;
        }

        if (clipTable.TryGetValue(soundID, out AudioClip loadedClip))
        {
            ApplyAndPlayBGM(soundData, loadedClip);

            return;
        }

        StartCoroutine(LoadAndPlayBGMCoroutine(soundData));

    }

    private IEnumerator LoadAndPlayBGMCoroutine(SoundData soundData)
    {
        if (soundData == null)
            yield break;

        int soundID = soundData.soundID;

        if (soundData.audioClipReference == null || !soundData.audioClipReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"[BGM] Addressable Reference가 유효하지 않습니다. ID={soundID}");
            yield break;
        }

        if (loadingSoundIDs.Contains(soundID))
        {
            while(loadingSoundIDs.Contains(soundID))
                yield return null;

            if (clipTable.TryGetValue(soundID, out AudioClip cachedClip))
            {
                ApplyAndPlayBGM(soundData, cachedClip);
            }

            yield break;
        }

        loadingSoundIDs.Add(soundID);

        AsyncOperationHandle<AudioClip> handle = soundData.audioClipReference.LoadAssetAsync<AudioClip>();

        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[BGM] AudioClip 로드 실패. ID={soundID}, Exception={handle.OperationException}");
            yield break;
        }

        AudioClip clip = handle.Result;

        if (clip == null)
        {
            Debug.LogError($"[BGM] 로드 결과가 null입니다. ID={soundID}");
            yield break;
        }
        clipTable[soundID] = clip;
        clipHandles[soundID] = handle;

        ApplyAndPlayBGM(soundData, clip);
    }

    private void ApplyAndPlayBGM(SoundData soundData, AudioClip clip)
    {
        if (soundData == null || soundData == null || clip == null)
            return;

        bgmSource.Stop();

        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.volume = soundData.volume;
        bgmSource.pitch = 1f;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        if (bgmMixerGroup != null)
        {
            bgmSource.outputAudioMixerGroup =
                bgmMixerGroup;
        }

        currentBgmID = soundData.soundID;

        bgmSource.Play();

        Debug.Log($"[BGM] 재생 시작 ID={currentBgmID}, Clip={clip.name}, " +
            $"Volume={bgmSource.volume}, IsPlaying={bgmSource.isPlaying}");
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmID = -1;

        Debug.Log("[BGM] 재생 정지");
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        foreach (AsyncOperationHandle<AudioClip> handle in clipHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        clipHandles.Clear();
        clipTable.Clear();
    }

}
