using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "TowerDB/Effect Data")]
public class EffectData : ScriptableObject
{
    public int effectID;
    public string label;
    public AssetReferenceGameObject effectPF;

    [HideInInspector] public GameObject loadedPrefab;
}
