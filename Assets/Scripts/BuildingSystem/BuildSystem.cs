using System;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Build Sound")]
    [SerializeField] private int buildSoundID = 20001;
    [SerializeField] private int dismantleSoundID = 20002;

    public int TowerLimit { get; private set; } = 0;
    public int CurrentTowerCount { get; private set; } = 0;
    public event Action<int, int> OnTowerCountChanged;

    private SoundManager soundManager;

    private void Awake()
    {
        soundManager = GetComponent<SoundManager>();
    }

    public void SetTowerLimit(int limit)
    {
        TowerLimit = limit;
        OnTowerCountChanged?.Invoke(CurrentTowerCount, TowerLimit);
    }

    public bool CanBuildTower()
    {
        return CurrentTowerCount < TowerLimit;
    }

    // 건물 설치
    public GameObject PlaceBuilding(GameObject prefabToPlace, IGridProvider grid, Vector2Int index, Vector3 pos, Quaternion rotation)
    {
        CurrentTowerCount++;
        OnTowerCountChanged?.Invoke(CurrentTowerCount, 0);

        GameObject newObj = Instantiate(prefabToPlace, pos, rotation);

        IBuildable buildable = newObj.GetComponent<IBuildable>();
        buildable.ConstructedGrid = grid;
        buildable.ConstructedIndex = index;

        PlayBuildSound(buildSoundID, prefabToPlace.transform.position);

        return newObj;
    }

    // 건물 파괴
    public void DestroyBuilding(GameObject buildingObj)
    {

        if (CurrentTowerCount > 0)
        {
            CurrentTowerCount--;
            OnTowerCountChanged?.Invoke(CurrentTowerCount, TowerLimit);
        }

        PlayBuildSound(dismantleSoundID, buildingObj.transform.position);

        Destroy(buildingObj);
    }

    private void PlayBuildSound(int soundID, Vector3 position)
    {
        if (soundID <= 0)
        {
            Debug.LogWarning(
                $"[BuildSystem Sound] 유효하지 않은 SoundID입니다. ID={soundID}");
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogError(
                $"[BuildSystem Sound] SoundManager.Instance가 null입니다. " +
                $"SoundID={soundID}");
            return;
        }

        Debug.Log(
            $"[BuildSystem Sound] 재생 요청 " +
            $"SoundID={soundID}, Position={position}");

        SoundManager.Instance.PlaySound(soundID, position);
    }


}
