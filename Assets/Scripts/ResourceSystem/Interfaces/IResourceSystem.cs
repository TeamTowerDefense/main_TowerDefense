using System;
using UnityEngine;

public interface IResourceSystem
{
    event Action<int> OnResourceChanged;

    int CurrentResource { get; }

    bool CanAfford(int amount);

    bool Spend(int amount);

    // ÀÚ¿ø È¹µæ
    void Earn(int amount);
}
