using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class ProjectileData : AttackBaseData
{
    [Header("Addressable")]
    public int projectileID;
    public int hitEffectID;
    public AssetReferenceGameObject projectilePF;
    [HideInInspector] public GameObject loadedPrefab { get; set; }

    [Header("기본 정보")]
    public float projectileSpeed = 10f;
    public LayerMask targetLayer;

    [Header("Sound")]
    public int fireSoundID;
    public int hitSoundID;
}

