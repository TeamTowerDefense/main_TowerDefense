using UnityEngine;

public interface IOnHitModifier
{
    void OnHit(MonoBehaviour owner, KeywordController targetController);
}
