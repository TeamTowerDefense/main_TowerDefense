using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static IGameInterface.ITowerInfoProvider;
namespace IGameInterface
{
    public interface ISceneService { }
    public interface IAutoSceneService
    {
        bool ReplaceExistingSceneService => false;
        bool LogSceneServiceRegistration => false;

        IEnumerable<Type> GetSceneServiceTypes()
        {
            Type markerType = typeof(ISceneService);

            return GetType().GetInterfaces().Where(type => type != markerType && markerType.IsAssignableFrom(type));
        }
        void RegisterSceneServices()
        {
            object self = this;

            foreach(Type serviceType in GetSceneServiceTypes())
            {
                bool success = ServiceLocator.Register(serviceType, self, ReplaceExistingSceneService);

                if (LogSceneServiceRegistration && success)
                    Debug.Log($"[AutoSceneService] Register {serviceType.Name} -> {GetType().Name}", self as UnityEngine.Object);
            }
        }
        void UnregisterSceneServices()
        {
            object self = this;

            foreach (Type serviceType in GetSceneServiceTypes())
            {
                bool success = ServiceLocator.Unregister(serviceType, self);

                if (LogSceneServiceRegistration && success)
                    Debug.Log($"[AutoSceneService] Unregister {serviceType.Name} -> {GetType().Name}", self as UnityEngine.Object);
            }
        }
    }
    public interface ICameraService : ISceneService
    {
        CameraViewType CurrentView { get; }
        CinemachineCamera CurrentCam { get; }

        void SetView(CameraViewType viewType);
        void Move(Vector2 input);
        void Zoom(float input);
        void Rotate(Vector2 input);

        void Focus(ICameraFocusTarget target, CameraViewType viewType = CameraViewType.TargetView);
    }

    public readonly struct GameInputEvent
    {
        public readonly string MapName;
        public readonly string ActionName;
        public readonly string FullName;
        public readonly InputActionPhase Phase;
        public readonly double Time;

        public readonly bool ButtonValue;
        public readonly float FloatValue;
        public readonly Vector2 Vector2Value;
        public readonly Vector3 Vector3Value;

        public GameInputEvent(InputAction.CallbackContext context)
        {
            InputAction action = context.action;

            MapName = action?.actionMap?.name ?? string.Empty;
            ActionName = action?.name ?? string.Empty;
            FullName = string.IsNullOrEmpty(MapName) ? ActionName : $"{MapName}/{ActionName}";
            Phase = context.phase;
            Time = context.time;

            ButtonValue = false;
            FloatValue = 0f;
            Vector2Value = Vector2.zero;
            Vector3Value = Vector3.zero;

            Type valueType = context.valueType;

            if (valueType == typeof(float))
            {
                FloatValue = context.ReadValue<float>();
                ButtonValue = context.ReadValueAsButton();
            }
            else if (valueType == typeof(Vector2))
            {
                Vector2Value = context.ReadValue<Vector2>();
            }
            else if (valueType == typeof(Vector3))
            {
                Vector3Value = context.ReadValue<Vector3>();
            }
            else
            {
                ButtonValue = context.ReadValueAsButton();
            }
        }

        public bool Is(string mapName, string actionName)
            => MapName == mapName && ActionName == actionName;

        public bool IsStarted(string mapName, string actionName)
            => Phase == InputActionPhase.Started && Is(mapName, actionName);

        public bool IsPerformed(string mapName, string actionName)
            => Phase == InputActionPhase.Performed && Is(mapName, actionName);

        public bool IsCanceled(string mapName, string actionName)
            => Phase == InputActionPhase.Canceled && Is(mapName, actionName);
    }

    public interface IInputService : ISceneService
    {
        InputActionAsset Actions { get; }
        GameInputMode CurrentMode { get; }

        Vector2 PointerScreenPosition { get; }
        Vector2 PointerDelta { get; }
        Vector2 ScrollDelta { get; }

        event Action<GameInputEvent> Started;
        event Action<GameInputEvent> Performed;
        event Action<GameInputEvent> Canceled;
        event Action<GameInputEvent> AnyTriggered;

        bool EnableMap(string mapName);
        bool DisableMap(string mapName);
        void EnableOnly(string mapName);
        void EnableMaps(params string[] mapNames);
        void SetMode(GameInputMode mode);
        void DisableAllMaps();

        bool TryGetAction(string mapName, string actionName, out InputAction action);

        bool IsPressed(string mapName, string actionName);
        bool WasPressedThisFrame(string mapName, string actionName);
        bool WasReleasedThisFrame(string mapName, string actionName);

        float ReadFloat(string mapName, string actionName);
        Vector2 ReadVector2(string mapName, string actionName);
        Vector3 ReadVector3(string mapName, string actionName);
    }

    public interface IWorldInteractable
    {

    }

    public interface IMapService : ISceneService
    {
        Bounds MapBounds { get; }
        Bounds CameraBounds { get; }
        bool HasBounds { get; }

        IReadOnlyList<TowerInfo> Towers { get; }
        IReadOnlyList<EnemyInfo> Enemies { get; }

        int AliveTowerCount { get; }
        int AliveEnemyCount { get; }

        void Register(object provider);
        void Unregister(object provider);

        bool TryGetEnemy(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo enemy);


