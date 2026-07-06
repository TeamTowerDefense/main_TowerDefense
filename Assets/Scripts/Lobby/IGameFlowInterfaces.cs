using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IGameFlowInterface
{
    #region 글로벌 서비스 자동 등록
    public interface IGlobalService { }
    public interface IAutoGlobalService
    {
        bool ReplaceExistingSceneService => false;
        bool LogGlobalServiceRegistration => false;

        IEnumerable<Type> GetGlobalServiceTypes()
        {
            Type markerType = typeof(IGlobalService);

            return GetType().GetInterfaces().Where(type => type != markerType && markerType.IsAssignableFrom(type));
        }
        void RegisterGlobalServices()
        {
            object self = this;

            foreach (Type serviceType in GetGlobalServiceTypes())
            {
                bool success = ServiceLocator.Register(serviceType, self, ReplaceExistingSceneService);

                if (LogGlobalServiceRegistration && success)
                    Debug.Log($"[AutoSceneService] Register {serviceType.Name} -> {GetType().Name}", self as UnityEngine.Object);
            }
        }
        void UnregisterGlobalServices()
        {
            object self = this;

            foreach (Type serviceType in GetGlobalServiceTypes())
            {
                bool success = ServiceLocator.Unregister(serviceType, self);

                if (LogGlobalServiceRegistration && success)
                    Debug.Log($"[AutoSceneService] Unregister {serviceType.Name} -> {GetType().Name}", self as UnityEngine.Object);
            }
        }
    }
    #endregion

    #region 게임 흐름 인터페이스
    public interface IGameFlowService : IGlobalService
    {
        event Action StageSelectOpenRequested;
        event Action<StageResultContext, StageProgressApplyResult> StageRunFinished;

        void FinishStageRun(StageResultContext result, LobbyOpenRequest lobbyRequest = LobbyOpenRequest.StageSelect);

        void EnterTitle();
        void EnterLobby();

        void OpenStageSelectFromLobby();

        void StartStageRun();
        void RetryStage();

        void ExitToLobby();
        void ExitToLobbyWithStageSelect();
    }

    public interface ISceneLoadService : IGlobalService
    {
        event Action<string> LoadStarted;
        event Action<float> LoadProgressChanged;
        event Action<string> LoadCompleted;

        bool IsLoading { get; }
        string CurrentSceneName { get; }
        string PendingSceneName { get; }

        void LoadScene(string sceneName, Action onLoaded = null);
        void LoadSceneWithLoading(string targetSceneName, Action onLoaded = null);
        void ReloadCurrentScene(Action onLoaded = null);
    }
    public interface ILobbyReturnContextService : IGlobalService
    {
        LobbyOpenRequest CurrentRequest { get; }

        void Request(LobbyOpenRequest request);
        LobbyOpenRequest Consume();
        void Clear();
    }
    public enum LobbyOpenRequest
    {
        None,
        StageSelect,
        PreBattleSetup
    }

    #endregion

    #region 스테이지 DB / run 컨텍스트

    public interface IStageDatabaseService : IGlobalService
    {
        bool IsLoaded { get; }
        IReadOnlyList<StageDataSO> Stages { get; }

        event Action Loaded;

        bool TryGetStage(string stageId, out StageDataSO stageData);
        StageDataSO GetFirstStage();
    }

    public interface IStageRunContextService : IGlobalService
    {
        bool HasValidRun { get; }

        string StageId { get; }
        StageDataSO StageData { get; }
        string BattleSceneName { get; }

        StageLoadoutContext Loadout { get; }

        void SetStage(StageDataSO stageData);
        void SetLoadout(StageLoadoutContext loadout);
        void Clear();
    }
    [Serializable]
    public class StageLoadoutContext
    {
        [SerializeField] List<int> selectedTowerIds = new();

        public IReadOnlyList<int> SelectedTowerIds => selectedTowerIds;
        public int Count => selectedTowerIds?.Count ?? 0;
        public StageLoadoutContext() { }
        public StageLoadoutContext(IEnumerable<int> towerIds)
        {
            selectedTowerIds = towerIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        }
        public bool Contains(int towerId) => selectedTowerIds?.Contains(towerId) ?? false;

        public void Set(IEnumerable<int> towerIds)
        {
            selectedTowerIds = towerIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        }
        public void Clear() => selectedTowerIds?.Clear();
    }
    #endregion

    #region 스테이지 진행도 저장

    public interface IStageProgressService : IGlobalService
    {
        bool IsLoaded { get; }

        void Load();
        void Save();

        bool IsStageUnlocked(StageDataSO stageData);
        bool IsStageCleared(string stageId);

        int GetStarMask(string stageId);
        StageProgressRecord GetRecord(string stageId);

        StageProgressApplyResult ApplyResult(StageDataSO stageData, StageResultContext result);
    }

    public interface ISaveService : IGlobalService
    {
        bool Exists(string saveKey);

        void Save<T>(string saveKey, T data);
        bool TryLoad<T>(string saveKey, out T data);

        T LoadOrCreate<T>(string saveKey) where T : new();

        void Delete(string saveKey);
    }

    [Serializable]
    public class StageProgressSaveData { public List<StageProgressRecord> records = new(); }
    [Serializable]
    public class StageProgressRecord
    {
        public string stageId;
        public bool isCleared;
        public int starMask;
        public int clearCount;
        public float bestBaseHpRate;
        public float bestClearTime = -1f;

        public bool HasStar(int index) => index >= 0 && index < 32 && (starMask & (1 << index)) != 0;

        public void AddStar(int index)
        {
            if (index < 0 || index >= 32) return;
            starMask |= 1 << index;
        }
    }

    public readonly struct StageProgressApplyResult
    {
        public readonly StageProgressRecord Record;
        public readonly int PreviousStarMask;
        public readonly int CurrentStarMask;
        public readonly bool IsFirstClear;
        public readonly bool ProgressChanged;

        public int NewlyEarnedStarMask => CurrentStarMask & ~PreviousStarMask;

        public StageProgressApplyResult(
            StageProgressRecord record,
            int previousStarMask,
            int currentStarMask,
            bool isFirstClear,
            bool progressChanged)
        {
            Record = record;
            PreviousStarMask = previousStarMask;
            CurrentStarMask = currentStarMask;
            IsFirstClear = isFirstClear;
            ProgressChanged = progressChanged;
        }
    }
    #endregion

    #region 전투 결과 컨텍스트
    public readonly struct StageResultContext
    {
        public readonly string StageId;
        public readonly bool Cleared;

        public readonly int CurrentBaseHp;
        public readonly int MaxBaseHp;

        public readonly float ElapsedTime;

        public readonly int BuiltTowerCount;
        public readonly int SpentResource;
        public readonly int KilledEnemyCount;
        public readonly int LeakedEnemyCount;

        public float BaseHpRate => MaxBaseHp <= 0 ? 0f : Mathf.Clamp01((float)CurrentBaseHp / MaxBaseHp);

        public StageResultContext(
                string stageId,
                bool cleared,
                int currentBaseHp,
                int maxBaseHp,
                float elapsedTime = 0f,
                int builtTowerCount = 0,
                int spentResource = 0,
                int killedEnemyCount = 0,
                int leakedEnemyCount = 0)
        {
            StageId = stageId;
            Cleared = cleared;
            CurrentBaseHp = currentBaseHp;
            MaxBaseHp = maxBaseHp;
            ElapsedTime = elapsedTime;
            BuiltTowerCount = builtTowerCount;
            SpentResource = spentResource;
            KilledEnemyCount = killedEnemyCount;
            LeakedEnemyCount = leakedEnemyCount;
        }
    }
    #endregion

}
