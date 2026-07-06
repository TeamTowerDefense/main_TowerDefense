using IGameInterface;
using System;
using UnityEngine;

public class ResourceSystem : MonoBehaviour, IResourceSystem, IAutoSceneService
{
    public event Action<int> OnResourceChanged;

    [Header("자원 설정")]
    [SerializeField] private int currentResource = 1000;

    public int CurrentResource => currentResource;

    private void Awake()
    {
        ((IAutoSceneService)this).RegisterSceneServices();
    }

    private void Start()
    {
        InitExternal();
    }

    private void OnDestroy()
    {
        ((IAutoSceneService)this).UnregisterSceneServices();
    }

    public void InitExternal()
    {
        OnResourceChanged?.Invoke(currentResource);
    }

    public bool CanAfford(int amount)
    {
        return currentResource >= amount;
    }

    public bool Spend(int amount)
    {
        if (CanAfford(amount))
        {
            currentResource -= amount;
            OnResourceChanged?.Invoke(currentResource);
            return true;
        }
        return false;
    }

    public void Earn(int amount)
    {
        currentResource += amount;
        OnResourceChanged?.Invoke(currentResource);
    }

    public void InitResource(int amount)
    {
        if (amount <= 0) return;

        currentResource = amount;
    }
}
