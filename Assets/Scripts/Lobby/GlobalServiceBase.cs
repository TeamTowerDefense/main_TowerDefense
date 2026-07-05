using IGameFlowInterface;
using UnityEngine;

public class GlobalServiceBase : MonoBehaviour, IAutoGlobalService
{
    [field: SerializeField] public virtual bool isDontDestroyOnLoad { get; protected set; } =  true;
    protected virtual void Awake()
    {
        ((IAutoGlobalService)this).RegisterGlobalServices();
        OnAwake();

        if (isDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnAwake() { }
    protected virtual void OnDestroy()
    {
        ((IAutoGlobalService)this).UnregisterGlobalServices();
        OnDestroyed();
    }
    protected virtual void OnDestroyed() { }
}