        Vector3 ClampCameraPosition(Vector3 position);
        bool ContainsWorldPosition(Vector3 worldPos);
    }

    public enum EnemyTargetMode
    {
        ClosestToTower,
        FarthestFromTower,
        FrontMost,
        BackMost
    }

    public interface IGameService : ISceneService
    {

    }

    public interface ICameraModule
    {
        CameraViewType ViewType { get; }
        CinemachineCamera Camera { get; }

        void Activate();
        void Deactivate();

        void Move(Vector2 input);
        void Zoom(float input);
        void Rotate(Vector2 input);
    }

    public interface ICameraFocusModule : ICameraModule
    {
        void Focus(ICameraFocusTarget target);
    }

    public interface ICameraFocusTarget
    {
        Transform CameraTarget { get; }
        Transform LookTarget { get; }
        Vector3 FocusForward { get; }
    }

    public enum CameraViewType
    {
        TopView,
        TargetView,
        PositionView,
        TurretView
    }

    public interface IInfoProvider<T> { bool TryGetInfo(out T info); T Info { get; } }
    public interface IMapInfoProvider : IInfoProvider<MapInfo> { }

    public interface ITowerInfoProvider : IInfoProvider<TowerInfo>
    {
        bool TryGetTowerInfo(out TowerInfo info);
        bool IInfoProvider<TowerInfo>.TryGetInfo(out TowerInfo info) => TryGetTowerInfo(out info);
        TowerInfo IInfoProvider<TowerInfo>.Info => TryGetTowerInfo(out TowerInfo info) ? info : null;
    }

    public interface IEnemyInfoProvider : IInfoProvider<EnemyInfo>
    {
        bool TryGetEnemyInfo(out EnemyInfo info);
        bool IInfoProvider<EnemyInfo>.TryGetInfo(out EnemyInfo info) => TryGetEnemyInfo(out info);
        EnemyInfo IInfoProvider<EnemyInfo>.Info => TryGetEnemyInfo(out EnemyInfo info) ? info : null;
    }

    public interface IEnemyInfoWriter
    {
        void SetAlive(bool value);
        void SetTargetable(bool value);
        void SetPathProgress(float value);
        void SetAttackTarget(IAttackTarget target);
    }

    public interface IAttackTarget
    {
        Transform TargetTransform { get; }
        bool CanBeDamaged { get; }

        void TakeDamage(float damage);
    }

    public class TowerInfo
    {
        Vector3 fallbackPos;

        public Transform Transform;
        public bool IsAlive;
        public bool IsPlaced;

        public Vector3 Position => Transform ? Transform.position : fallbackPos;

        public TowerInfo(Transform transform, Vector3 position, bool isAlive = true, bool isPlaced = true)
        {
            Transform = transform;
            fallbackPos = position;
            IsAlive = isAlive;
            IsPlaced = isPlaced;
        }
    }

    public class EnemyInfo
    {
        Vector3 fallbackPosition;

        public Transform Transform;
        public IAttackTarget AttackTarget;

        public bool IsAlive;
        public bool IsTargetable;
        public float PathProgress;

        public Vector3 Position => Transform ? Transform.position : fallbackPosition;
        public bool CanBeTargeted => IsAlive && IsTargetable && Transform;

        public EnemyInfo(
            Transform transform,
            Vector3 position,
            bool isAlive = true,
            bool isTargetable = true,
            float pathProgress = 0f,
            IAttackTarget attackTarget = null)
        {
            Transform = transform;
            fallbackPosition = position;
            IsAlive = isAlive;
            IsTargetable = isTargetable;
            PathProgress = pathProgress;
            AttackTarget = attackTarget;
        }
    }

    public class MapInfo
    {
        public Bounds MapBounds;
        public Bounds CameraBounds;
        public bool HasBounds;

        public MapInfo(Bounds mapBounds, Bounds cameraBounds, bool hasBounds = true)
        {
            MapBounds = mapBounds;
            CameraBounds = cameraBounds;
            HasBounds = hasBounds;
        }
    }

    public interface ITowerTargetFinder
    {
        EnemyInfo CurrentTarget { get; }
        EnemyTargetMode ChaseMode { get; }
        bool HasTarget { get; }

        bool TryGetTarget(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo target);
        bool TryGetTarget(Vector3 origin, float range, out EnemyInfo target);
        bool IsValidTarget(EnemyInfo target, Vector3 origin, float range);
        void SetChaseMode(EnemyTargetMode mode);
        void ClearTarget();
    }

    public enum StageState
    {
        None,
        Prepare,
        Playing,
        WaveClear,
        StageClear,
        StageFailed
    }

    public interface IMonsterSpawnManager : ISceneService
    {
        bool IsSpawning { get; }
        bool SpawnFinished { get; }

        void StartWave(MonsterSpawnDataSO spawnData);
        void StopWave();
    }

    public interface IStageDamageSource
    {
        int LeakDamage { get; }
    }
    public interface IStageService : ISceneService
    {
        StageState CurrentState { get; }
        int CurrentBaseHp { get; }
        int MaxBaseHp { get; }
        int TowerLimit { get; }

        event Action<StageState> StateChanged;
        event Action<int, int> BaseHpChanged;

        void StartStage();
        void StartWave();
        void TakeBaseDamage(int damage);
    }


}
