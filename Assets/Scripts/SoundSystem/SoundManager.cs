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


    private int currentBgmID = -1;
    private int requestedBgmID = -1;

    private readonly Dictionary<int, SoundData> soundTable = new Dictionary<int, SoundData>();
    private readonly Dictionary<int, AudioClip> clipTable = new Dictionary<int, AudioClip>();

    private readonly Dictionary<string, AudioClip> assetClipTable = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> assetHandleTable = 
        new Dictionary<string, AsyncOperationHandle<AudioClip>>();
    private readonly HashSet<string> loadingAssetKeys = new HashSet<string>();

    private readonly Dictionary<int, AsyncOperationHandle<AudioClip>> clipHandles = 
        new Dictionary<int, AsyncOperationHandle<AudioClip>>();

    private readonly Queue<SoundPoolObject> soundPool = new Queue<SoundPoolObject>();
    private readonly HashSet<SoundPoolObject> pooledObjects = new HashSet<SoundPoolObject>();

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

    private IEnumerator Start()
    {
        yield return PreloadAllSoundsCoroutine();
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        StopAllCoroutines();

        foreach (AsyncOperationHandle<AudioClip> handle in assetHandleTable.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        assetHandleTable.Clear();
        assetClipTable.Clear();
        clipTable.Clear();
        soundTable.Clear();
        loadingAssetKeys.Clear();

        soundPool.Clear();
        pooledObjects.Clear();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
        $"[BGM Scene] 씬 로드됨. Scene={scene.name}");

        switch (scene.name)
        {
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
        if (soundDB.sounds == null)
        {
            Debug.LogError("[SoundManager] SoundDB.sounds가 null입니다.");
            return;
        }

        foreach (SoundData soundData in soundDB.sounds)
        {
            if (soundData == null)
            {
                Debug.LogWarning("[SoundManager] SoundDB에 null 데이터가 있습니다.");
                continue;
            }

            if (soundData.soundID <= 0)
            {
                Debug.LogWarning($"[SoundManager] 유효하지 않은 SoundID입니다. Data={soundData.name}, ID={soundData.soundID}");
                continue;
            }

            if (soundTable.ContainsKey(soundData.soundID))
            {
                Debug.LogError(
                    $"[SoundManager] 중복 SoundID가 있습니다. ID={soundData.soundID}, Data={soundData.name}");
                continue;
            }

            soundTable.Add(soundData.soundID, soundData);
        }

    }
    private bool TryGetSoundData(int soundID,  out SoundData soundData)
    {
        soundData = null;

        if (soundID <= 0)
        {
            Debug.LogWarning(
                $"[SoundManager] 유효하지 않은 ID입니다. ID={soundID}");
            return false;
        }

        if (!soundTable.TryGetValue(soundID, out soundData))
        {
            //Debug.LogWarning($"[SoundManager] 등록되지 않은 Sound ID입니다. ID={soundID}");
            return false;
        }

        if (soundData == null)
        {
            Debug.LogError($"[SoundManager] SoundData가 null입니다. ID={soundID}");
            return false;
        }

        return true;
    }

    private bool TryGetRuntimeKey(SoundData soundData, out string runtimeKey)
    {
        runtimeKey = null;

        if (soundData == null)
            return false;

        if (soundData.audioClipReference == null)
        {
            Debug.LogError($"[SoundManager] AudioClip Reference가 null입니다. ID={soundData.soundID}, Data={soundData.name}");
            return false;
        }

        if (!soundData.audioClipReference.RuntimeKeyIsValid())
        {
            Debug.LogError( $"[SoundManager] RuntimeKey가 유효하지 않습니다. ID={soundData.soundID}, Data={soundData.name}");
            return false;
        }

        object keyObject =
            soundData.audioClipReference.RuntimeKey;

        if (keyObject == null)
        {
            Debug.LogError($"[SoundManager] RuntimeKey가 null입니다. ID={soundData.soundID}");
            return false;
        }

        runtimeKey = keyObject.ToString();

        if (string.IsNullOrWhiteSpace(runtimeKey))
        {
            Debug.LogError($"[SoundManager] RuntimeKey 문자열이 비어 있습니다. ID={soundData.soundID}");
            return false;
        }

        return true;
    }

    private IEnumerator EnsureClipLoaded(
       SoundData soundData)
    {
        if (soundData == null)
            yield break;

        int soundID = soundData.soundID;
  
        if (clipTable.TryGetValue(
                soundID,
                out AudioClip idCachedClip) &&
            idCachedClip != null)
        {
            yield break;
        }

        if (!TryGetRuntimeKey(
                soundData,
                out string runtimeKey))
        {
            yield break;
        }

        if (assetClipTable.TryGetValue(
                runtimeKey,
                out AudioClip assetCachedClip) &&
            assetCachedClip != null)
        {
            clipTable[soundID] = assetCachedClip;
            yield break;
        }

        /*
         * 다른 SoundData 또는 다른 호출 경로에서 같은 에셋을
         * 로드 중이라면 완료될 때까지 기다립니다.
         */
        if (loadingAssetKeys.Contains(runtimeKey))
        {
            while (loadingAssetKeys.Contains(runtimeKey))
                yield return null;

            if (assetClipTable.TryGetValue(runtimeKey, out assetCachedClip) && assetCachedClip != null)
            {
                clipTable[soundID] = assetCachedClip;
            }
            else
            {
                Debug.LogError($"[SoundManager] 로드 대기 후 Clip이 없습니다. ID={soundID}, Key={runtimeKey}");
            }

            yield break;
        }

        loadingAssetKeys.Add(runtimeKey);

        AsyncOperationHandle<AudioClip> handle;

        try
        {
            handle = Addressables.LoadAssetAsync<AudioClip>(soundData.audioClipReference.RuntimeKey);
        }
        catch (Exception exception)
        {
            loadingAssetKeys.Remove(runtimeKey);

            Debug.LogError($"[SoundManager] Addressable 로드 요청 예외\n" +
                $"ID={soundID}, Key={runtimeKey}, Data={soundData.name}\n" +
                $"{exception}");

            yield break;
        }

        yield return handle;

        loadingAssetKeys.Remove(runtimeKey);

        if (!handle.IsValid())
        {
            Debug.LogError($"[SoundManager] 유효하지 않은 Handle입니다. ID={soundID}, Key={runtimeKey}");

            yield break;
        }

        if (handle.Status !=
            AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SoundManager] AudioClip 로드 실패\n" +
                $"ID={soundID}, Key={runtimeKey}, Data={soundData.name}\n" +
                $"Exception={handle.OperationException}");

            if (handle.IsValid())
                Addressables.Release(handle);

            yield break;
        }

        AudioClip loadedClip = handle.Result;

        if (loadedClip == null)
        {
            Debug.LogError($"[SoundManager] 로드는 성공했지만 Clip이 null입니다. ID={soundID}, Key={runtimeKey}");
            Addressables.Release(handle);
            yield break;
        }

        assetHandleTable[runtimeKey] = handle;
        assetClipTable[runtimeKey] = loadedClip;
        clipTable[soundID] = loadedClip;

        Debug.Log($"[SoundManager] AudioClip 로드 완료. ID={soundID}, Clip={loadedClip.name}");
    }

    public void PreloadAllSounds()
    {
        StartCoroutine(PreloadAllSoundsCoroutine());
    }
    public IEnumerator PreloadAllSoundsCoroutine()
    {
        foreach (SoundData soundData in soundTable.Values)
        {
            if (soundData == null)
                continue;

            yield return EnsureClipLoaded(soundData);
        }

        Debug.Log($"[SoundManager] 전체 사운드 Preload 완료. SoundIDClipCount={clipTable.Count}, AssetClipCount={assetClipTable.Count}");
    }

    private void CreateInitialPool()
    {
        if (soundPrefab == null)
        {
            Debug.LogError("[SoundManager] SoundPrefab이 없습니다.");
            return;
        }

        if (soundParent == null)
        {
            Debug.LogWarning("[SoundManager] SoundParent가 null입니다. SoundManager Transform을 사용합니다.");
            soundParent = transform;
        }
        int createCount = Mathf.Max(0, initialPoolSize);

        for (int i = 0; i < initialPoolSize; i++)
        {
            SoundPoolObject soundObject = CreateSoundObject();

            if (soundObject == null)
                continue;

            soundPool.Enqueue(soundObject);
            pooledObjects.Add(soundObject);
        }
    }

    private SoundPoolObject CreateSoundObject()
    {
        if (soundPrefab == null)
            return null;

        SoundPoolObject soundObject = Instantiate(soundPrefab, soundParent);

        if (soundObject == null)
        {
            Debug.LogError("[SoundManager] SoundPoolObject 생성에 실패했습니다.");
            return null;
        }

        AudioSource source = soundObject.AudioSource;

        if (source == null)
        {
            Debug.LogError($"[SoundManager] SoundPoolObject에 AudioSource가 없습니다. Object={soundObject.name}");
        }
        else if (sfxMixerGroup != null)
        {
            source.outputAudioMixerGroup = sfxMixerGroup;
        }

        soundObject.gameObject.SetActive(false);

        return soundObject;
    }

    private SoundPoolObject Spawn(Vector3 position)
    {
        SoundPoolObject soundObject = null;

        while (soundPool.Count > 0 &&
               soundObject == null)
        {
            soundObject = soundPool.Dequeue();

            if (soundObject != null)
                pooledObjects.Remove(soundObject);
        }

        if (soundObject == null)
        {
            soundObject = CreateSoundObject();
        }

        if (soundObject == null)
        {
            Debug.LogError("[SoundManager] SoundPoolObject Spawn 실패");
            return null;
        }

        Transform soundTransform = soundObject.transform;

        soundTransform.SetParent(soundParent);
        soundTransform.position = position;
        soundTransform.rotation = Quaternion.identity;

        soundObject.gameObject.SetActive(true);

        return soundObject;
    }

    public void Despawn(SoundPoolObject soundObject)
    {
        if (soundObject == null)
            return;

        if (pooledObjects.Contains(soundObject))
        {
            Debug.LogWarning($"[SoundManager] 이미 풀에 반환된 객체입니다. Object={soundObject.name}");
            return;
        }

        soundObject.ResetSound();

        soundObject.transform.SetParent(soundParent);
        soundObject.gameObject.SetActive(false);

        pooledObjects.Add(soundObject);
        soundPool.Enqueue(soundObject);
    }

    public void PlaySound(int soundID, Vector3 position, float volumeScale = 1f)
    {
        if (!TryGetSoundData(soundID, out SoundData soundData))
        {
            return;
        }

        StartCoroutine(PlaySoundCoroutine(soundData, position, volumeScale));

    }

    private IEnumerator PlaySoundCoroutine(SoundData soundData, Vector3 position, float volumeScale)
    {
        yield return EnsureClipLoaded(soundData);

        if (!clipTable.TryGetValue(soundData.soundID, out AudioClip clip) || clip == null)
        {
            Debug.LogError($"[SoundManager] 재생할 Clip이 없습니다. ID={soundData.soundID}");
            yield break;
        }

        SpawnAndPlay(soundData, clip, position, volumeScale);
    }

    public SoundPoolObject PlayLoopSound(int soundID, Vector3 position, float volumeScale = 1f)
    {
        if (soundID <= 0)
            return null;

        if (!TryGetSoundData(soundID, out SoundData soundData))
        {
            return null;
        }

        if (!clipTable.TryGetValue(soundID, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"[SoundManager] 루프 사운드가 아직 로드되지 않았습니다. ID={soundID}");

            StartCoroutine(EnsureClipLoaded(soundData));

            return null;
        }

        SoundPoolObject soundObject = Spawn(position);

        if (soundObject == null)
            return null;

        soundObject.Play(clip, soundData, volumeScale);

        return soundObject;
    }

    public void PlayBGM(int soundID)
    {
        if (!TryGetSoundData(soundID, out SoundData soundData))
        {
            return;
        }

        if (bgmSource == null)
        {
            Debug.LogError("[SoundManager] BGM AudioSource가 연결되지 않았습니다.");
            return;
        }

        if (currentBgmID == soundID &&
            bgmSource.isPlaying)
        {
            Debug.Log($"[BGM] 같은 곡을 유지합니다. ID={soundID}, Time={bgmSource.time:F2}");

            return;
        }

        if (requestedBgmID == soundID)
        {
            Debug.Log($"[BGM] 이미 요청 또는 로드 중입니다. ID={soundID}");
            return;
        }

        requestedBgmID = soundID;

        StartCoroutine(LoadAndPlayBGMCoroutine(soundData, soundID));
    }

    private IEnumerator LoadAndPlayBGMCoroutine(SoundData soundData, int requestID)
    {
        yield return EnsureClipLoaded(soundData);

        if (requestedBgmID != requestID)
        {
            yield break;
        }

        if (!clipTable.TryGetValue(requestID, out AudioClip clip) || clip == null)
        {
            Debug.LogError($"[BGM] 재생할 Clip이 없습니다. ID={requestID}");

            requestedBgmID = -1;
            yield break;
        }

        ApplyAndPlayBGM(soundData, clip);
    }

    private void SpawnAndPlay(SoundData soundData, AudioClip clip, Vector3 position, float volumeScale)
    {
        SoundPoolObject soundObject = Spawn(position);

        soundObject.Play(clip, soundData, volumeScale);

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

}
