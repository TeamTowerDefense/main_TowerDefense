using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

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
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSoundTable();
        CreateInitialPool();
    }

    private void BuildSoundTable()
    {
        soundTable.Clear();

        if (soundDB == null)
        {
            Debug.LogError("[SoundManger] SoundDB가 없습니다.");
            return;
        }

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

        soundObject.gameObject.SetActive(false);
        return soundObject;
    }

    private SoundPoolObject Spawn(Vector3 position)
    {
        SoundPoolObject soundObject;

        if(soundPool.Count > 0)
        {
            soundObject = soundPool.Dequeue();
        }
        else
        {
            soundObject = CreateSoundObject();
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

        soundObject.ResetSound();
        soundObject.gameObject.SetActive(false);
        soundObject.transform.SetParent(soundParent);

        soundPool.Enqueue(soundObject);
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
