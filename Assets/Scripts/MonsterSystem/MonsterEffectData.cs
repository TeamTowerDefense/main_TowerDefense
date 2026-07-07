using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;

[CreateAssetMenu(menuName = "TowerDB/Monster Effect Data")]
public class MonsterEffectData : ScriptableObject
{
    public int effectID;
    public AssetReferenceGameObject effectPF;
    [HideInInspector] public GameObject loadedPrefab;
}
