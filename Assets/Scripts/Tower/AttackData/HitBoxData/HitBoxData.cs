using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum HitBoxDamageMode
{
    OncePerTarget,
    TickDamage
}

public class HitBoxData : AttackBaseData
{
    [Header("Addressable")]
    public int hitBoxID;
    public int hitEffectID;

    public float activeTime = 0.3f;
    public float damageInterval = 0.2f;
    public EffectData hitEffectData;

    [Header("Shape")]
    public Vector3 center = new Vector3(0f, 0f, 3f);

    [Header("Damage Type")]
    public HitBoxDamageMode damageMode = HitBoxDamageMode.OncePerTarget;
    [Header("Effect Option")]
    public bool stopEffectOutOfRange = true;

    [Header("Hit Timing")]
    public float colliderActiveTime = 0.2f;
    
    [Header("Debuff")]
    public bool applyDebuff;
    public List<DebuffEffectData> debuffs;

    [Header("Debuff Tick")]
    public float debuffApplyInterval = 0.5f;

    public AssetReferenceGameObject hitboxPF;

    [HideInInspector] public GameObject loadedPrefab;
}
